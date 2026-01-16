using System;
using Domain.Interfaces;
using Infra.EventStore.Mongo.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Infra.Repositories;

public static class EventStoreMongoLayer
{
    // Registra IMongoClient, IMongoDatabase e o repositório de EventStore.
    // Espera configurações via IConfiguration ou variáveis de ambiente:
    // - MONGO_CONNECTION_STRING (ex: mongodb+srv://... ou mongodb://user:pass@host)
    // - MONGO_DATABASE
    // - MONGO_MAX_POOL (opcional)
    public static IServiceCollection AddEventStoreComMongo(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("MONGO_CONNECTION_STRING")
                               ?? Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING")
                               ?? "mongodb://localhost:27017";

        var databaseName = configuration.GetValue<string>("MONGO_DATABASE")
                          ?? Environment.GetEnvironmentVariable("MONGO_DATABASE")
                          ?? "appdb";

        var maxPool = configuration.GetValue<int?>("MONGO_MAX_POOL") ?? 100;

        var mongoSettings = MongoClientSettings.FromConnectionString(connectionString);
        // Boas práticas de produção
        mongoSettings.RetryWrites = true;
        mongoSettings.MaxConnectionPoolSize = maxPool;
        mongoSettings.ReadPreference = ReadPreference.PrimaryPreferred;
        mongoSettings.ReadConcern = ReadConcern.Majority;
        mongoSettings.WriteConcern = WriteConcern.WMajority;
        mongoSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
        mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        // TLS/SSL é respeitado automaticamente se string contém 'mongodb+srv' ou tls param; adicionalmente, confirme nas políticas da infra.
        // Para client-side field level encryption, configure kmsProviders e AutoEncryptionOptions aqui (não incluído no MVP).

        var client = new MongoClient(mongoSettings);

        services.AddSingleton<IMongoClient>(client);
        services.AddSingleton(sp => client.GetDatabase(databaseName));

        // Registro do repositório genérico
        services.AddScoped(typeof(IEventStoreRepository<>), typeof(MongoEventStoreRepository<>));

        // opcional: registrar serviços auxiliares de auditoria / health-checks se necessário
        return services;
    }
}
