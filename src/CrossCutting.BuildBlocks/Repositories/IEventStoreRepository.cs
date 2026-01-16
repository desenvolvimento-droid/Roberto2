using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;

namespace Domain.Interfaces;

/// <summary>
/// Contrato para repositórios de Event Sourcing baseados em agregados.
/// Expandido para suportar melhores práticas: versionamento, idempotência, paginação e snapshots.
/// </summary>
/// <typeparam name="TAggregate">Tipo do agregado</typeparam>
public interface IEventStoreRepository<TAggregate>
    where TAggregate : AggregateRoot, new()
{
    /// <summary>
    /// Carrega o agregado reconstruído a partir do histórico de eventos.
    /// </summary>
    /// <param name="id">Identificador do agregado</param>
    /// <param name="uptoVersion">Se informado, reidrata até a versão especificada (inclusive)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task<TAggregate?> LoadAsync(Guid id, long? uptoVersion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera eventos do stream com paginação (útil para reprojeção / catch-up).
    /// </summary>
    Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid aggregateId, long fromVersion = 1, int pageSize = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna a última versão conhecida do stream (0 se inexistente).
    /// </summary>
    Task<long> GetLastVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Append explícito com expectedVersion para optimistic concurrency.
    /// Deve ser idempotente: se EventId(s) já persistidos, não falhar.
    /// </summary>
    Task AppendAsync(Guid aggregateId, IReadOnlyCollection<IDomainEvent> events, long expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Conveniência: append a partir de agregado, usando aggregate.DomainEvents e aggregate.Versao como expectedVersion.
    /// Implementação deve garantir expected-version, idempotência e atualizar a versão do agregado.
    /// </summary>
    Task AppendAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Salva snapshot do agregado para acelerar reidratação.
    /// </summary>
    Task SaveSnapshotAsync(Guid aggregateId, object snapshot, long version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera snapshot e a versão associada.
    /// </summary>
    Task<(object? Snapshot, long Version)> GetSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica existência de stream.
    /// </summary>
    Task<bool> StreamExistsAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deleta stream (uso administrativo).
    /// </summary>
    Task DeleteStreamAsync(Guid aggregateId, CancellationToken cancellationToken = default);
}