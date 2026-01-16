using MediatR;

namespace BuildingBlocks.Core.Event;


public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    public DateTime OcorreuEm { get; }
    public string TipoEvento { get; }
}

public record DomainEvent : IDomainEvent
{
    public DomainEvent(Guid eventId, string tipoEvento)
    {
        TipoEvento = tipoEvento;
        EventId = eventId;
    }

    public DateTime OcorreuEm => DateTime.Now;
    public string TipoEvento { get; private set; }

    public Guid EventId { get; private set; }
}

public interface IIntegrationEvent : IDomainEvent;


