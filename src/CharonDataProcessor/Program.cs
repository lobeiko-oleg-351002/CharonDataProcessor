using CharonDataProcessor.Configuration;
using CharonDataProcessor.Consumers;
using CharonDataProcessor.Services;
using CharonDataProcessor.Services.Interfaces;
using MassTransit;
using Microsoft.Extensions.Options;
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

    builder.Services.AddScoped<IMetricProcessorService, MetricProcessorService>();

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

                cfg.ConfigureEndpoints(context);
            }
        });
    });

    var app = builder.Build();
    
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
