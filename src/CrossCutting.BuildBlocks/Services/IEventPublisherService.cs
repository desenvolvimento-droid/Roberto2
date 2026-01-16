using BuildingBlocks.Core.Event;

namespace CrossCutting.BuildBlocks.Services;

public interface IEventPublisherService
{
    Task SchedulePublishEventAsync(
        IIntegrationEvent @event,
        CancellationToken cancellationToken = default);

    Task PublishScheduledEventsAsync(
        CancellationToken cancellationToken = default);
}
