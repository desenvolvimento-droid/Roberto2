using BuildingBlocks.Core.Event;
using BuildingBlocks.Core.Model;
using Domain.Interfaces;
using Infra.EventStore.Mongo.Documents;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

// Namespace leve e alinhado com o layer de registro em DI.
namespace Infra.EventStore.Mongo.Repositories;

/// <summary>
/// Implementação MongoDB do repositório de Event Store.
/// - Usa índices únicos para garantir idempotência e concorrência por versão.
/// - Bulk inserts com IsOrdered = false para performance.
/// - Tratamento de BulkWrite para tolerância a reenvios (idempotência) e race conditions.
/// - Async/await e CancellationToken em todas as operações.
/// </summary>
/// <typeparam name="TAggregate">Tipo do agregado</typeparam>
public sealed class MongoEventStoreRepository<TAggregate> : IEventStoreRepository<TAggregate>
    where TAggregate : AggregateRoot, new()
{
    private const string EventsCollectionName = "event_store";
    private const string SnapshotsCollectionName = "event_store_snapshots";

    // Document model otimizado para consultas/serialização
    

    private readonly IMongoCollection<EventDocument> _events;
    private readonly IMongoCollection<SnapshotDocument> _snapshots;
    private readonly ILogger<MongoEventStoreRepository<TAggregate>> _logger;

    public MongoEventStoreRepository(IMongoDatabase database, ILogger<MongoEventStoreRepository<TAggregate>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (database == null) throw new ArgumentNullException(nameof(database));

        _events = database.GetCollection<EventDocument>(EventsCollectionName);
        _snapshots = database.GetCollection<SnapshotDocument>(SnapshotsCollectionName);

        // Não bloquear startup em ambientes onde criação de índices é restrita.
        try
        {
            EnsureIndexes();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro garantindo índices do EventStore (pode ser ambiente com permissão restrita).");
        }
    }

    private void EnsureIndexes()
    {
        var keys = Builders<EventDocument>.IndexKeys;
        var models = new List<CreateIndexModel<EventDocument>>
        {
            // índice único por EventId protege idempotência por id
            new CreateIndexModel<EventDocument>(keys.Ascending(e => e.Id),
                new CreateIndexOptions { Unique = true, Name = "ix_event_id_unique" }),

            // índice único por aggregate + version -> garante concorrência por versão
            new CreateIndexModel<EventDocument>(keys.Ascending(e => e.AggregateId).Ascending(e => e.Version),
                new CreateIndexOptions { Unique = true, Name = "ix_aggregate_version_unique" }),

            // query pattern para fetch por aggregate em ordem
            new CreateIndexModel<EventDocument>(keys.Ascending(e => e.AggregateId).Ascending(e => e.Version),
                new CreateIndexOptions { Name = "ix_aggregate_version_asc" })
        };

        _events.Indexes.CreateMany(models);

        var snapshotKeys = Builders<SnapshotDocument>.IndexKeys;
        _snapshots.Indexes.CreateOne(new CreateIndexModel<SnapshotDocument>(snapshotKeys.Ascending(s => s.AggregateId).Descending(s => s.Version),
            new CreateIndexOptions { Name = "ix_snapshot_aggregate_version" }));
    }

    public async Task<TAggregate?> LoadAsync(Guid aggregateId, long? uptoVersion = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1) Tenta snapshot mais recente (<= uptoVersion quando informado)
        SnapshotDocument? snapDoc;
        if (uptoVersion.HasValue)
        {
            snapDoc = await _snapshots.Find(s => s.AggregateId == aggregateId && s.Version <= uptoVersion.Value)
                                      .SortByDescending(s => s.Version)
                                      .Limit(1)
                                      .FirstOrDefaultAsync(cancellationToken);
        }
        else
        {
            snapDoc = await _snapshots.Find(s => s.AggregateId == aggregateId)
                                      .SortByDescending(s => s.Version)
                                      .Limit(1)
                                      .FirstOrDefaultAsync(cancellationToken);
        }

        var aggregate = new TAggregate();

        if (snapDoc != null)
        {
            try
            {
                var clr = Type.GetType(snapDoc.SnapshotType);
                if (clr != null)
                {
                    var snapshotObj = BsonSerializer.Deserialize(snapDoc.Data, clr);
                    // Hook público para a implementação concreta restaurar estado do snapshot.
                    aggregate.RestoreFromSnapshotState(snapshotObj, snapDoc.Version);
                }
                else
                {
                    _logger.LogDebug("Tipo de snapshot não encontrado ({SnapshotType}) para aggregate {AggregateId}", snapDoc.SnapshotType, aggregateId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao desserializar snapshot para aggregate {AggregateId}. Ignorando snapshot.", aggregateId);
            }
        }

        // 2) Busca eventos após o snapshot (ou desde 1)
        var startVersion = (snapDoc?.Version ?? 0) + 1;
        var filter = Builders<EventDocument>.Filter.Eq(e => e.AggregateId, aggregateId)
                     & Builders<EventDocument>.Filter.Gte(e => e.Version, startVersion);

        if (uptoVersion.HasValue)
            filter &= Builders<EventDocument>.Filter.Lte(e => e.Version, uptoVersion.Value);

        var eventsCursor = await _events.Find(filter)
                                        .SortBy(e => e.Version)
                                        .ToListAsync(cancellationToken);

        if (eventsCursor.Count == 0 && (snapDoc == null || aggregate.Versao == 0))
        {
            // stream inexistente
            return null;
        }

        // Reidrata aplicando cada evento via hook de agregados (idempotência é responsabilidade do aggregate)
        foreach (var doc in eventsCursor)
        {
            try
            {
                var evt = Deserialize(doc);
                aggregate.ApplyEventFromHistory(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao desserializar/aplicar evento {EventId} do aggregate {AggregateId}", doc.Id, aggregateId);
                throw;
            }
        }

        return aggregate;
    }

    public async Task<IReadOnlyList<IDomainEvent>> GetEventsAsync(Guid aggregateId, long fromVersion = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var docs = await _events.Find(e => e.AggregateId == aggregateId && e.Version >= fromVersion)
                                .SortBy(e => e.Version)
                                .Limit(pageSize)
                                .ToListAsync(cancellationToken);

        return docs.Select(Deserialize).ToArray();
    }

    public async Task<long> GetLastVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var last = await _events.Find(e => e.AggregateId == aggregateId)
                                .SortByDescending(e => e.Version)
                                .Limit(1)
                                .Project(e => e.Version)
                                .FirstOrDefaultAsync(cancellationToken);

        // FirstOrDefaultAsync retorna 0 quando não existe documento; manter garantia de 0.
        return last;
    }

    /// <summary>
    /// Persistência principal de eventos com controle otimista e idempotência.
    /// - expectedVersion: versão observada antes do append (use aggregate.OriginalVersao).
    /// </summary>
    public async Task AppendAsync(Guid aggregateId, IReadOnlyCollection<IDomainEvent> events, long expectedVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (events == null || events.Count == 0)
            return;

        // Verifica last version atual para validar concorrência
        var lastVersion = await GetLastVersionAsync(aggregateId, cancellationToken);
        if (lastVersion != expectedVersion)
            throw new ConcurrencyException($"Concurrency conflict on aggregate {aggregateId}. Expected {expectedVersion} but last version is {lastVersion}.");

        // Preparar documentos com versões incrementais
        var eventDocs = events.Select((e, i) =>
        {
            var evType = e.GetType();
            return new EventDocument
            {
                Id = e.EventId,
                AggregateId = aggregateId,
                AggregateType = typeof(TAggregate).AssemblyQualifiedName!,
                EventType = evType.AssemblyQualifiedName!,
                OccurredOn = e.OcorreuEm,
                Version = expectedVersion + i + 1,
                Data = e.ToBsonDocument()
            };
        }).ToList();

        // InsertMany com IsOrdered = false para melhor throughput; em caso de BulkWriteException
        // reavaliamos idempotência pela faixa de versões envolvida.
        try
        {
            await _events.InsertManyAsync(eventDocs, new InsertManyOptions { IsOrdered = false }, cancellationToken);
        }
        catch (MongoBulkWriteException<EventDocument> bulkEx)
        {
            _logger.LogWarning(bulkEx, "Bulk write exception ao gravar eventos para aggregate {AggregateId}", aggregateId);

            // Recupera eventos existentes para a faixa de versões tentadas
            var fromVersion = eventDocs.Min(d => d.Version);
            var toVersion = eventDocs.Max(d => d.Version);

            var existing = await _events.Find(e => e.AggregateId == aggregateId && e.Version >= fromVersion && e.Version <= toVersion)
                                        .ToListAsync(cancellationToken);

            var existingByVersion = existing.ToDictionary(e => e.Version);

            // Verifica compatibilidade (idempotência por EventId) ou conflito de versão
            foreach (var doc in eventDocs)
            {
                if (existingByVersion.TryGetValue(doc.Version, out var existent))
                {
                    if (existent.Id == doc.Id)
                        continue; // idempotente
                    // versão já ocupada por um evento diferente -> conflito de concorrência real
                    throw new ConcurrencyException($"Concurrency conflict: version {doc.Version} already taken by a different event (aggregate {aggregateId}).");
                }

                // Se não existe, tentamos inserir individualmente (tolerância a inserção parcial)
                try
                {
                    await _events.InsertOneAsync(doc, cancellationToken: cancellationToken);
                }
                catch (MongoWriteException mwx) when (mwx.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                {
                    // Race condition: checar existência novamente
                    var existNow = await _events.Find(e => e.Id == doc.Id).FirstOrDefaultAsync(cancellationToken);
                    if (existNow == null || existNow.Version != doc.Version)
                        throw new InvalidOperationException($"Idempotency/Concurrency ambiguity persisting event {doc.Id} for aggregate {aggregateId}.");
                }
            }
        }
    }

    public async Task AppendAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (aggregate == null)
            throw new ArgumentNullException(nameof(aggregate));

        var newEvents = aggregate.GetUncommittedEvents();
        if (newEvents == null || newEvents.Count == 0)
            return;

        // expectedVersion deve ser a versão observada ao carregar o agregado
        var expectedVersion = aggregate.OriginalVersao;

        await AppendAsync(aggregate.Id, newEvents, expectedVersion, cancellationToken);

        // Após persistência bem sucedida, marcar eventos como committed (atualiza versão do agregado)
        aggregate.MarkEventsAsCommitted();
    }

    public async Task SaveSnapshotAsync(Guid aggregateId, object snapshot, long version, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));

        var doc = new SnapshotDocument
        {
            AggregateId = aggregateId,
            SnapshotType = snapshot.GetType().AssemblyQualifiedName!,
            Version = version,
            Data = snapshot.ToBsonDocument(),
            CreatedAt = DateTime.UtcNow
        };

        // Upsert mantendo apenas o snapshot mais avançado por AggregateId
        var filter = Builders<SnapshotDocument>.Filter.Eq(s => s.AggregateId, aggregateId) & Builders<SnapshotDocument>.Filter.Lte(s => s.Version, version);
        var update = Builders<SnapshotDocument>.Update
            .Set(s => s.SnapshotType, doc.SnapshotType)
            .Set(s => s.Version, doc.Version)
            .Set(s => s.Data, doc.Data)
            .Set(s => s.CreatedAt, doc.CreatedAt)
            .SetOnInsert(s => s.AggregateId, doc.AggregateId);

        await _snapshots.UpdateOneAsync(s => s.AggregateId == aggregateId && s.Version <= version, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task<(object? Snapshot, long Version)> GetSnapshotAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snap = await _snapshots.Find(s => s.AggregateId == aggregateId)
                                   .SortByDescending(s => s.Version)
                                   .Limit(1)
                                   .FirstOrDefaultAsync(cancellationToken);

        if (snap == null) return (null, 0L);

        try
        {
            var clr = Type.GetType(snap.SnapshotType);
            if (clr == null) return (snap.Data, snap.Version);

            var obj = BsonSerializer.Deserialize(snap.Data, clr);
            return (obj, snap.Version);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao desserializar snapshot para aggregate {AggregateId}", aggregateId);
            return (snap.Data, snap.Version);
        }
    }

    public async Task<bool> StreamExistsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var count = await _events.CountDocumentsAsync(e => e.AggregateId == aggregateId, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task DeleteStreamAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var evtDelete = await _events.DeleteManyAsync(e => e.AggregateId == aggregateId, cancellationToken);
        var snapDelete = await _snapshots.DeleteManyAsync(s => s.AggregateId == aggregateId, cancellationToken);

        _logger.LogInformation("Deleted stream {AggregateId}: events {EventsDeleted}, snapshots {SnapshotsDeleted}", aggregateId, evtDelete.DeletedCount, snapDelete.DeletedCount);
    }

    private IDomainEvent Deserialize(EventDocument doc)
    {
        var eventType = Type.GetType(doc.EventType, throwOnError: true)!;
        var domainEvent = (IDomainEvent)BsonSerializer.Deserialize(doc.Data, eventType)!;
        return domainEvent;
    }

    /// <summary>
    /// Exceção especializada para conflitos de concorrência no event store.
    /// </summary>
    public sealed class ConcurrencyException : Exception
    {
        public ConcurrencyException() { }
        public ConcurrencyException(string? message) : base(message) { }
        public ConcurrencyException(string? message, Exception? inner) : base(message, inner) { }
    }
}