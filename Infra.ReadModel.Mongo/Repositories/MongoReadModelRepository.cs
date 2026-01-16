using Domain.BuildingBlocks.Models;
using EventDriven.Marten.Exemple.Infra.Repository;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Infra.ReadModel.Mongo.Repositories;

public sealed class MongoReadModelRepository<TReadModel>(
    IMongoDatabase database,
    ILogger<MongoReadModelRepository<TReadModel>> logger)
    : IReadModelRepository<TReadModel>
    where TReadModel : class, IReadModel
{
    private readonly IMongoCollection<TReadModel> _collection = database.GetCollection<TReadModel>(
            typeof(TReadModel).Name);

    public async Task<TReadModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<TReadModel> GetAll(
        Func<IQueryable<TReadModel>, IQueryable<TReadModel>>? query = null,
        CancellationToken cancellationToken = default)
    {
        var q = _collection.AsQueryable();
        return query is null ? q : query(q);
    }

    public async Task UpsertAsync(
        TReadModel model,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TReadModel>
            .Filter.Eq(x => x.Id, model.Id);

        await _collection.ReplaceOneAsync(
            filter,
            model,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }
}
