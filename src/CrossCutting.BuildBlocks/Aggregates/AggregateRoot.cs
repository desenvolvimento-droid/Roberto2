using BuildingBlocks.Core.Event;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BuildingBlocks.Core.Model;

/// <summary>
/// Base para Aggregate Roots compatível com Event Sourcing.
/// Responsável por:
/// - Aplicação de eventos (novos vs histórico)
/// - Controle de versão (optimistic concurrency)
/// - Tracking de eventos não confirmados
/// - Suporte a snapshot
/// - Validação de invariantes
///
/// Observação:
/// - A ordenação e versionamento oficial dos eventos
///   deve ser garantida pelo Event Store.
/// </summary>
public abstract class AggregateRoot : IAggregate
{
    // Identidade do agregado (definida apenas por eventos)
    public Guid Id { get; protected set; }

    // Versão atual do agregado (quantidade de eventos confirmados)
    public long Versao { get; protected set; } = 0;

    // Versão observada no carregamento (expectedVersion)
    public long OriginalVersao { get; private set; } = 0;

    // Auditoria técnica
    public DateTime CriadoEm { get; protected set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; protected set; } = DateTime.UtcNow;

    // Eventos ainda não persistidos
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    public IReadOnlyCollection<IDomainEvent> GetUncommittedEvents()
        => _uncommittedEvents.AsReadOnly();

    // Proteção adicional contra reaplicação acidental
    private readonly HashSet<Guid> _appliedEventIds = new();

    /// <summary>
    /// Registra e aplica um novo evento de domínio.
    /// Gera um evento não confirmado (uncommitted).
    /// </summary>
    protected void RecordEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is null)
            throw new ArgumentNullException(nameof(domainEvent));

        // Proteção contra duplicidade acidental
        if (_appliedEventIds.Contains(domainEvent.EventId))
            return;

        When(domainEvent);
        ValidateInvariants();

        _uncommittedEvents.Add(domainEvent);
        _appliedEventIds.Add(domainEvent.EventId);

        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Aplica um evento vindo do histórico.
    /// Não gera evento não confirmado.
    /// </summary>
    public void ApplyEventFromHistory(IDomainEvent domainEvent)
    {
        if (domainEvent is null)
            throw new ArgumentNullException(nameof(domainEvent));

        if (_appliedEventIds.Contains(domainEvent.EventId))
            return;

        When(domainEvent);

        _appliedEventIds.Add(domainEvent.EventId);
        Versao++;
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Reidrata o agregado a partir de um histórico ordenado de eventos.
    /// </summary>
    public void LoadFromHistory(IEnumerable<IDomainEvent> history)
    {
        if (history is null)
            throw new ArgumentNullException(nameof(history));

        foreach (var @event in history)
            ApplyEventFromHistory(@event);

        OriginalVersao = Versao;
        _uncommittedEvents.Clear();
        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Hook opcional para criação de snapshot.
    /// </summary>
    public virtual object? CreateSnapshot() => null;

    /// <summary>
    /// Hook opcional para restaurar estado a partir de snapshot.
    /// </summary>
    protected virtual void RestoreFromSnapshot(object snapshot) { }

    /// <summary>
    /// Restaura estado a partir de snapshot e ajusta versão internamente.
    /// Deve ser chamado pelo repositório.
    /// </summary>
    public void RestoreFromSnapshotState(object snapshot, long snapshotVersion)
    {
        RestoreFromSnapshot(snapshot);

        Versao = snapshotVersion;
        OriginalVersao = snapshotVersion;

        _uncommittedEvents.Clear();
        _appliedEventIds.Clear();

        AtualizadoEm = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca eventos não confirmados como persistidos
    /// e atualiza a versão do agregado.
    /// </summary>
    public IDomainEvent[] MarkEventsAsCommitted()
    {
        if (_uncommittedEvents.Count == 0)
            return Array.Empty<IDomainEvent>();

        var committed = _uncommittedEvents.ToArray();
        _uncommittedEvents.Clear();

        Versao += committed.Length;
        OriginalVersao = Versao;

        return committed;
    }

    /// <summary>
    /// Aplica o efeito de um evento no estado do agregado.
    /// Não deve gerar novos eventos.
    /// </summary>
    protected abstract void When(IDomainEvent @event);

    /// <summary>
    /// Valida invariantes de domínio após mudanças de estado.
    /// </summary>
    protected abstract void ValidateInvariants();
}
