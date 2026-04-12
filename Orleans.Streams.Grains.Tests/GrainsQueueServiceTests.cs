using NSubstitute;
using Orleans.Runtime;

namespace Orleans.Streams.Grains.Tests;

public class GrainsQueueServiceTests
{
    [Fact]
    public async Task QueueMessageBatchAsync_DelegatesToQueueGrain()
    {
        var queueId = TestHelpers.NewQueueId();
        var clusterClient = Substitute.For<IClusterClient>();
        var queueGrain = Substitute.For<IQueueGrain>();
        clusterClient.GetGrain<IQueueGrain>(queueId.ToString(), null).Returns(queueGrain);
        var mapper = Substitute.For<IStreamQueueMapper>();
        var sut = new GrainsQueueService("provider", mapper, clusterClient);
        var message = TestHelpers.NewBatch(1);

        await sut.QueueMessageBatchAsync(queueId, message);

        await queueGrain.Received(1).QueueMessageBatchAsync(message);
    }

    [Fact]
    public async Task GetQueueMessagesAsync_DelegatesToQueueGrain()
    {
        var queueId = TestHelpers.NewQueueId();
        var clusterClient = Substitute.For<IClusterClient>();
        var queueGrain = Substitute.For<IQueueGrain>();
        var expected = new List<GrainsQueueBatchContainer> { TestHelpers.NewBatch(1) };
        queueGrain.GetQueueMessagesAsync(3).Returns(expected);
        clusterClient.GetGrain<IQueueGrain>(queueId.ToString(), null).Returns(queueGrain);
        var mapper = Substitute.For<IStreamQueueMapper>();
        var sut = new GrainsQueueService("provider", mapper, clusterClient);

        var result = await sut.GetQueueMessagesAsync(queueId, 3);

        Assert.Single(result);
        Assert.Same(expected[0], result[0]);
    }

    [Fact]
    public async Task DeleteQueueMessageAsync_DelegatesToQueueGrain()
    {
        var queueId = TestHelpers.NewQueueId();
        var clusterClient = Substitute.For<IClusterClient>();
        var queueGrain = Substitute.For<IQueueGrain>();
        clusterClient.GetGrain<IQueueGrain>(queueId.ToString(), null).Returns(queueGrain);
        var mapper = Substitute.For<IStreamQueueMapper>();
        var sut = new GrainsQueueService("provider", mapper, clusterClient);
        var message = TestHelpers.NewBatch(1);

        await sut.DeleteQueueMessageAsync(queueId, message);

        await queueGrain.Received(1).DeleteQueueMessageAsync(message);
    }

    [Fact]
    public async Task ShutdownAsync_DelegatesToQueueGrain()
    {
        var queueId = TestHelpers.NewQueueId();
        var clusterClient = Substitute.For<IClusterClient>();
        var queueGrain = Substitute.For<IQueueGrain>();
        clusterClient.GetGrain<IQueueGrain>(queueId.ToString(), null).Returns(queueGrain);
        var mapper = Substitute.For<IStreamQueueMapper>();
        var sut = new GrainsQueueService("provider", mapper, clusterClient);

        await sut.ShutdownAsync(queueId);

        await queueGrain.Received(1).ShutdownAsync();
    }

    [Fact]
    public async Task GetQueueStatusAsync_ReturnsStatusForAllQueues_WhenQueueIsNotSpecified()
    {
        var queueId1 = TestHelpers.NewQueueId("ns1");
        var queueId2 = TestHelpers.NewQueueId("ns2");
        var clusterClient = Substitute.For<IClusterClient>();
        var queueGrain1 = Substitute.For<IQueueGrain>();
        var queueGrain2 = Substitute.For<IQueueGrain>();
        var status1 = new QueueStatus("a", 1, 2, 3, 4);
        var status2 = new QueueStatus("b", 5, 6, 7, 8);
        queueGrain1.GetStatusAsync().Returns(status1);
        queueGrain2.GetStatusAsync().Returns(status2);
        clusterClient.GetGrain<IQueueGrain>(queueId1.ToString(), null).Returns(queueGrain1);
        clusterClient.GetGrain<IQueueGrain>(queueId2.ToString(), null).Returns(queueGrain2);
        var mapper = Substitute.For<IStreamQueueMapper>();
        mapper.GetAllQueues().Returns([queueId1, queueId2]);
        var sut = new GrainsQueueService("provider", mapper, clusterClient);

        var result = await sut.GetQueueStatusAsync();

        Assert.Equal(2, result.Count);
        Assert.Same(status1, result[queueId1]);
        Assert.Same(status2, result[queueId2]);
    }

    [Fact]
    public async Task GetQueueStatusAsync_ReturnsOnlyRequestedQueue_WhenQueueIsSpecified()
    {
        var queueId = TestHelpers.NewQueueId("ns");
        var clusterClient = Substitute.For<IClusterClient>();
        var queueGrain = Substitute.For<IQueueGrain>();
        var status = new QueueStatus("etag", 1, 1, 0, 0);
        queueGrain.GetStatusAsync().Returns(status);
        clusterClient.GetGrain<IQueueGrain>(queueId.ToString(), null).Returns(queueGrain);
        var mapper = Substitute.For<IStreamQueueMapper>();
        var sut = new GrainsQueueService("provider", mapper, clusterClient);

        var result = await sut.GetQueueStatusAsync(queueId);

        Assert.Single(result);
        Assert.Same(status, result[queueId]);
    }
}
