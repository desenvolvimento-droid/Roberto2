using CrossCutting.BuildBlocks.Services;
using Hangfire;

namespace Worker.Workers;

public static class OrquestationWorker 
{
    public static void AddDispatchEventsSchedule()
    {
        RecurringJob.AddOrUpdate<IEventDispatcherService>(
            "process-pending-messages",
            processor => processor.DispatchEventsScheduleAsync(CancellationToken.None),
            "*/10 * * * * *"); 
    }

    public static void AddPublishScheduledEvents()
    {
        RecurringJob.AddOrUpdate<IEventPublisherService>(
            "process-pending-messages",
            processor => processor.PublishScheduledEventsAsync(CancellationToken.None),
            "*/10 * * * * *");
    }
}
