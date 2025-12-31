using CharonDataProcessor.Configuration;
using CharonDataProcessor.Consumers;
using CharonDataProcessor.Middleware;
using CharonDataProcessor.Middleware.Interfaces;
using CharonDataProcessor.Services;
using CharonDataProcessor.Services.Decorators;
using CharonDataProcessor.Services.Interfaces;
using CharonDbContext.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "CharonDataProcessor")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/charon-data-processor-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Charon Data Processor");

    var builder = WebApplication.CreateBuilder(args);
    
    builder.Host.UseSerilog();
    
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.Configure<RabbitMqOptions>(
        builder.Configuration.GetSection(RabbitMqOptions.SectionName));
    builder.Services.Configure<DatabaseOptions>(
        builder.Configuration.GetSection(DatabaseOptions.SectionName));
    builder.Services.Configure<GatewayOptions>(
        builder.Configuration.GetSection(GatewayOptions.SectionName));
    builder.Services.Configure<NotificationOptions>(
        builder.Configuration.GetSection(NotificationOptions.SectionName));

    var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
    if (!string.IsNullOrEmpty(databaseOptions?.ConnectionString))
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(databaseOptions.ConnectionString);
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
    }

    builder.Services.AddHttpClient();
    builder.Services.AddScoped<ILoggingService, LoggingService>();
    builder.Services.AddScoped<IExceptionHandlingService, ExceptionHandlingService>();
    builder.Services.AddScoped<INotificationService, NotificationService>();

    builder.Services.AddScoped<MetricProcessorService>();
    builder.Services.AddScoped<IMetricProcessorService>(serviceProvider =>
    {
        var inner = serviceProvider.GetRequiredService<MetricProcessorService>();
        var exceptionHandling = serviceProvider.GetRequiredService<IExceptionHandlingService>();
        return new MetricProcessorServiceDecorator(inner, exceptionHandling);
    });

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<MetricMessageConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            var options = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();
            if (options != null)
            {
                
                cfg.Host(new Uri($"rabbitmq://{options.HostName}:{options.Port}"), h =>
                {
                    h.Username(options.UserName);
                    h.Password(options.Password);
                });

                // Let MassTransit auto-configure the endpoint based on the message type
                cfg.ConfigureEndpoints(context);
            }
        });
    });

    var app = builder.Build();

    // Apply database migrations
    if (!string.IsNullOrEmpty(databaseOptions?.ConnectionString))
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                Log.Information("Applying database migrations...");
                await dbContext.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to apply database migrations: {Error}", ex.Message);
            }
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Charon Data Processor API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    
    Log.Information("Charon Data Processor started successfully");
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Charon Data Processor terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
