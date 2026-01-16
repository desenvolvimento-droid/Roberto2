using Api.Options;
using CrossCutting.BuildBlocks.Behaviors;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;

#region ConfigureBuilder
var builder = WebApplication.CreateBuilder(args);
#endregion


#region BindConfiguration
var mongoOptions = builder.Configuration.GetSection("Mongo").Get<MongoOptions>() ??
    throw new ArgumentNullException("Mongo configuration section is missing");

var rabbitOptions = builder.Configuration.GetSection("RabbitMQ").Get<RabbitOptions>() ??
    throw new ArgumentNullException("Mongo configuration section is missing");

var otelOptions = builder.Configuration.GetSection("OpenTelemetry").Get<OpenTelemetryOptions>() ??
    new OpenTelemetryOptions("http://otel-collector:4317", 0.1);
#endregion

#region AddApplicationServices
builder.Services.AddSingleton<IMetricsCollector, PrometheusMetricsCollector>();
builder.Services.AddSingleton(new ActivitySource("MyApp.Commands"));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ObservabilityBehavior<,>));

builder.Services.AddSingleton(provider =>
{
    var tracerProvider = Sdk.CreateTracerProviderBuilder()
        .AddSource("MyApi")
        .Build();

    return tracerProvider.GetTracer("MyApi");
});
#endregion

#region AddMassTransitAndRabbitMq
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        // Constrói a Uri do RabbitMQ e usa a sobrecarga que aceita Uri + configurador
        var virtualHostSegment = string.IsNullOrWhiteSpace(rabbitOptions.VirtualHost)
            ? string.Empty
            : "/" + rabbitOptions.VirtualHost.Trim('/');
        var rabbitUri = new Uri($"rabbitmq://{rabbitOptions.Host}:{rabbitOptions.Port}{virtualHostSegment}");

        cfg.Host(rabbitUri, h =>
        {
            h.Username(rabbitOptions.Username);
            h.Password(rabbitOptions.Password);
        });

        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
    });
});
#endregion

#region AddMongoClient
var mongoConnectionString = mongoOptions.ConnectionString ??
    $"mongodb://{mongoOptions.Username}:{mongoOptions.Password}@{mongoOptions.Host}:{mongoOptions.Port}";

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = MongoClientSettings.FromConnectionString(mongoConnectionString);
    settings.RetryWrites = true;
    settings.ConnectTimeout = TimeSpan.FromSeconds(10);
    return new MongoClient(settings);
});
#endregion

#region UseSerilogLogging
builder.Host.UseSerilog((ctx, lc) =>
{
    // mantém console e enrich; se desejar ler configuração Serilog completa, adicione ReadFrom.Configuration(ctx.Configuration)
    lc.WriteTo.Console()
      .Enrich.FromLogContext()
      .Enrich.WithProperty("TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty);
});
#endregion

#region AddOpenTelemetryTracingAndMetrics
// Build TracerProvider (usando Sdk) — compatível com os pacotes já referenciados
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyApi"))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    // .AddMongoDBInstrumentation() // habilitar se adicionar o pacote de instrumentação
    // .AddMassTransitInstrumentation()
    .SetSampler(new TraceIdRatioBasedSampler(otelOptions.TraceSamplingRatio))
    .AddOtlpExporter(o =>
    {
        o.Endpoint = new Uri(otelOptions.OtlpEndpoint);
    })
    .Build();

// Build MeterProvider (usando Sdk)
var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyApi"))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddRuntimeInstrumentation()
    .AddOtlpExporter(o =>
    {
        o.Endpoint = new Uri(otelOptions.OtlpEndpoint);
    })
    .Build();

// Registra providers no DI para permitir injeção e gerenciamento
builder.Services.AddSingleton(tracerProvider);
builder.Services.AddSingleton(meterProvider);
#endregion

#region AddMediatRServices
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
#endregion

#region AddHealthChecksDependencies
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());
// Se preferir habilitar checagens de Mongo/Rabbit, descomente e ajuste conforme pacotes disponíveis:
// .AddMongoDb(mongoConnectionString, name: "MongoDB")
// .AddRabbitMQ($"amqp://{rabbitOptions.Username}:{rabbitOptions.Password}@{rabbitOptions.Host}:{rabbitOptions.Port}", name: "RabbitMQ");
#endregion

#region AddControllersAndSwagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
#endregion

#region BuildApplication
var app = builder.Build();
#endregion

#region UseHttpPipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "docs/{documentName}/swagger.json";
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/docs/v1/swagger.json", "My API V1");
        c.RoutePrefix = "docs";
    });
}

app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();
#endregion

#region MapObservabilityEndpoints
app.MapHealthChecks("/health");
#endregion

#region RegisterApplicationLifetime
app.Lifetime.ApplicationStarted.Register(() =>
{
    Log.Information("Aplicação iniciada com sucesso.");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Aplicação está sendo finalizada...");

    try
    {
        // Flush e dispose dos providers para garantir envio dos spans/metrics pendentes
        tracerProvider?.ForceFlush();
        tracerProvider?.Dispose();
    }
    catch { }

    try
    {
        meterProvider?.Dispose();
    }
    catch { }
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Aplicação finalizada.");
});
#endregion

#region RunApplication
app.Run();
#endregion
