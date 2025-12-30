using CharonDataProcessor.Services.Interfaces;
using CharonDbContext.Data;
using CharonDbContext.Messages;
using CharonDbContext.Models;
using System.Text.Json;

namespace CharonDataProcessor.Services;

public class MetricProcessorService : IMetricProcessorService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MetricProcessorService> _logger;

    public MetricProcessorService(
        ApplicationDbContext dbContext,
        INotificationService notificationService,
        ILogger<MetricProcessorService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ProcessMetricAsync(MetricMessage metric, CancellationToken cancellationToken = default)
    {        
        if (metric == null)
        {
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
            await _notificationService.NotifyMetricSavedAsync(metricEntity.Id, cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "===> No changes saved to database for metric - Type: {Type}, Name: {Name}",
                metric.Type,
                metric.Name);
        }
    }
}

