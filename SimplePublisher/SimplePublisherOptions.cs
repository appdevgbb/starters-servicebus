internal class SimplePublisherOptions
{
    public const string SectionName = "SimplePublisher";

    public string Namespace { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public int SendBatchSize { get; set; } = 100;
    public int SendBatchDelayInMs { get; set; } = 1000;
}