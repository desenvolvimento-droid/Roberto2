using Domain.Entities.Clientes;
using Domain.Interfaces;
using EventDriven.Marten.Exemple.Infra.Repository;
using Infra.EventStore.Mongo;
using Infra.Repositories.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Infra.Repositories;

public static class Layer
{
    public static IServiceCollection AddEventStoreComMongo(this IServiceCollection services)
    {
       services.AddScoped(typeof(IEventStoreRepository<>), typeof(MongoEventStoreRepository<>));
        return services;
    }
}
