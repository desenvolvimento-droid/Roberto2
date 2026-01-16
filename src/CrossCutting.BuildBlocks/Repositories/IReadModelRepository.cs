namespace EventDriven.Marten.Exemple.Infra.Repository;

public interface IReadModelRepository<TReadModel>
{
    Task<TReadModel?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    IQueryable<TReadModel> GetAll(
        Func<IQueryable<TReadModel>, IQueryable<TReadModel>>? query = null,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        TReadModel model,
        CancellationToken cancellationToken = default);
}

