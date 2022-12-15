using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

IHost host = Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
{
    var simpleSubscriberSection = hostContext.Configuration.GetSection(SimpleSubscriberOptions.SectionName);
    if(!simpleSubscriberSection.Exists())
    {
        throw new Exception($"Missing Configuration Section [{SimpleSubscriberOptions.SectionName}].");
    }
    
    services.Configure<SimpleSubscriberOptions>(simpleSubscriberSection);

    services.AddSingleton<ServiceBusClient>(p => 
    {
        var simpleSubscriberOptions = p.GetRequiredService<IOptionsMonitor<SimpleSubscriberOptions>>();
        return new ServiceBusClient(simpleSubscriberOptions.CurrentValue.Namespace, new DefaultAzureCredential());
    });

    services.AddSingleton<ServiceBusProcessor>(p => 
    {
        var simpleSubscriberOptions = p.GetRequiredService<IOptionsMonitor<SimpleSubscriberOptions>>();

        var options = new ServiceBusProcessorOptions
        {
            PrefetchCount = simpleSubscriberOptions.CurrentValue.PrefetchCount,
            MaxConcurrentCalls = simpleSubscriberOptions.CurrentValue.MaxConcurrentCalls
        };

        var serviceBusClient = p.GetService<ServiceBusClient>();
        return serviceBusClient!.CreateProcessor(simpleSubscriberOptions.CurrentValue.QueueName, options);
    });
    
    services.AddApplicationInsightsTelemetryWorkerService();
    services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) => {});

    services.AddHostedService<SimpleSubscriberService>();
})
.Build();

await host.RunAsync();