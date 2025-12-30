using CharonDataProcessor.Services.Interfaces;
using CharonDbContext.Messages;
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
        _logger.LogInformation("===> CONSUMER INVOKED! Message received in MetricMessageConsumer");
        
        var message = context.Message;
        
        _logger.LogInformation(
            "===> Processing MetricMessage - Type: {Type}, Name: {Name}, MessageId: {MessageId}",
            message?.Type ?? "NULL",
            message?.Name ?? "NULL",
            context.MessageId);

        if (message == null)
        {
            _logger.LogError("===> Message is NULL!");
            return;
        }

        try
        {
            _logger.LogInformation("===> Calling ProcessMetricAsync");
            await _processorService.ProcessMetricAsync(message, context.CancellationToken);
            
            _logger.LogInformation(
                "===> Successfully processed MetricMessage - Type: {Type}, Name: {Name}",
                message.Type,
                message.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "===> Error processing MetricMessage - Type: {Type}, Name: {Name}",
                message.Type,
                message.Name);
            throw;
        }
    }
}

