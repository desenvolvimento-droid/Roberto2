using Infra.Repositories.Documents;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Util;

public static class MongoEventStoreUtil
{
    public static async Task EnsureAsync(
        IMongoDatabase database,
        CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<EventDocument>("event_store");

        var indexes = new[]
        {
            new CreateIndexModel<EventDocument>(
                Builders<EventDocument>.IndexKeys
                    .Ascending(e => e.AggregateId)
                    .Ascending(e => e.Version),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_event_stream"
                }),

            new CreateIndexModel<EventDocument>(
                Builders<EventDocument>.IndexKeys
                    .Ascending(e => e.EventType),
                new CreateIndexOptions
                {
                    Name = "ix_event_type"
                })
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }
}
