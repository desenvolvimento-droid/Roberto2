using BuildingBlocks.Core.Event;

namespace Domain.Services;


public interface IEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<IDomainEvent> events,
        CancellationToken cancellationToken = default);
}

