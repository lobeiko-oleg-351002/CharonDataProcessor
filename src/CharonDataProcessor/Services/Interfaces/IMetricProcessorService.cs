using CharonDbContext.Messages;

namespace CharonDataProcessor.Services.Interfaces;

public interface IMetricProcessorService
{
    Task ProcessMetricAsync(MetricMessage metric, CancellationToken cancellationToken = default);
}

