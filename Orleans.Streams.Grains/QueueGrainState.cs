namespace Orleans.Streams.Grains;

[GenerateSerializer]
[Alias("Orleans.Streams.Grains.QueueGrainState")]
public class QueueGrainState
{
    [Id(0)]
    public long LastReadMessage { get; set; }

    [Id(1)]
    public Queue<GrainsQueueBatchContainer> Messages { get; set; } = new();

    [Id(2)]
    public Queue<GrainsQueueBatchContainer> PendingMessages { get; set; } = new();

    [Id(3)]
    public Queue<GrainsQueueBatchContainer> DroppedMessages { get; set; } = new();

    [Id(4)]
    public Queue<GrainsQueueBatchContainer> ReplayMessages { get; set; } = new();
}
