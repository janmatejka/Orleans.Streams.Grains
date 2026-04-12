using Orleans.Runtime;

namespace Orleans.Streams.Grains.Tests;

public class GrainsStreamQueueMapperTests
{
    [Fact]
    public void Ctor_Throws_WhenMaxStreamNamespaceQueueCountIsInvalid()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 0
        };

        Assert.Throws<ArgumentException>(() => new GrainsStreamQueueMapper(options));
    }

    [Fact]
    public void GetAllQueues_ReturnsQueuesFromDefaultAndNamespaceSpecificConfig()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 2,
            NamespaceQueue =
            [
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = "orders",
                    QueueCount = 3
                }
            ]
        };
        var sut = new GrainsStreamQueueMapper(options);

        var result = sut.GetAllQueues().ToList();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void GetQueueForStream_ReturnsQueue_ForKnownNamespace()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1,
            NamespaceQueue =
            [
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = "orders",
                    QueueCount = 1
                }
            ]
        };
        var sut = new GrainsStreamQueueMapper(options);

        var queue = sut.GetQueueForStream(StreamId.Create("orders", Guid.NewGuid()));

        Assert.False(queue.Equals(default(QueueId)));
    }

    [Fact]
    public void GetQueueForStream_Throws_ForUnknownNamespace()
    {
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1
        };
        var sut = new GrainsStreamQueueMapper(options);

        Assert.Throws<ArgumentException>(() => sut.GetQueueForStream(StreamId.Create("unknown", Guid.NewGuid())));
    }
}
