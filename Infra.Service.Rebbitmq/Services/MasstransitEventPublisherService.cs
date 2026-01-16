using BuildingBlocks.Core.Event;
using CrossCutting.BuildBlocks.Repositories;
using CrossCutting.BuildBlocks.Services;
using Hangfire;
using Infra.Services.Batch.OutboxMessage;
using MassTransit;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace Infra.Publisher.MassTransit.Services;

public sealed class MasstransitEventPublisherService(
        IBatchRepository<BatchModel> outboxRepository,
        IPublishEndpoint publishEndpoint)
    : IEventPublisherService
{
    public async Task SchedulePublishEventAsync(
        IIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        var outbox = new BatchModel
        {
            ReferenceId = @event.EventId,
            Type = @event.GetType().AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(@event),
            OccurredAt = @event.OcorreuEm,
            Category = "Integration"
        };

        await outboxRepository.SaveEventsAsync(
            new[] { outbox },
            cancellationToken);
    }

    /// <summary>
    /// Job Hangfire para processar todos os eventos de integração pendentes
    /// (apenas eventos que implementam IIntegrationEvent, assíncrono e seguro)
    /// </summary>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    [Queue("integration-events")]
    public async Task PublishScheduledEventsAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await outboxRepository.GetPendingAsync(
            batchSize: 100,
            cancellationToken);

        if (messages.Count == 0)
            return;

        var actionBlock = new ActionBlock<BatchModel>(async message =>
        {
            try
            {
                var eventType = Type.GetType(message.Type, throwOnError: true)!;

                if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
                {
                    return;
                }

                var integrationEvent = (IIntegrationEvent)JsonSerializer.Deserialize(
                    message.Payload,
                    eventType)!;

                await publishEndpoint.Publish(
                    integrationEvent,
                    cancellationToken);

                await outboxRepository.MarkAsProcessedAsync(
                    message.BatchId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Falha individual
                await outboxRepository.MarkAsFailedAsync(
                    message.BatchId,
                    ex.ToString(),
                    cancellationToken);
            }
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 10, 
            CancellationToken = cancellationToken
        });

        foreach (var message in messages)
        {
            actionBlock.Post(message);
        }

        actionBlock.Complete();
        await actionBlock.Completion;
    }
}