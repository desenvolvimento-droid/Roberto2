using BuildingBlocks.Core.Model;

namespace Domain.Interfaces;

/// <summary>
/// Contrato para repositórios de Event Sourcing baseados em agregados.
/// </summary>
/// <typeparam name="TAggregate">Tipo do agregado</typeparam>
public interface IEventStoreRepository<TAggregate>
    where TAggregate : AggregateRoot, new()
{
    /// <summary>
    /// Carrega o agregado reconstruído a partir do histórico de eventos.
    /// </summary>
    /// <param name="id">Identificador do agregado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task<TAggregate?> LoadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona os eventos pendentes do agregado ao stream, criando ou atualizando conforme necessário.
    /// </summary>
    /// <param name="aggregate">Agregado com eventos pendentes</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task AppendAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
}