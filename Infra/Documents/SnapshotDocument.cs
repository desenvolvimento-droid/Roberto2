using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Infra.EventStore.Mongo.Documents;

public sealed class EventDocument
{
    [BsonId]
    public Guid Id { get; set; } // EventId (único)

    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public DateTime OccurredOn { get; set; }
    public long Version { get; set; }

    // Serializado como BsonDocument para compatibilidade e versionamento.
    public BsonDocument Data { get; set; } = null!;
}

public sealed class SnapshotDocument
{
    [BsonId]
    public ObjectId InternalId { get; set; }

    public Guid AggregateId { get; set; }
    public string SnapshotType { get; set; } = null!;
    public long Version { get; set; }
    public BsonDocument Data { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}