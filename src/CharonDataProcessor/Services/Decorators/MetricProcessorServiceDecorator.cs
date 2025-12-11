using CharonDataProcessor.Middleware.Interfaces;
using CharonDataProcessor.Models;
using CharonDataProcessor.Services.Interfaces;

namespace CharonDataProcessor.Services.Decorators;

public class MetricProcessorServiceDecorator : IMetricProcessorService
{
    private readonly IMetricProcessorService _inner;
    private readonly IExceptionHandlingService _exceptionHandling;

    public MetricProcessorServiceDecorator(
        IMetricProcessorService inner,
        IExceptionHandlingService exceptionHandling)
    {
        _inner = inner;
        _exceptionHandling = exceptionHandling;
    }

    public async Task ProcessMetricAsync(Models.MetricMessage metric, CancellationToken cancellationToken = default)
    {
        await _exceptionHandling.ExecuteAsync(
            async () => await _inner.ProcessMetricAsync(metric, cancellationToken),
            $"{nameof(ProcessMetricAsync)} (Type: {metric?.Type}, Name: {metric?.Name})",
            cancellationToken);
    }
}

