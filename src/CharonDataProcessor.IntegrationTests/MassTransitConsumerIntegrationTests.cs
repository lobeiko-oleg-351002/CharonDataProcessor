using CharonDataProcessor.Configuration;
using CharonDataProcessor.Consumers;
using CharonDataProcessor.Services;
using CharonDataProcessor.Services.Interfaces;
using CharonDbContext.Data;
using CharonDbContext.Messages;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace CharonDataProcessor.IntegrationTests;

public class MassTransitConsumerIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer;
    private readonly MsSqlContainer _sqlContainer;
    private ServiceProvider? _serviceProvider;
    private IBusControl? _busControl;
    private ApplicationDbContext? _dbContext;

    public MassTransitConsumerIntegrationTests()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .WithPortBinding(5672, true)
            .Build();

        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_rabbitMqContainer.StartAsync(), _sqlContainer.StartAsync());

        var port = _rabbitMqContainer.GetMappedPublicPort(5672);
        var hostName = "localhost";
        var userName = "guest";
        var password = "guest";

        var maxRetries = 30;
        var retryDelay = TimeSpan.FromMilliseconds(500);
        var connected = false;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = hostName,
                    Port = port,
                    UserName = userName,
                    Password = password,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(2)
                };

                using var connection = await factory.CreateConnectionAsync();
                connected = connection.IsOpen;
                await connection.CloseAsync();
                break;
            }
            catch (Exception) when (i < maxRetries - 1)
            {
                await Task.Delay(retryDelay);
            }
        }

        if (!connected)
        {
            throw new InvalidOperationException($"Failed to connect to RabbitMQ after {maxRetries} attempts");
        }

        var sqlConnectionString = _sqlContainer.GetConnectionString();

        var rabbitMqOptions = new RabbitMqOptions
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            ExchangeName = "metrics",
            QueueName = "metrics.queue"
        };

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(sqlConnectionString));

        services.AddHttpClient();

        services.Configure<RabbitMqOptions>(options =>
        {
            options.HostName = rabbitMqOptions.HostName;
            options.Port = rabbitMqOptions.Port;
            options.UserName = rabbitMqOptions.UserName;
            options.Password = rabbitMqOptions.Password;
            options.ExchangeName = rabbitMqOptions.ExchangeName;
            options.QueueName = rabbitMqOptions.QueueName;
        });

        services.Configure<GatewayOptions>(opts =>
        {
            opts.BaseUrl = "http://localhost:9999"; // dummy URL for integration tests
        });

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMetricProcessorService, MetricProcessorService>();

        services.AddMassTransit(x =>
        {
            x.AddConsumer<MetricMessageConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri($"rabbitmq://{rabbitMqOptions.HostName}:{rabbitMqOptions.Port}"), h =>
                {
                    h.Username(rabbitMqOptions.UserName);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.Message<MetricMessage>(m => m.SetEntityName(rabbitMqOptions.ExchangeName));

                cfg.ReceiveEndpoint(rabbitMqOptions.QueueName, e =>
                {
                    e.ConfigureConsumer<MetricMessageConsumer>(context);
                });
            });
        });

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        await _dbContext.Database.EnsureCreatedAsync();

        _busControl = _serviceProvider.GetRequiredService<IBusControl>();
        await _busControl.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (_busControl != null)
        {
            await _busControl.StopAsync(CancellationToken.None);
        }

        _dbContext?.Dispose();
        _serviceProvider?.Dispose();

        await Task.WhenAll(
            _rabbitMqContainer.DisposeAsync().AsTask(),
            _sqlContainer.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task Consumer_ShouldProcessMessage_WhenMessageIsPublished()
    {
        var publishEndpoint = _serviceProvider!.GetRequiredService<IPublishEndpoint>();

        var message = new MetricMessage
        {
            Type = "motion",
            Name = "Garage",
            Payload = new Dictionary<string, object> { { "motionDetected", true } }
        };

        await publishEndpoint.Publish(message, CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var savedMetric = await _dbContext!.Metrics.FirstOrDefaultAsync();
        savedMetric.Should().NotBeNull();
        savedMetric!.Type.Should().Be("motion");
        savedMetric.Name.Should().Be("Garage");
        savedMetric.PayloadJson.Should().Contain("motionDetected");
    }

    [Fact]
    public async Task Consumer_ShouldProcessMultipleMessages()
    {
        var publishEndpoint = _serviceProvider!.GetRequiredService<IPublishEndpoint>();

        var messages = new[]
        {
            new MetricMessage { Type = "motion", Name = "Garage", Payload = new Dictionary<string, object> { { "value", true } } },
            new MetricMessage { Type = "energy", Name = "Office", Payload = new Dictionary<string, object> { { "kwh", 123.45 } } }
        };

        foreach (var message in messages)
        {
            await publishEndpoint.Publish(message, CancellationToken.None);
        }

        await Task.Delay(TimeSpan.FromSeconds(3));

        var count = await _dbContext!.Metrics.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(2);
    }
}

