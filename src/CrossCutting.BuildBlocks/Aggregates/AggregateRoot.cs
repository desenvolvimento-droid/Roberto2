using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Event;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingBlocks.Core.Model;

/// <summary>
/// Base para Aggregate Roots compatível com Event Sourcing.
/// Fornece: aplicação de eventos (replay vs novos), tracking de eventos não confirmados,
/// controle de versão, hooks de snapshot e validação de invariantes.
/// </summary>
public abstract record AggregateRoot : IAggregate
{
    // Identificador do agregado
    public Guid Id { get; set; }

    // Versão atual do agregado (número de eventos aplicados / confirmados)
    public long Versao { get; protected set; } = 0;

    // Versão observada quando o agregado foi carregado (útil como expectedVersion)
    public long OriginalVersao { get; private set; } = 0;

    // Auditoria
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    // Eventos não confirmados
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();

    // Helper para idempotência durante replay
    private readonly HashSet<Guid> _appliedEventIds = new();

    /// <summary>
    /// Registra e aplica um novo evento no agregado (gera um UncommittedEvent).
    /// </summary>
    protected void RecordEvent(IDomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
        if (_uncommittedEvents.Any(e => e.EventId == domainEvent.EventId)) return;

        When(domainEvent);
        ValidateInvariants();

        _uncommittedEvents.Add(domainEvent);
        _appliedEventIds.Add(domainEvent.EventId);

        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Aplica um evento vindo do histórico sem marcá-lo como uncommitted.
    /// Idempotente: ignora EventId já aplicado.
    /// </summary>
    public void ApplyEventFromHistory(IDomainEvent domainEvent)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
        if (_appliedEventIds.Contains(domainEvent.EventId)) return;

        When(domainEvent);
        _appliedEventIds.Add(domainEvent.EventId);

        Versao++;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Reidrata o agregado a partir do histórico (lista ordenada).
    /// </summary>
    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (history == null) throw new ArgumentNullException(nameof(history));

        foreach (var e in history)
            ApplyEventFromHistory(e);

        OriginalVersao = Versao;
        _uncommittedEvents.Clear();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Hook opcional: criar snapshot (implementação concreta retorna DTO de snapshot).
    /// </summary>
    public virtual object? CreateSnapshot() => null;

    /// <summary>
    /// Hook opcional: restaurar estado a partir de snapshot. Implementação concreta deve aplicar estado.
    /// </summary>
    public virtual void RestoreFromSnapshot(object snapshot, long snapshotVersion) { }

    /// <summary>
    /// Restaura snapshot e garante atualização de versão do agregado internamente.
    /// Use este método a partir do repositório ao desserializar snapshot.
    /// </summary>
    public virtual void RestoreFromSnapshotState(object snapshot, long snapshotVersion)
    {
        RestoreFromSnapshot(snapshot, snapshotVersion);

        // Ajusta versão observada após restaurar estado
        Versao = snapshotVersion;
        OriginalVersao = snapshotVersion;

        _uncommittedEvents.Clear();
        _appliedEventIds.Clear();

        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca eventos não confirmados como confirmados (committed) e atualiza versão.
    /// </summary>
    public IDomainEvent[] MarkEventsAsCommitted()
    {
        if (_uncommittedEvents.Count == 0) return Array.Empty<IDomainEvent>();

        var committed = _uncommittedEvents.ToArray();
        _uncommittedEvents.Clear();

        Versao += committed.Length;
        OriginalVersao = Versao;

        return committed;
    }

    /// <summary>
    /// Deve ser implementado para aplicar efeitos de estado de cada evento.
    /// Não deve gerar novos eventos (use RecordEvent para isso).
    /// </summary>
    protected abstract void When(IDomainEvent @event);

    /// <summary>
    /// Deve validar invariantes de domínio após alterações de estado.
    /// Lançar exceção caso invariantes violadas.
    /// </summary>
    protected abstract void ValidateInvariants();
}