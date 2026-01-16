using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Core.Model;

/// <summary>
/// Contrato público para Aggregate Roots compatível com Event Sourcing / DDD.
/// Define a superfície mínima necessária para reidratação, versionamento,
/// eventos não confirmados (uncommitted) e snapshots.
/// </summary>
public interface IAggregate
{
    /// <summary>
    /// Identificador do agregado.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Versão de eventos confirmados no stream (apenas leitura pública).
    /// </summary>
    long Versao { get; }

    /// <summary>
    /// Versão observada ao carregar o agregado (útil como expectedVersion).
    /// </summary>
    long OriginalVersao { get; }

    /// <summary>
    /// Timestamps de auditoria.
    /// </summary>
    DateTime CriadoEm { get; set; }
    DateTime AtualizadoEm { get; set; }

    /// <summary>
    /// Aplica um evento vindo do histórico sem marcá-lo como uncommitted.
    /// Uso em replay/reidratação; implementação deve ser idempotente.
    /// </summary>
    void ApplyEventFromHistory(IDomainEvent domainEvent);

    /// <summary>
    /// Reidrata o agregado a partir de um histórico de eventos ordenados.
    /// </summary>
    void LoadFromHistory(IEnumerable<IDomainEvent> history);

    /// <summary>
    /// Marca os eventos não confirmados como confirmados (committed) e retorna os eventos confirmados.
    /// </summary>
    IDomainEvent[] MarkEventsAsCommitted();

    /// <summary>
    /// Retorna uma cópia/visão dos eventos não confirmados (uncommitted).
    /// </summary>
    IReadOnlyCollection<IDomainEvent> GetUncommittedEvents();

    /// <summary>
    /// Hook opcional para gerar um snapshot do estado atual do agregado.
    /// </summary>
    object? CreateSnapshot();

    /// <summary>
    /// Hook opcional para restaurar estado a partir de um snapshot desserializado.
    /// </summary>
    void RestoreFromSnapshot(object snapshot, long snapshotVersion);
}