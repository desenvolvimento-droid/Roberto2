namespace Infra.Services.Processors;

using Hangfire;
using MediatR;
using System;
using System.Threading.Tasks;
using System.Text.Json;

public class OutboxProcessorJob
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IMediator _mediator;

    public OutboxProcessorJob(IOutboxRepository outboxRepository, IMediator mediator)
    {
        _outboxRepository = outboxRepository;
        _mediator = mediator;
    }

    // Método que o Hangfire vai chamar periodicamente
    [AutomaticRetry(Attempts = 3)] // Retry automático do Hangfire
    public async Task ProcessOutboxAsync(Guid ProcessId)
    {
        const int batchSize = 50; // Processa 50 mensagens por vez
        var pendingMessages = await _outboxRepository.GetPendingMessagesAsync(batchSize);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Marca como Processing
                await _outboxRepository.MarkAsProcessingAsync(message.Id);

                // Desserializa o evento dinamicamente
                var eventType = Type.GetType(message.EventType);
                if (eventType == null)
                {
                    await _outboxRepository.MarkAsFailedAsync(message.Id, "Tipo de evento não encontrado");
                    continue;
                }

                var @event = JsonSerializer.Deserialize(message.Payload, eventType);
                if (@event is null)
                {
                    await _outboxRepository.MarkAsFailedAsync(message.Id, "Falha na desserialização do evento");
                    continue;
                }

                // Publica o evento no MediatR
                if (@event is INotification notification)
                {
                    await _mediator.Publish(notification);
                }
                else if (@event is IRequest)
                {
                    await _mediator.Send((IRequest<object>)@event);
                }
                else
                {
                    await _outboxRepository.MarkAsFailedAsync(message.Id, "Evento não implementa INotification/IRequest");
                    continue;
                }

                // Marca como Processed
                await _outboxRepository.MarkAsProcessedAsync(message.Id);
            }
            catch (Exception ex)
            {
                // Marca como Failed com a mensagem de erro
                await _outboxRepository.MarkAsFailedAsync(message.Id, ex.Message);
            }
        }
    }
}
