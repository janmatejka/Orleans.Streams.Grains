using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans.Providers.Streams.Common;

namespace Orleans.Streams.Grains.Tests;

public class GrainsQueueAdapterReceiverTests
{
    private const int ReplayRetentionBatchCount = 64;

    [Fact]
    public async Task GetQueueMessagesAsync_UsesDefaultPeekCountForNegativeInput()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        service.GetQueueMessagesAsync(queueId, ReplayRetentionBatchCount).Returns(new List<GrainsQueueBatchContainer>());
        var sut = new GrainsQueueAdapterReceiver(queueId, service, ReplayRetentionBatchCount, loggerFactory);

        await sut.GetQueueMessagesAsync(-1);

        await service.Received(1).GetQueueMessagesAsync(queueId, ReplayRetentionBatchCount);
    }

    [Fact]
    public async Task Initialize_DrainsReplayWindowBeforeLiveMessages_AndDropsDuplicates()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var replayFirst = TestHelpers.NewBatch(1, "orders", "a");
        var replaySecond = TestHelpers.NewBatch(2, "orders", "b");
        var liveDuplicate = TestHelpers.NewBatch(2, "orders", "dup");
        var liveFresh = TestHelpers.NewBatch(3, "orders", "fresh");
        service.GetReplayWindowAsync(queueId, ReplayRetentionBatchCount).Returns(
            new GrainsQueueReplayWindow
            {
                Messages = [replayFirst, replaySecond],
                WarmupCutoffToken = replaySecond.SequenceToken
            });
        service.GetQueueMessagesAsync(queueId, 2).Returns(new List<GrainsQueueBatchContainer> { liveDuplicate, liveFresh });
        var sut = new GrainsQueueAdapterReceiver(queueId, service, ReplayRetentionBatchCount, loggerFactory);

        await sut.Initialize(TimeSpan.FromSeconds(5));

        var replayBatch = await sut.GetQueueMessagesAsync(2);

        Assert.Collection(
            replayBatch,
            batch => Assert.Same(replayFirst, batch),
            batch => Assert.Same(replaySecond, batch));
        await service.DidNotReceive().GetQueueMessagesAsync(queueId, 2);

        var liveBatch = await sut.GetQueueMessagesAsync(2);

        Assert.Single(liveBatch);
        Assert.Same(liveFresh, liveBatch[0]);
        await service.Received(1).GetQueueMessagesAsync(queueId, 2);
    }

    [Fact]
    public async Task Initialize_Fails_WhenReplayWindowFetchFails()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        service.GetReplayWindowAsync(queueId, ReplayRetentionBatchCount).Returns<Task<GrainsQueueReplayWindow>>(_ => throw new InvalidOperationException("boom"));
        var sut = new GrainsQueueAdapterReceiver(queueId, service, ReplayRetentionBatchCount, loggerFactory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Initialize(TimeSpan.FromSeconds(5)));
        await service.DidNotReceive().GetQueueMessagesAsync(Arg.Any<QueueId>(), Arg.Any<int>());
    }

    [Fact]
    public async Task MessagesDeliveredAsync_DeletesOnlyDeliveredFinalizedMessages()
    {
        var queueId = TestHelpers.NewQueueId("orders");
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var sut = new GrainsQueueAdapterReceiver(queueId, service, ReplayRetentionBatchCount, loggerFactory);
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
        var sut = new GrainsQueueAdapterReceiver(queueId, service, ReplayRetentionBatchCount, loggerFactory);

        await sut.MessagesDeliveredAsync(new List<IBatchContainer>());

        await service.DidNotReceiveWithAnyArgs().DeleteQueueMessageAsync(default, default!);
    }

    [Fact]
    public async Task Shutdown_PreventsFurtherReads_AndCallsServiceShutdown()
    {
        var queueId = TestHelpers.NewQueueId();
        var service = Substitute.For<IGrainsQueueService>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var sut = new GrainsQueueAdapterReceiver(queueId, service, ReplayRetentionBatchCount, loggerFactory);

        await sut.Shutdown(TimeSpan.FromSeconds(5));
        var result = await sut.GetQueueMessagesAsync(10);

        await service.Received(1).ShutdownAsync(queueId);
        Assert.Empty(result);
    }
}
