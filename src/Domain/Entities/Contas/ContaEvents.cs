using BuildingBlocks.Core.Event;

namespace Domain.Entities.Contas;

public sealed record ContaCreated(System.Guid ContaId, System.Guid ClienteId) : DomainEvent(System.Guid.NewGuid(), nameof(ContaCreated));

public sealed record LimiteCreditoDefinido(System.Guid ContaId, decimal LimiteCredito) : DomainEvent(System.Guid.NewGuid(), nameof(LimiteCreditoDefinido));

public sealed record ContaActivated(System.Guid ContaId) : DomainEvent(System.Guid.NewGuid(), nameof(ContaActivated));

public sealed record ContaBlocked(System.Guid ContaId) : DomainEvent(System.Guid.NewGuid(), nameof(ContaBlocked));

public sealed record ReservationCompleted(Guid ContaId, decimal Valor) : DomainEvent(Guid.NewGuid(), nameof(ReservationCompleted));
