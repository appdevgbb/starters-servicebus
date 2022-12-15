using System.Timers;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Timer = System.Timers.Timer;

internal class SimpleSubscriberService : IHostedService
{
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger _logger;
    private ulong _messageCount = 0;
    private ulong _previousMessageCount = 0;
    private Timer _timer;
    private DateTime? _lastSignalTime;

    public SimpleSubscriberService(IHostApplicationLifetime hostApplicationLifetime, 
                         ServiceBusProcessor processor,
                         ILogger<SimpleSubscriberService> logger,
                         IOptionsMonitor<SimpleSubscriberOptions> options)
    {
        _processor = processor;
        _logger = logger;
        _timer = new Timer(options.CurrentValue.PerfTimerWindowInMs);
        _timer.Elapsed += ( sender, e ) => HandleTimer(e);
        _timer.Start();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting SimpleSubscriberService: PrefetchCount = {_processor.PrefetchCount}, MaxConcurrentCalls = {_processor.MaxConcurrentCalls}");

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync();

        _logger.LogInformation("Started SimpleSubscriberService.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Stopping SimpleSubscriberService...");
        
        await _processor.StopProcessingAsync();

        _processor.ProcessMessageAsync -= MessageHandler;
        _processor.ProcessErrorAsync -= ErrorHandler;

        _logger.LogInformation("Stopped SimpleSubscriberService");
    }

    private Task MessageHandler(ProcessMessageEventArgs arg)
    {
        Interlocked.Increment(ref _messageCount);
        return Task.CompletedTask;
    }

    private Task ErrorHandler(ProcessErrorEventArgs arg)
    {
        _logger.LogError($"Received Error: {arg.Exception}");
        return Task.CompletedTask;
    }

    private void HandleTimer(ElapsedEventArgs e)
    {
        TimeSpan elapsedTime = e.SignalTime - (_lastSignalTime ?? e.SignalTime - TimeSpan.FromMilliseconds(_timer.Interval));
        _lastSignalTime = e.SignalTime;
        var currentMessageCount = Interlocked.Read(ref _messageCount);
        var numMessagesInPeriod = currentMessageCount - _previousMessageCount;
        _previousMessageCount = currentMessageCount;
        
        _logger.LogInformation($"Processed {numMessagesInPeriod} messages in {elapsedTime.TotalMilliseconds}ms ({numMessagesInPeriod / (double)elapsedTime.TotalMilliseconds * 1000:f2}msgs/s)");
    }
}