namespace Orleans.Streams.Grains.Tests;

public class QueueGrainStateTests
{
    [Fact]
    public void ReplayMessages_DefaultsToEmptyQueue()
    {
        var property = typeof(QueueGrainState).GetProperty("ReplayMessages");

        Assert.NotNull(property);

        var state = new QueueGrainState();
        var replayMessages = property!.GetValue(state) as Queue<GrainsQueueBatchContainer>;

        Assert.NotNull(replayMessages);
        Assert.Empty(replayMessages!);
    }
}
