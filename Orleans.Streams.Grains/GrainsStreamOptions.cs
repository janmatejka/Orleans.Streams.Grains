namespace Orleans.Streams.Grains;

public class GrainsStreamOptions
{
    public const int DefaultMaxStreamNamespaceQueueCount = 5;
    public const int DefaultReplayRetentionBatchCount = 1000;

    public int MaxStreamNamespaceQueueCount { get; set; } = DefaultMaxStreamNamespaceQueueCount;

    public int ReplayRetentionBatchCount { get; set; } = DefaultReplayRetentionBatchCount;

    public GrainsStreamProviderNamespaceQueueOptions[] NamespaceQueue { get; set; } = [];

    public Func<QueueId, Task<IStreamFailureHandler>>? StreamFailureHandlerFactory { get; set; }

    public class GrainsStreamProviderNamespaceQueueOptions
    {
        public required string Namespace { get; set; } = "";

        public int QueueCount { get; set; } = DefaultMaxStreamNamespaceQueueCount;
    }
}
