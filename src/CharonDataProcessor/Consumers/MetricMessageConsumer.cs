using CharonDataProcessor.Models;
using CharonDataProcessor.Services.Interfaces;
using MassTransit;

namespace CharonDataProcessor.Consumers;

public class MetricMessageConsumer : IConsumer<MetricMessage>
{
    private readonly IMetricProcessorService _processorService;
    private readonly ILogger<MetricMessageConsumer> _logger;

    public MetricMessageConsumer(
        IMetricProcessorService processorService,
        ILogger<MetricMessageConsumer> logger)
    {
        _processorService = processorService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MetricMessage> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Processing MetricMessage - Type: {Type}, Name: {Name}",
            message.Type,
            message.Name);

        try
        {
            await _processorService.ProcessMetricAsync(message, context.CancellationToken);
            
            _logger.LogInformation(
                "Successfully processed MetricMessage - Type: {Type}, Name: {Name}",
                message.Type,
                message.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing MetricMessage - Type: {Type}, Name: {Name}",
                message.Type,
                message.Name);
            throw;
        }
    }
}

