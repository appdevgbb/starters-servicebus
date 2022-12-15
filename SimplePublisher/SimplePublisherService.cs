using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class SimplePublisherService : IHostedService
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<SimplePublisherOptions> _options;

    public SimplePublisherService(IHostApplicationLifetime hostApplicationLifetime, 
                         ServiceBusSender sender,
                         ILogger<SimplePublisherService> logger,
                         IOptionsMonitor<SimplePublisherOptions> options)
    {
        _sender = sender;
        _logger = logger;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started SimplePublisherService.");

        int numMessagesInBatch = 0;
        while(!cancellationToken.IsCancellationRequested)
        {
            var batch = await _sender.CreateMessageBatchAsync();
            
            while(++numMessagesInBatch <= _options.CurrentValue.SendBatchSize && batch.TryAddMessage(new ServiceBusMessage(Guid.NewGuid().ToString())));
            
            await _sender.SendMessagesAsync(batch);
            
            _logger.LogInformation($"Published {batch.Count} messages.");
            
            numMessagesInBatch = 0;

            await Task.Delay(_options.CurrentValue.SendBatchDelayInMs);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopped SimplePublisherService.");
        return Task.CompletedTask;
    }
}