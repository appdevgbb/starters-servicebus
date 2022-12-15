using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

IHost host = Host.CreateDefaultBuilder(args).ConfigureServices((hostContext, services) =>
{
    var simplePublisherSection = hostContext.Configuration.GetSection(SimplePublisherOptions.SectionName);
    if(!simplePublisherSection.Exists())
    {
        throw new Exception($"Missing Configuration Section [{SimplePublisherOptions.SectionName}].");
    }
    services.Configure<SimplePublisherOptions>(simplePublisherSection);

    services.AddSingleton<ServiceBusClient>(p => 
    {
        var simplePublisherOptions = p.GetRequiredService<IOptionsMonitor<SimplePublisherOptions>>();
        return new ServiceBusClient(simplePublisherOptions.CurrentValue.Namespace, new DefaultAzureCredential());
    });

    services.AddSingleton<ServiceBusSender>(p =>
    {
        var simplePublisherOptions = p.GetRequiredService<IOptionsMonitor<SimplePublisherOptions>>();
        var serviceBusClient = p.GetService<ServiceBusClient>();
        return serviceBusClient!.CreateSender(simplePublisherOptions.CurrentValue.QueueName);
    });

    services.AddApplicationInsightsTelemetryWorkerService();
    services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) => {});

    services.AddHostedService<SimplePublisherService>();
})
.Build();

await host.RunAsync();