using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;
using Domain.BuildingBlocks.Dispacher;
using Domain.BuildingBlocks.Models;
using Domain.Interfaces;
using Domain.Services;
using Infra.Repositories.Documents;
using Marten.Events.Projections;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Text.Json;

namespace Infra.EventStore.Mongo;

public sealed class MongoEventStoreRepository<TAggregate>(
    IMongoDatabase database,
    ILogger<MongoEventStoreRepository<TAggregate>> logger)
    : IEventStoreRepository<TAggregate>
    where TAggregate : AggregateRoot, new()
{
    private readonly IMongoCollection<EventDocument> _collection = database.GetCollection<EventDocument>("event_store");

    public async Task<TAggregate?> LoadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var events = await _collection
            .Find(e => e.AggregateId == id)
            .SortBy(e => e.Version)
            .ToListAsync(cancellationToken);

        if (events.Count == 0)
            return null;

        var aggregate = new TAggregate();

        foreach (var doc in events)
        {
            var domainEvent = Deserialize(doc);
            aggregate.ApplyEvent(domainEvent);
        }

        aggregate.ClearDomainEvents();

        return aggregate;
    }

    public async Task AppendAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        if (!aggregate.DomainEvents.Any())
            return;

        var expectedVersion = aggregate.Versao - aggregate.DomainEvents.Count;

        var lastVersion = await _collection
            .Find(e => e.AggregateId == aggregate.Id)
            .SortByDescending(e => e.Version)
            .Limit(1)
            .Project(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastVersion != expectedVersion)
            throw new InvalidOperationException(
                $"Concurrency conflict on aggregate {aggregate.Id}");

        var eventDocuments = aggregate.DomainEvents
            .Select((@event, index) => new EventDocument
            {
                Id = @event.EventId,
                AggregateId = aggregate.Id,
                AggregateType = typeof(TAggregate).AssemblyQualifiedName!,
                EventType = @event.TipoEvento,
                OccurredOn = @event.OcorreuEm,
                Version = expectedVersion + index + 1,
                Data = @event
            })
            .ToList();

        await _collection.InsertManyAsync(
            eventDocuments,
            cancellationToken: cancellationToken);

        aggregate.ClearDomainEvents();
    }

    private static IDomainEvent Deserialize(EventDocument doc)
    {
        var eventType = Type.GetType(doc.EventType, throwOnError: true)!;

        var json = JsonSerializer.Serialize(doc.Data);
        return (IDomainEvent)JsonSerializer.Deserialize(json, eventType)!;
    }
}
