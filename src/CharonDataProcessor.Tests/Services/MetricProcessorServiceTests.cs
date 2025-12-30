using CharonDataProcessor.Services;
using CharonDataProcessor.Services.Interfaces;
using CharonDbContext.Data;
using CharonDbContext.Messages;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace CharonDataProcessor.Tests.Services;

public class MetricProcessorServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<MetricProcessorService>> _loggerMock;
    private readonly MetricProcessorService _service;

    public MetricProcessorServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<MetricProcessorService>>();
        _service = new MetricProcessorService(_dbContext, _notificationServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldSaveMetric_WhenMetricIsValid()
    {
        var metricMessage = new MetricMessage
        {
            Type = "motion",
            Name = "Garage",
            Payload = new Dictionary<string, object> { { "motionDetected", true } }
        };

        await _service.ProcessMetricAsync(metricMessage);

        var savedMetric = await _dbContext.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.Type.Should().Be("motion");
        savedMetric.Name.Should().Be("Garage");
        savedMetric.PayloadJson.Should().Contain("motionDetected");
        savedMetric.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldNotSave_WhenMetricIsNull()
    {
        await _service.ProcessMetricAsync(null!);

        var count = await _dbContext.Metrics.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldHandleEmptyPayload()
    {
        var metricMessage = new MetricMessage
        {
            Type = "energy",
            Name = "Office",
            Payload = new Dictionary<string, object>()
        };

        await _service.ProcessMetricAsync(metricMessage);

        var savedMetric = await _dbContext.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.PayloadJson.Should().Be("{}");
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldHandleNullPayload()
    {
        var metricMessage = new MetricMessage
        {
            Type = "temperature",
            Name = "Kitchen",
            Payload = null!
        };

        await _service.ProcessMetricAsync(metricMessage);

        var savedMetric = await _dbContext.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.PayloadJson.Should().Be("{}");
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldHandleNullTypeAndName()
    {
        var metricMessage = new MetricMessage
        {
            Type = null!,
            Name = null!,
            Payload = new Dictionary<string, object> { { "value", 123 } }
        };

        await _service.ProcessMetricAsync(metricMessage);

        var savedMetric = await _dbContext.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.Type.Should().BeEmpty();
        savedMetric.Name.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldSerializeComplexPayload()
    {
        var metricMessage = new MetricMessage
        {
            Type = "sensor",
            Name = "LivingRoom",
            Payload = new Dictionary<string, object>
            {
                { "temperature", 22.5 },
                { "humidity", 45 },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            }
        };

        await _service.ProcessMetricAsync(metricMessage);

        var savedMetric = await _dbContext.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.PayloadJson.Should().Contain("temperature");
        savedMetric.PayloadJson.Should().Contain("22.5");
        savedMetric.PayloadJson.Should().Contain("humidity");
        savedMetric.PayloadJson.Should().Contain("45");
    }
}

