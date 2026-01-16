namespace Infra.Services.EventDispatchers.Outbox;

public enum OutboxMessageStatus
{
    Pendente,
    Processando,
    Processado,
    Falhou
}