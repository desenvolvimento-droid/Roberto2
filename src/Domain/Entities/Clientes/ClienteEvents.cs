using BuildingBlocks.Core.Event;

namespace Domain.Entities.Clientes;

public sealed record ClienteEvents(Guid ClienteId) : DomainEvent(Guid.NewGuid(), nameof(ClienteEvents));

public sealed record ClienteCreated(Guid ClienteId, string Nome) : DomainEvent(Guid.NewGuid(), nameof(ClienteCreated));

public sealed record ClienteDeactivated(Guid ClienteId) : DomainEvent(Guid.NewGuid(), nameof(ClienteDeactivated));