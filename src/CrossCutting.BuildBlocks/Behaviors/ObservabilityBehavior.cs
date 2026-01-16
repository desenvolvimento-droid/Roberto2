using MediatR;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;

namespace CrossCutting.BuildBlocks.Behaviors;

public class ObservabilityBehavior<TCommand, TResponse> : IPipelineBehavior<TCommand, TResponse>
    where TCommand : IRequest<TResponse>
{
    private readonly ILogger<ObservabilityBehavior<TCommand, TResponse>> _logger;
    private readonly IMetricsCollector _metrics;
    private readonly ActivitySource _activitySource;

    public ObservabilityBehavior(
        ILogger<ObservabilityBehavior<TCommand, TResponse>> logger,
        IMetricsCollector metrics,
        ActivitySource activitySource)
    {
        _logger = logger;
        _metrics = metrics;
        _activitySource = activitySource;
    }

    public async Task<TResponse> Handle(TCommand command, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var commandName = typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        // Incrementa contador de commands iniciados
        _metrics.IncrementCounter($"{commandName}_started");

        using var activity = _activitySource.StartActivity(commandName, ActivityKind.Internal);
        activity?.SetTag("command.type", commandName);

        try
        {
            _logger.LogInformation("Executando command {CommandName}", commandName);

            var response = await next(); // Executa o handler

            stopwatch.Stop();

            _metrics.RecordHistogram($"{commandName}_duration_ms", stopwatch.ElapsedMilliseconds);
            _metrics.IncrementCounter($"{commandName}_succeeded");

            activity?.SetTag("status", "success");
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

            _logger.LogInformation("Command {CommandName} concluído em {Elapsed}ms", commandName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _metrics.IncrementCounter($"{commandName}_failed");
            _metrics.RecordHistogram($"{commandName}_duration_ms", stopwatch.ElapsedMilliseconds);

            activity?.SetTag("status", "failed");
            activity?.SetTag("exception", ex.GetType().Name);
            activity?.SetTag("duration_ms", stopwatch.ElapsedMilliseconds);

            _logger.LogError(ex, "Command {CommandName} falhou após {Elapsed}ms", commandName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
