using CharonDataProcessor.Services.Interfaces;
using CharonDbContext.Data;
using CharonDbContext.Messages;
using CharonDbContext.Models;
using Microsoft.EntityFrameworkCore;
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
        _logger.LogInformation("===> ProcessMetricAsync CALLED");
        
        if (metric == null)
        {
            _logger.LogWarning("===> Received null metric message");
            return;
        }

        _logger.LogInformation("===> Metric Type: {Type}, Name: {Name}, Payload count: {Count}", 
            metric.Type, metric.Name, metric.Payload?.Count ?? 0);

        var payloadJson = JsonSerializer.Serialize(metric.Payload ?? new Dictionary<string, object>());
        
        _logger.LogInformation("===> PayloadJson: {Json}", payloadJson);

        var metricEntity = new Metric
        {
            Type = metric.Type ?? string.Empty,
            Name = metric.Name ?? string.Empty,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("===> Adding metric to DbContext");
        _dbContext.Metrics.Add(metricEntity);
        
        _logger.LogInformation("===> Calling SaveChangesAsync");
        var savedCount = await _dbContext.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("===> SaveChangesAsync returned: {Count}", savedCount);

        if (savedCount > 0)
        {
            _logger.LogInformation(
                "===> Metric saved to database - Id: {Id}, Type: {Type}, Name: {Name}",
                metricEntity.Id,
                metric.Type,
                metric.Name);

            // Notify Gateway to broadcast via SignalR
            _logger.LogInformation("===> Calling NotifyMetricSavedAsync");
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

