using CharonDataProcessor.Models;
using CharonDataProcessor.Services.Interfaces;

namespace CharonDataProcessor.Services;

public class MetricProcessorService : IMetricProcessorService
{
    private readonly ILogger<MetricProcessorService> _logger;

    public MetricProcessorService(ILogger<MetricProcessorService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMetricAsync(MetricMessage metric, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing metric - Type: {Type}, Name: {Name}, Payload: {@Payload}",
            metric.Type,
            metric.Name,
            metric.Payload);

        await Task.Delay(10, cancellationToken);

        _logger.LogInformation(
            "Metric processed successfully - Type: {Type}, Name: {Name}",
            metric.Type,
            metric.Name);
    }
}

