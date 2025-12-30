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
        var message = context.Message;      

        if (message == null)
        {
            return;
        }

        await _processorService.ProcessMetricAsync(message, context.CancellationToken);
    }
}

