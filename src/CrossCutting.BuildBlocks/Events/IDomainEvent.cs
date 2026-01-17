using MediatR;

namespace BuildingBlocks.Core.Event;


public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    public DateTime OcorreuEm { get; }
    public string TipoEvento { get; }
    // Versao must be writable so infrastructure / aggregates can assign the
    // expected aggregate version to the event instance before persisting.
    public long Versao { get; set; }
    // Arbitrary metadata for the event (origin, userId, requestId, etc.).
    // Stored as string values to ensure BSON/JSON compatibility.
    public IDictionary<string, string?> Metadata { get; set; }
}

public record DomainEvent : IDomainEvent
{
    public DomainEvent(Guid eventId, string tipoEvento, long versao = 0)
    {
        TipoEvento = tipoEvento;
        EventId = eventId;
        Versao = versao;
        Metadata = new Dictionary<string, string?>();
    }

    // Use UTC consistently across the domain
    public DateTime OcorreuEm => DateTime.UtcNow;
    public string TipoEvento { get; private set; }

    public Guid EventId { get; private set; }

    // Allow infrastructure to set the version before persistence so the
    // serialized event reflects the aggregate stream version.
    public long Versao { get; set; }
    public IDictionary<string, string?> Metadata { get; set; }
}

public interface IIntegrationEvent : IDomainEvent;


