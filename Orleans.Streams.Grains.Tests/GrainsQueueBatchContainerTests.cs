using Orleans.Providers.Streams.Common;

namespace Orleans.Streams.Grains.Tests;

public class GrainsQueueBatchContainerTests
{
    [Fact]
    public void Ctor_Throws_WhenEventsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GrainsQueueBatchContainer(
                TestHelpers.NewStreamId(),
                null,
                new Dictionary<string, object>()));
    }

    [Fact]
    public void GetEvents_FiltersByType_AndReturnsDerivedSequenceTokens()
    {
        var sut = new GrainsQueueBatchContainer(
            TestHelpers.NewStreamId(),
            [1, "skip", 2],
            new Dictionary<string, object>(),
            new EventSequenceTokenV2(42));

        var events = sut.GetEvents<int>().ToList();

        Assert.Equal(2, events.Count);
        Assert.Equal(1, events[0].Item1);
        Assert.Equal(2, events[1].Item1);
        Assert.Equal(new EventSequenceTokenV2(42), events[0].Item2);
        Assert.Equal(new EventSequenceTokenV2(42, 1), events[1].Item2);
    }

    [Fact]
    public void ImportRequestContext_ReturnsFalse_WhenContextIsNull()
    {
        var sut = new GrainsQueueBatchContainer(
            TestHelpers.NewStreamId(),
            [1],
            null);

        var imported = sut.ImportRequestContext();

        Assert.False(imported);
    }

    [Fact]
    public void ToString_ContainsBasicBatchInfo()
    {
        var sut = new GrainsQueueBatchContainer(
            TestHelpers.NewStreamId("orders"),
            [1, 2, 3],
            new Dictionary<string, object>(),
            new EventSequenceTokenV2(7));

        var text = sut.ToString();

        Assert.Contains("GrainsQueueBatchContainer", text);
        Assert.Contains("#Items=3", text);
    }
}
