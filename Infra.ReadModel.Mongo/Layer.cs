using Domain.Entities.Clientes;
using Domain.Interfaces;
using EventDriven.Marten.Exemple.Infra.Repository;
using Infra.Models;
using Infra.ReadModel.Mongo.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Infra.ReadModel.Mongo;

public static class Layer
{
    public static IServiceCollection AddReadModelComMongo(this IServiceCollection services)
    {
        services.AddScoped(typeof(IReadModelRepository<>), typeof(MongoReadModelRepository<>));

        return services;
    }
}
