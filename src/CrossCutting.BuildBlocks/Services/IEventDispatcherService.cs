using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;

namespace CrossCutting.BuildBlocks.Services;

public interface IEventDispatcherService
{
    Task ScheduleEventsDispatchAsync(
        IReadOnlyCollection<IDomainEvent> notification,
        CancellationToken cancellationToken = default);

    Task DispatchEventsScheduleAsync(
        CancellationToken cancellationToken = default);
}
