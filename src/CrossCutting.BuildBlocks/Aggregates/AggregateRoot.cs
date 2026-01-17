using BuildingBlocks.Core.Event;
using Domain.Exceptions;

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
    public Guid Id { get; set; }

    // Versão atual do agregado (quantidade de eventos confirmados)
    public long Versao { get; protected set; } = 0;

    // Versão observada no carregamento (expectedVersion)
    public long OriginalVersao { get; private set; } = 0;

    // Auditoria técnica
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

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

        // Ensure metadata contains a timestamp for auditing and reprojection.
        try
        {
            if (domainEvent.Metadata == null)
                domainEvent.Metadata = new Dictionary<string, string?>();

            if (!domainEvent.Metadata.ContainsKey("timestamp") || string.IsNullOrWhiteSpace(domainEvent.Metadata["timestamp"]))
                domainEvent.Metadata["timestamp"] = domainEvent.OcorreuEm.ToString("o");
        }
        catch
        {
            // Metadata is best-effort; do not break domain logic if metadata assignment fails.
        }

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
    /// Implementa o contrato de IAggregate.RestoreFromSnapshot(snapshot, snapshotVersion).
    /// </summary>
    public void RestoreFromSnapshot(object snapshot, long snapshotVersion)
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
    /// validando a versão esperada (expectedVersion) antes de atualizar a versão do agregado.
    /// </summary>
    public IDomainEvent[] MarkEventsAsCommitted(long expectedVersion)
    {
        if (_uncommittedEvents.Count == 0)
            return Array.Empty<IDomainEvent>();

        if (OriginalVersao != expectedVersion)
            throw new ConcurrencyException($"Concurrency conflict on aggregate {Id}. Expected {expectedVersion} but OriginalVersao is {OriginalVersao}.");

        var committed = _uncommittedEvents.ToArray();
        _uncommittedEvents.Clear();

        Versao += committed.Length;
        OriginalVersao = Versao;

        return committed;
    }

    /// <summary>
    /// Compatibilidade: versão sem parâmetro delega para a sobrecarga validando contra a OriginalVersao.
    /// </summary>
    public IDomainEvent[] MarkEventsAsCommitted()
        => MarkEventsAsCommitted(OriginalVersao);

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
