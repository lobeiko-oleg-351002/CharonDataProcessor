using CharonDataProcessor.Configuration;
using CharonDataProcessor.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CharonDataProcessor.Services;

public class NotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHttpClientFactory httpClientFactory,
        IOptions<NotificationOptions> options,
        ILogger<NotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyMetricSavedAsync(int metricId, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_options.BaseUrl}/api/notification/metric/{metricId}";
            var response = await httpClient.PostAsync(url, null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully notified notifications service about metric {MetricId}", metricId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to notify notifications service about metric {MetricId}. Status: {StatusCode}",
                    metricId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying notifications service about metric {MetricId}", metricId);
            // Don't throw - notification failure shouldn't break metric processing
        }
    }
}

