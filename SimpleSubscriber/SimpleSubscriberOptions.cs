internal class SimpleSubscriberOptions
{
    public const string SectionName = "SimpleSubscriber";

    public string Namespace { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public int PrefetchCount { get; set; } = 0;
    public int MaxConcurrentCalls { get; set; } = 16;
    public int PerfTimerWindowInMs { get; set; } = 1000;
}