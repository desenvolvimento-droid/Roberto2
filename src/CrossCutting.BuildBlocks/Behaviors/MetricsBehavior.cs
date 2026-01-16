using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Prometheus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrossCutting.BuildBlocks.Behaviors;

public interface IMetricsCollector
{
    void IncrementCounter(string name, double value = 1);
    void RecordHistogram(string name, double value);
}

public class PrometheusMetricsCollector : IMetricsCollector
{
    public void IncrementCounter(string name, double value = 1)
    {
        var counter = Metrics.CreateCounter(name, $"Contador de {name}");
        counter.Inc(value);
    }

    public void RecordHistogram(string name, double value)
    {
        var histogram = Metrics.CreateHistogram(name, $"Histograma de {name}");
        histogram.Observe(value);
    }
}

public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<MetricsBehavior<TRequest, TResponse>> _logger;
    private readonly IMetricsCollector _metrics; // Interface customizada para métricas

    public MetricsBehavior(ILogger<MetricsBehavior<TRequest, TResponse>> logger,
                           IMetricsCollector metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _metrics.IncrementCounter($"{requestName}_started");

        try
        {
            _logger.LogInformation("Iniciando request {RequestName}", requestName);

            var response = await next(); // Executa o handler

            stopwatch.Stop();
            _metrics.RecordHistogram($"{requestName}_duration_ms", stopwatch.ElapsedMilliseconds);
            _metrics.IncrementCounter($"{requestName}_succeeded");

            _logger.LogInformation("Request {RequestName} concluído em {Elapsed}ms", requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metrics.IncrementCounter($"{requestName}_failed");
            _metrics.RecordHistogram($"{requestName}_duration_ms", stopwatch.ElapsedMilliseconds);

            _logger.LogError(ex, "Request {RequestName} falhou após {Elapsed}ms", requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
