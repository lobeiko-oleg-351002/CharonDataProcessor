using CharonDataProcessor.Data;
using CharonDataProcessor.Models;
using CharonDataProcessor.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.MsSql;

namespace CharonDataProcessor.IntegrationTests;

public class DatabaseIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private ApplicationDbContext? _dbContext;
    private MetricProcessorService? _service;

    public DatabaseIntegrationTests()
    {
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var connectionString = _sqlContainer.GetConnectionString();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        _dbContext = new ApplicationDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        var loggerMock = new Mock<ILogger<MetricProcessorService>>();
        _service = new MetricProcessorService(_dbContext, loggerMock.Object);
    }

    public async Task DisposeAsync()
    {
        _dbContext?.Dispose();
        await _sqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldPersistToDatabase()
    {
        var metricMessage = new MetricMessage
        {
            Type = "motion",
            Name = "Garage",
            Payload = new Dictionary<string, object> { { "motionDetected", true } }
        };

        await _service!.ProcessMetricAsync(metricMessage);

        var savedMetric = await _dbContext!.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.Type.Should().Be("motion");
        savedMetric.Name.Should().Be("Garage");
        savedMetric.PayloadJson.Should().Contain("motionDetected");
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldHandleMultipleMetrics()
    {
        var metrics = new[]
        {
            new MetricMessage { Type = "motion", Name = "Garage", Payload = new Dictionary<string, object> { { "value", true } } },
            new MetricMessage { Type = "energy", Name = "Office", Payload = new Dictionary<string, object> { { "kwh", 123.45 } } },
            new MetricMessage { Type = "temperature", Name = "Kitchen", Payload = new Dictionary<string, object> { { "celsius", 22.5 } } }
        };

        foreach (var metric in metrics)
        {
            await _service!.ProcessMetricAsync(metric);
        }

        var count = await _dbContext!.Metrics.CountAsync();
        count.Should().Be(3);

        var allMetrics = await _dbContext.Metrics.ToListAsync();
        allMetrics.Should().Contain(m => m.Type == "motion" && m.Name == "Garage");
        allMetrics.Should().Contain(m => m.Type == "energy" && m.Name == "Office");
        allMetrics.Should().Contain(m => m.Type == "temperature" && m.Name == "Kitchen");
    }

    [Fact]
    public async Task ProcessMetricAsync_ShouldSetCreatedAtTimestamp()
    {
        var before = DateTime.UtcNow;
        
        var metricMessage = new MetricMessage
        {
            Type = "sensor",
            Name = "Test",
            Payload = new Dictionary<string, object>()
        };

        await _service!.ProcessMetricAsync(metricMessage);

        var after = DateTime.UtcNow;
        var savedMetric = await _dbContext!.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.CreatedAt.Should().BeAfter(before.AddSeconds(-1));
        savedMetric.CreatedAt.Should().BeBefore(after.AddSeconds(1));
    }
}

