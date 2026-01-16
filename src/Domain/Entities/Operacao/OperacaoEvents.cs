using BuildingBlocks.Core.Event;

namespace Domain.Entities.Operacao;

public sealed record OperacaoEvents(System.Guid OperacaoId) 
    : DomainEvent(System.Guid.NewGuid(), nameof(OperacaoEvents)), IIntegrationEvent;

public sealed record OperacaoCreated(System.Guid OperacaoId, System.Guid ContaId, OperacaoTipo Tipo, decimal Amount, System.Guid? DestinationContaId, System.Guid CorrelationId)
    : DomainEvent(System.Guid.NewGuid(), nameof(OperacaoCreated)), IIntegrationEvent;

public sealed record OperacaoFailed(System.Guid OperacaoId, string Reason) 
    : DomainEvent(System.Guid.NewGuid(), nameof(OperacaoFailed)), IIntegrationEvent;

public sealed record OperacaoProcessed(System.Guid OperacaoId) 
    : DomainEvent(System.Guid.NewGuid(), nameof(OperacaoProcessed)), IIntegrationEvent;

public sealed record OperacaoReverted(System.Guid OperacaoId, string Reason) 
    : DomainEvent(System.Guid.NewGuid(), nameof(OperacaoReverted)), IIntegrationEvent;

public sealed record OperacaoValidated(System.Guid OperacaoId) 
    : DomainEvent(System.Guid.NewGuid(), nameof(OperacaoValidated)), IIntegrationEvent;