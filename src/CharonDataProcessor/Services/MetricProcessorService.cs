using CharonDataProcessor.Data;
using CharonDataProcessor.Models;
using CharonDataProcessor.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CharonDataProcessor.Services;

public class MetricProcessorService : IMetricProcessorService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MetricProcessorService> _logger;

    public MetricProcessorService(
        ApplicationDbContext dbContext,
        ILogger<MetricProcessorService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ProcessMetricAsync(MetricMessage metric, CancellationToken cancellationToken = default)
    {
        if (metric == null)
        {
            _logger.LogWarning("Received null metric message");
            return;
        }

        var payloadJson = JsonSerializer.Serialize(metric.Payload ?? new Dictionary<string, object>());

        var metricEntity = new Metric
        {
            Type = metric.Type ?? string.Empty,
            Name = metric.Name ?? string.Empty,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Metrics.Add(metricEntity);
        var savedCount = await _dbContext.SaveChangesAsync(cancellationToken);

        if (savedCount > 0)
        {
            _logger.LogInformation(
                "Metric saved to database - Id: {Id}, Type: {Type}, Name: {Name}",
                metricEntity.Id,
                metric.Type,
                metric.Name);
        }
        else
        {
            _logger.LogWarning(
                "No changes saved to database for metric - Type: {Type}, Name: {Name}",
                metric.Type,
                metric.Name);
        }
    }
}

