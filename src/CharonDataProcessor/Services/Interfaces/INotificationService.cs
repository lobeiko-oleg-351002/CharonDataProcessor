namespace CharonDataProcessor.Services.Interfaces;

public interface INotificationService
{
    Task NotifyMetricSavedAsync(int metricId, CancellationToken cancellationToken = default);
}

