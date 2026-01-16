using BuildingBlocks.Core.Event;

namespace Domain.Entities.Clientes;

public sealed record ClienteCreated(Guid ClienteId, string Nome) : DomainEvent(Guid.NewGuid(), nameof(ClienteCreated));
