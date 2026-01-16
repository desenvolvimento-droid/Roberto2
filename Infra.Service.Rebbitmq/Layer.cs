using CrossCutting.BuildBlocks.Repositories;
using CrossCutting.BuildBlocks.Services;
using Hangfire;
using Infra.Persistence.Mongo.Outbox;
using Infra.Publisher.MassTransit.Services;
using Infra.Services.Batch.OutboxMessage;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Infra.Repositories;

public static class Layer
{
    /// <summary>
    /// Registra os serviços do publisher e jobs do MassTransit
    /// </summary>
    public static IServiceCollection AddPublisherComMassTransit(this IServiceCollection services)
    {
        // Repositório do Outbox
        services.AddScoped<IBatchRepository<BatchModel>, BatchRepository>();

        return services;
    }

    /// <summary>
    /// Método para agendar jobs do Hangfire na inicialização
    /// </summary>
    public static void UsePublisherMassTransitJobs(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    }
}
