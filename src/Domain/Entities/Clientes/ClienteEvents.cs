using BuildingBlocks.Core.Event;

namespace Domain.Entities.Clientes;

public sealed record ClienteEvents(
    Guid ClienteId,
    long Version) 
    : DomainEvent(Guid.NewGuid(), nameof(ClienteEvents), Version);

public sealed record ClienteCreated(
    Guid ClienteId, 
    string Nome,
    long Versao) 
    : DomainEvent(Guid.NewGuid(), nameof(ClienteCreated), Versao);

public sealed record ClienteDeactivated(
    Guid ClienteId,
    long Versao) 
    : DomainEvent(Guid.NewGuid(), nameof(ClienteDeactivated), Versao);

public sealed record ReservationCompleted(
    Guid ClienteId, 
    decimal Valor,
    long Versao) 
    : DomainEvent(Guid.NewGuid(), nameof(ClienteDeactivated), Versao);