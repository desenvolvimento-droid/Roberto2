using MediatR;

namespace BuildingBlocks.Core.Event;


public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    public DateTime OcorreuEm { get; }
    public string TipoEvento { get; }
    public long Versao { get; }
}

public record DomainEvent : IDomainEvent
{
    public DomainEvent(Guid eventId, string tipoEvento, long versao)
    {
        TipoEvento = tipoEvento;
        EventId = eventId;
        Versao = versao;
    }

    public DateTime OcorreuEm => DateTime.Now;
    public string TipoEvento { get; private set; }

    public Guid EventId { get; private set; }

    public long Versao { get; private set; }
}

public interface IIntegrationEvent : IDomainEvent;


