using CrossCutting.BuildBlocks.Repositories;
using Infra.Services.Batch.OutboxMessage;
using MongoDB.Driver;

namespace Infra.Persistence.Mongo.Outbox;

public sealed class BatchRepository : IBatchRepository<BatchModel>
{
    private const string CollectionName = "outbox_messages";

    private readonly IMongoCollection<BatchModel> _collection;

    public BatchRepository(
        IMongoClient client,
        string databaseName)
    {
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<BatchModel>(CollectionName);

        CreateIndexes();
    }

    // ------------------------------------
    // Indexes for performance & correctness
    // ------------------------------------
    private void CreateIndexes()
    {
        var indexes = new List<CreateIndexModel<BatchModel>>
        {
            new(
                Builders<BatchModel>.IndexKeys
                    .Ascending(x => x.ProcessedAt)
                    .Ascending(x => x.ProcessingAt)
                    .Ascending(x => x.OccurredAt)),

            new(
                Builders<BatchModel>.IndexKeys
                    .Ascending(x => x.CorrelationId)),

            new(
                Builders<BatchModel>.IndexKeys
                    .Ascending(x => x.ReferenceId))
        };

        _collection.Indexes.CreateMany(indexes);
    }

    // ---------------------------
    // Save (batch insert)
    // ---------------------------
    public async Task SaveEventsAsync(
        IReadOnlyCollection<BatchModel> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages is null || messages.Count == 0)
            return;

        await _collection.InsertManyAsync(
            messages,
            new InsertManyOptions { IsOrdered = false },
            cancellationToken);
    }

    // ----------------------------------------------------
    // Get Pending (atomic lock per document)
    // ----------------------------------------------------
    public async Task<List<BatchModel>> GetPendingAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var filter = Builders<BatchModel>.Filter.And(
            Builders<BatchModel>.Filter.Eq(x => x.ProcessedAt, null),
            Builders<BatchModel>.Filter.Eq(x => x.ProcessingAt, null)
        );

        var update = Builders<BatchModel>.Update
            .Set(x => x.ProcessingAt, now);

        var options = new FindOneAndUpdateOptions<BatchModel>
        {
            ReturnDocument = ReturnDocument.After
        };

        var messages = new List<BatchModel>();

        for (var i = 0; i < batchSize; i++)
        {
            var message = await _collection.FindOneAndUpdateAsync(
                filter,
                update,
                options,
                cancellationToken);

            if (message is null)
                break;

            messages.Add(message);
        }

        return messages;
    }

    // ---------------------------
    // Mark as Processed
    // ---------------------------
    public async Task MarkAsProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<BatchModel>.Filter.Eq(x => x.BatchId, messageId);

        var update = Builders<BatchModel>.Update
            .Set(x => x.ProcessedAt, DateTime.UtcNow)
            .Unset(x => x.ProcessingAt)
            .Unset(x => x.Error);

        await _collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    // ---------------------------
    // Mark as Failed
    // ---------------------------
    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<BatchModel>.Filter.Eq(x => x.BatchId, messageId);

        var update = Builders<BatchModel>.Update
            .Set(x => x.Error, error)
            .Inc(x => x.RetryCount, 1)
            .Unset(x => x.ProcessingAt);

        await _collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }
}
