using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;
using CrossCutting.BuildBlocks.Repositories;
using CrossCutting.BuildBlocks.Services;
using Hangfire;
using Infra.Services.Batch.OutboxMessage;
using MediatR;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace Infra.Dispacher.Hangfire.Services;

public sealed class EventDispatcherService(
        IBatchRepository<BatchModel> outbox,
        IMediator mediator,
        IBatchRepository<BatchModel> outboxRepository)
    : IEventDispatcherService
{
    private const int DefaultBatchSize = 20;
    private const int MaxDegreeOfParallelism = 10;
    private const int HangfireTimeoutInSeconds = 300;

    public async Task ScheduleEventsDispatchAsync(
        IReadOnlyCollection<IDomainEvent> notification,
        CancellationToken cancellationToken = default)
    {
        if (notification is null || notification.Count == 0)
            return;

        foreach (var domainEvent in notification.Where(e => e is not null))
        {
            var batch = new BatchModel
            {
                ReferenceId = domainEvent.EventId,
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(
                    domainEvent,
                    domainEvent.GetType()),
                OccurredAt = DateTime.UtcNow
            };

            await outbox.SaveEventsAsync(new[] { batch }, cancellationToken);
        }
    }

    /// <summary>
    /// Job único do Hangfire para processamento de dispatcher
    /// (processa todos os eventos pendentes de forma assíncrona e segura)
    /// </summary>
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: HangfireTimeoutInSeconds)]
    [Queue("dispacher-events")]
    public async Task DispatchEventsScheduleAsync(
        CancellationToken cancellationToken = default)
    {
        // pega todos os eventos pendentes
        var messages = await outboxRepository.GetPendingAsync(
            batchSize: DefaultBatchSize, // ajustável
            cancellationToken);

        if (messages.Count == 0)
            return;

        // bloco de ação para processamento paralelo seguro
        var actionBlock = new ActionBlock<BatchModel>(async message =>
        {
            try
            {
                var eventType = Type.GetType(message.Type, throwOnError: true)!;
                var notification = (INotification)JsonSerializer.Deserialize(
                    message.Payload,
                    eventType)!;

                await mediator.Publish(notification, cancellationToken);

                await outboxRepository.MarkAsProcessedAsync(
                    message.BatchId,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // marca falha individualmente
                await outboxRepository.MarkAsFailedAsync(
                    message.BatchId,
                    ex.ToString(),
                    cancellationToken);
            }
        },
        new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism, // controlando paralelismo
            CancellationToken = cancellationToken
        });

        // envia todas as mensagens para processamento
        foreach (var message in messages)
        {
            actionBlock.Post(message);
        }

        // espera todos os eventos terminarem
        actionBlock.Complete();
        await actionBlock.Completion;
    }
}
