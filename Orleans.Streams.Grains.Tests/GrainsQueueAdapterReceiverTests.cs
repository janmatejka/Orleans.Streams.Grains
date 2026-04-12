using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans.Providers.Streams.Common;

namespace Orleans.Streams.Grains.Tests;

public class GrainsQueueAdapterReceiverTests
{
    [Fact]
    public async Task GetQueueMessagesAsync_UsesDefaultPeekCountForNegativeInput()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        service.GetQueueMessagesAsync(queueId, 32).Returns(new List<GrainsQueueBatchContainer>());
        var sut = new GrainsQueueAdapterReceiver(queueId, service, loggerFactory);

        await sut.GetQueueMessagesAsync(-1);

        await service.Received(1).GetQueueMessagesAsync(queueId, 32);
    }

    [Fact]
    public async Task MessagesDeliveredAsync_DeletesOnlyDeliveredFinalizedMessages()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var sut = new GrainsQueueAdapterReceiver(queueId, service, loggerFactory);
        var first = TestHelpers.NewBatch(1, "orders", "a");
        var second = TestHelpers.NewBatch(2, "orders", "b");
        service.GetQueueMessagesAsync(queueId, 2).Returns(new List<GrainsQueueBatchContainer> { first, second });

        await sut.GetQueueMessagesAsync(2);
        await sut.MessagesDeliveredAsync(new List<IBatchContainer> { second });

        await service.DidNotReceive().DeleteQueueMessageAsync(queueId, first);
        await service.Received(1).DeleteQueueMessageAsync(queueId, second);
    }

    [Fact]
    public async Task MessagesDeliveredAsync_DoesNothing_ForEmptyDeliveryBatch()
    {
        var queueId = TestHelpers.NewQueueId();
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var sut = new GrainsQueueAdapterReceiver(queueId, service, loggerFactory);

        await sut.MessagesDeliveredAsync(new List<IBatchContainer>());

        await service.DidNotReceiveWithAnyArgs().DeleteQueueMessageAsync(default, default!);
    }

    [Fact]
    public async Task Shutdown_PreventsFurtherReads_AndCallsServiceShutdown()
    {
        var queueId = TestHelpers.NewQueueId();
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var sut = new GrainsQueueAdapterReceiver(queueId, service, loggerFactory);

        await sut.Shutdown(TimeSpan.FromSeconds(5));
        var result = await sut.GetQueueMessagesAsync(10);

        await service.Received(1).ShutdownAsync(queueId);
        Assert.Empty(result);
    }
}
