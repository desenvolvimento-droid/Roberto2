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
    // Keep backward-compatibility: version is optional and defaults to 0 when not provided.
    public DomainEvent(Guid eventId, string tipoEvento, long versao = 0)
    {
        TipoEvento = tipoEvento;
        EventId = eventId;
        Versao = versao;
    }

    // Use UTC consistently across the domain
    public DateTime OcorreuEm => DateTime.UtcNow;
    public string TipoEvento { get; private set; }

    public Guid EventId { get; private set; }

    public long Versao { get; private set; }
}

public interface IIntegrationEvent : IDomainEvent;


