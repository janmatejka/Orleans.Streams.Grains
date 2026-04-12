namespace Orleans.Streams.Grains;

public class GrainsQueueOptions
{
    public DeadLetterStrategyType DeadLetterStrategy { get; set; }

    public int ReplayRetentionBatchCount { get; set; } = GrainsStreamOptions.DefaultReplayRetentionBatchCount;
}
