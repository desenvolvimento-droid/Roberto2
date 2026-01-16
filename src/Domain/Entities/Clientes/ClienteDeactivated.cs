using BuildingBlocks.Core.Event;

namespace Domain.Entities.Clientes;

public sealed record ClienteDeactivated(Guid ClienteId) : DomainEvent(Guid.NewGuid(), nameof(ClienteDeactivated));
