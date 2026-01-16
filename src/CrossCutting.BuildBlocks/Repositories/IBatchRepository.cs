using BuildingBlocks.Core.Event;

namespace CrossCutting.BuildBlocks.Repositories;

public interface IBatchRepository<T>
{
    Task SaveEventsAsync(
        IReadOnlyCollection<T> messages,
        CancellationToken cancellationToken = default);

    Task<List<T>> GetPendingAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default);
}
