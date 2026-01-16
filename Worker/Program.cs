using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Worker;
using Worker.Workers;

// USAR WebApplication.CreateBuilder() ao invés de Host.CreateApplicationBuilder()
var builder = WebApplication.CreateBuilder(args);

// Configurar Hangfire
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(
              builder.Configuration.GetConnectionString("HangfireConnection"),
              new SqlServerStorageOptions
              {
                  CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                  SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                  QueuePollInterval = TimeSpan.Zero,
                  UseRecommendedIsolationLevel = true,
                  DisableGlobalLocks = true
              });
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.ServerName = $"OutboxProcessor-{Environment.MachineName}";
});

OrquestationWorker.AddPublishScheduledEvents();
OrquestationWorker.AddDispatchEventsSchedule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "Worker Outbox Dashboard",
    AppPath = "/"
});

// Health check endpoint
app.MapGet("/", () => "Worker Outbox Service is running");
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// Endpoint para parar/continuar o worker (opcional)
app.MapPost("/worker/pause", async (IHostApplicationLifetime lifetime) =>
{
    await Task.CompletedTask;
    return Results.Ok(new { message = "Worker paused" });
});

app.Run();