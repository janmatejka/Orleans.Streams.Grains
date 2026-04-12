using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans.Runtime;

namespace Orleans.Streams.Grains.Tests;

public class GrainsQueueAdapterTests
{
    [Fact]
    public void Ctor_InitializesPublicProperties()
    {
        var mapper = Substitute.For<IStreamQueueMapper>();
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();

        var sut = new GrainsQueueAdapter(mapper, service, loggerFactory, "provider");

        Assert.Equal("provider", sut.Name);
        Assert.True(sut.IsRewindable);
        Assert.Equal(StreamProviderDirection.ReadWrite, sut.Direction);
    }

    [Fact]
    public async Task QueueMessageBatchAsync_MapsQueueAndDelegatesToService()
    {
        var mapper = Substitute.For<IStreamQueueMapper>();
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var sut = new GrainsQueueAdapter(mapper, service, loggerFactory, "provider");
        var streamId = TestHelpers.NewStreamId("orders");
        var queueId = TestHelpers.NewQueueId("orders");
        mapper.GetQueueForStream(streamId).Returns(queueId);
        var payload = new[] { 1, 2, 3 };

        await sut.QueueMessageBatchAsync(streamId, payload, null!, new Dictionary<string, object>());

        await service.Received(1).QueueMessageBatchAsync(
            queueId,
            Arg.Is<GrainsQueueBatchContainer>(batch => batch.StreamId.Equals(streamId)));
    }

    [Fact]
    public void CreateReceiver_ReturnsReceiver()
    {
        var mapper = Substitute.For<IStreamQueueMapper>();
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var sut = new GrainsQueueAdapter(mapper, service, loggerFactory, "provider");

        var receiver = sut.CreateReceiver(TestHelpers.NewQueueId());

        Assert.IsType<GrainsQueueAdapterReceiver>(receiver);
    }
}
