using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Orleans.Streams.Grains.Tests;

public class GrainsRewindableQueueCacheTests
{
    [Fact]
    public void GetCacheCursor_ReturnsLatestMessagesWithinRetentionWindow()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var logger = Substitute.For<ILogger>();
        var cache = new GrainsRewindableQueueCache(queueId, "provider", 2, logger);
        var streamId = TestHelpers.NewStreamId("orders");
        var first = new GrainsQueueBatchContainer(streamId, ["a"], new Dictionary<string, object>(), new EventSequenceTokenV2(1));
        var second = new GrainsQueueBatchContainer(streamId, ["b"], new Dictionary<string, object>(), new EventSequenceTokenV2(2));
        var third = new GrainsQueueBatchContainer(streamId, ["c"], new Dictionary<string, object>(), new EventSequenceTokenV2(3));

        cache.AddToCache(new List<IBatchContainer> { first, second, third });

        var cursor = cache.GetCacheCursor(streamId, null!);
        var current = cursor.GetCurrent(out var exception);

        Assert.Null(exception);
        Assert.Same(third, current);
        Assert.True(cursor.MoveNext());

        current = cursor.GetCurrent(out exception);
        Assert.Null(exception);
        Assert.Same(second, current);
        Assert.False(cursor.MoveNext());
    }

    [Fact]
    public void GetCacheCursor_ThrowsQueueCacheMissForEvictedToken()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var logger = Substitute.For<ILogger>();
        var cache = new GrainsRewindableQueueCache(queueId, "provider", 2, logger);
        var streamId = TestHelpers.NewStreamId("orders");
        var first = new GrainsQueueBatchContainer(streamId, ["a"], new Dictionary<string, object>(), new EventSequenceTokenV2(1));
        var second = new GrainsQueueBatchContainer(streamId, ["b"], new Dictionary<string, object>(), new EventSequenceTokenV2(2));
        var third = new GrainsQueueBatchContainer(streamId, ["c"], new Dictionary<string, object>(), new EventSequenceTokenV2(3));

        cache.AddToCache(new List<IBatchContainer> { first, second, third });

        Assert.Throws<QueueCacheMissException>(() => cache.GetCacheCursor(streamId, first.SequenceToken));
    }
}
