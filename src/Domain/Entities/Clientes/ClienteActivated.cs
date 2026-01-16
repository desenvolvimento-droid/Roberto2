using BuildingBlocks.Core.Event;

namespace Domain.Entities.Clientes;

public sealed record ClienteActivated(Guid ClienteId) : DomainEvent(Guid.NewGuid(), nameof(ClienteActivated));
