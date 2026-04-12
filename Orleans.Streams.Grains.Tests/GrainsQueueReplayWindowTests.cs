namespace Orleans.Streams.Grains.Tests;

public class GrainsQueueReplayWindowTests
{
    [Fact]
    public void ReplayWindow_DefaultsToEmptyMessagesAndNullCutoff()
    {
        var type = Type.GetType("Orleans.Streams.Grains.GrainsQueueReplayWindow, Orleans.Streams.Grains");

        Assert.NotNull(type);

        var window = Activator.CreateInstance(type!);
        var messagesProperty = type!.GetProperty("Messages");
        var cutoffProperty = type.GetProperty("WarmupCutoffToken");

        Assert.NotNull(messagesProperty);
        Assert.NotNull(cutoffProperty);

        var messages = messagesProperty!.GetValue(window) as List<GrainsQueueBatchContainer>;

        Assert.NotNull(messages);
        Assert.Empty(messages!);
        Assert.Null(cutoffProperty!.GetValue(window));
    }

    [Fact]
    public void QueueGrainInterface_ExposesGetReplayWindowAsync()
    {
        Assert.NotNull(typeof(IQueueGrain).GetMethod("GetReplayWindowAsync"));
    }

    [Fact]
    public void QueueServiceInterface_ExposesGetReplayWindowAsync()
    {
        Assert.NotNull(typeof(IGrainsQueueService).GetMethod("GetReplayWindowAsync"));
    }

    [Fact]
    public void QueueService_ExposesGetReplayWindowAsync()
    {
        Assert.NotNull(typeof(GrainsQueueService).GetMethod("GetReplayWindowAsync"));
    }
}
