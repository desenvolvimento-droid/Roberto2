using BuildingBlocks.Core.Event;
using System.Reflection;

namespace BuildingBlocks.Core.Model;

public abstract record AggregateRoot : IAggregate
{
    public Guid Id { get; set; }
    public long Versao { get; set; } = 0;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IDomainEvent[] ClearDomainEvents()
    {
        IDomainEvent[] dequeuedEvents = _domainEvents.ToArray();

        _domainEvents.Clear();
        Versao++;
        return dequeuedEvents;
    }

    public void ApplyEvent(IDomainEvent @event)
    {
        When(@event);
        Validar();
        _domainEvents.Add(@event);
        AtualizadoEm = DateTime.UtcNow;    
    }

    protected abstract void When(IDomainEvent @event);
    protected abstract void Validar();

}