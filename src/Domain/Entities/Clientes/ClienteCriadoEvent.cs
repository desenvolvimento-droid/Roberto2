using BuildingBlocks.Core.Event;

namespace Domain.Entities.Clientes;

public record ClienteCriadoEvent(
    Guid Id, 
    string Nome) : DomainEvent(Id, nameof(NomeClienteAtualizadoEvent));

public record NomeClienteAtualizadoEvent(
    Guid Id, 
    string Nome) : DomainEvent(Id, nameof(NomeClienteAtualizadoEvent));
