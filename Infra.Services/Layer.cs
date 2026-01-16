using Common.Services;
using CrossCutting.BuildBlocks.Services;
using Hangfire;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Infra.Services;

public static class Layer
{
    public static IServiceCollection AddMassTransitMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(Policy.Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<SampleConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"], h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint("cliente-criado-queue", e =>
                {
                    e.ConfigureConsumer<SampleConsumer>(context);

                    // Retry MassTransit
                    e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(2)));

                    // DLQ automático MassTransit
                    e.DiscardFaultedMessages(); // ou use fila _error nativa
                });
            });
        });

        // Outbox + Publisher
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IPublisherEventService, MassTransientEventService>();
        services.AddScoped<OutboxPublisherJob>();

        RecurringJob.AddOrUpdate<OutboxPublisherJob>(
            "outbox-masstransit-publisher",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Minutely);

        return services;
    }
}
