using BuildingBlocks.Core.Event;

namespace BuildingBlocks.Core.Model;


public interface IAggregate 
{
    Guid Id { get; }
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    IDomainEvent[] ClearDomainEvents();
}