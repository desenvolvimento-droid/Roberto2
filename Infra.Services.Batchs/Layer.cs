using CrossCutting.BuildBlocks.Repositories;
using Hangfire;
using Infra.Dispacher.Hangfire.Services;
using Infra.Persistence.Mongo.Outbox;
using Infra.Services.Batch.OutboxMessage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infra.Repositories;

public static class Layer
{
    /// <summary>
    /// Registra os serviços do publisher e jobs do MassTransit
    /// </summary>
    public static IServiceCollection AddDispacherComHangfire(this IServiceCollection services)
    {
        // Repositório do Outbox
        services.AddScoped<IBatchRepository<BatchModel>, BatchRepository>();

        return services;
    }
}
