using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.Streams.Grains.Tests;

public class QueueGrainTests
{
    [Fact]
    public async Task QueueMessageBatchAsync_AssignsTokenAndEnqueuesMessage()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var state = new QueueGrainState();
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Log });
        var sut = new QueueGrain(persistent, logger, options);
        var message = new GrainsQueueBatchContainer(
            TestHelpers.NewStreamId(),
            [1],
            new Dictionary<string, object>());

        await sut.QueueMessageBatchAsync(message);

        Assert.Single(state.Messages);
        Assert.NotNull(message.SequenceToken);
        Assert.Equal(1, state.LastReadMessage);
    }

    [Fact]
    public async Task GetQueueMessagesAsync_MovesMessagesToPending()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var first = TestHelpers.NewBatch(1);
        var second = TestHelpers.NewBatch(2);
        var state = new QueueGrainState();
        state.Messages.Enqueue(first);
        state.Messages.Enqueue(second);
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Log });
        var sut = new QueueGrain(persistent, logger, options);

        var result = await sut.GetQueueMessagesAsync(1);

        Assert.Single(result);
        Assert.Same(first, result[0]);
        Assert.Single(state.Messages);
        Assert.Single(state.PendingMessages);
    }

    [Fact]
    public async Task DeleteQueueMessageAsync_DeadLetterStrategy_StoresDroppedMessage()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var pending1 = TestHelpers.NewBatch(1);
        var pending2 = TestHelpers.NewBatch(2);
        var state = new QueueGrainState();
        state.PendingMessages.Enqueue(pending1);
        state.PendingMessages.Enqueue(pending2);
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.DeadLetterQueue });
        var sut = new QueueGrain(persistent, logger, options);

        await sut.DeleteQueueMessageAsync(pending2);

        Assert.Single(state.DroppedMessages);
        Assert.Empty(state.PendingMessages);
    }

    [Fact]
    public async Task DeleteQueueMessageAsync_RequeueStrategy_RequeuesMessage()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var pending1 = TestHelpers.NewBatch(1);
        var pending2 = TestHelpers.NewBatch(2);
        var state = new QueueGrainState();
        state.PendingMessages.Enqueue(pending1);
        state.PendingMessages.Enqueue(pending2);
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Requeue });
        var sut = new QueueGrain(persistent, logger, options);

        await sut.DeleteQueueMessageAsync(pending2);

        Assert.Single(state.Messages);
        Assert.Empty(state.PendingMessages);
    }

    [Fact]
    public async Task DeleteQueueMessageAsync_DropsGapAndAppendsMatchedMessageToReplayMessages()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var first = TestHelpers.NewBatch(1, "orders", "a");
        var second = TestHelpers.NewBatch(2, "orders", "b");
        var state = new QueueGrainState();
        state.PendingMessages.Enqueue(first);
        state.PendingMessages.Enqueue(second);
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Log });
        var sut = new QueueGrain(persistent, logger, options);

        await sut.DeleteQueueMessageAsync(second);

        Assert.Empty(state.Messages);
        Assert.Empty(state.DroppedMessages);
        Assert.Single(state.ReplayMessages);
        Assert.Same(second, state.ReplayMessages.Peek());
        Assert.Empty(state.PendingMessages);
        await persistent.Received(1).WriteStateAsync();
    }

    [Fact]
    public async Task DeleteQueueMessageAsync_NormalizesEventIndexBeforeMatching()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var pending = TestHelpers.NewBatch(1, "orders", "a");
        var state = new QueueGrainState();
        state.PendingMessages.Enqueue(pending);
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Log });
        var sut = new QueueGrain(persistent, logger, options);
        var targetToken = new EventSequenceTokenV2(1).CreateSequenceTokenForEvent(1);
        var target = new GrainsQueueBatchContainer(
            TestHelpers.NewStreamId("orders"),
            ["b"],
            new Dictionary<string, object>(),
            targetToken);

        await sut.DeleteQueueMessageAsync(target);

        Assert.Single(state.ReplayMessages);
        Assert.Same(pending, state.ReplayMessages.Peek());
        Assert.Empty(state.PendingMessages);
        await persistent.Received(1).WriteStateAsync();
    }

    [Fact]
    public async Task DeleteQueueMessageAsync_TrimsReplayMessagesToRetentionCount()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var existingReplay = TestHelpers.NewBatch(1, "orders", "a");
        var pending = TestHelpers.NewBatch(2, "orders", "b");
        var state = new QueueGrainState();
        state.ReplayMessages.Enqueue(existingReplay);
        state.PendingMessages.Enqueue(pending);
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions
        {
            DeadLetterStrategy = DeadLetterStrategyType.Log,
            ReplayRetentionBatchCount = 1
        });
        var sut = new QueueGrain(persistent, logger, options);

        await sut.DeleteQueueMessageAsync(pending);

        Assert.Single(state.ReplayMessages);
        Assert.Same(pending, state.ReplayMessages.Peek());
        await persistent.Received(1).WriteStateAsync();
    }

    [Fact]
    public async Task DeleteQueueMessageAsync_IsIdempotent_ForRepeatedDelete()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var pending = TestHelpers.NewBatch(1, "orders", "a");
        var state = new QueueGrainState();
        state.PendingMessages.Enqueue(pending);
        persistent.State.Returns(state);
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Log });
        var sut = new QueueGrain(persistent, logger, options);

        await sut.DeleteQueueMessageAsync(pending);
        await sut.DeleteQueueMessageAsync(pending);

        Assert.Single(state.ReplayMessages);
        Assert.Same(pending, state.ReplayMessages.Peek());
        await persistent.Received(1).WriteStateAsync();
    }

    [Fact]
    public async Task ShutdownAsync_WritesPersistentState()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        persistent.State.Returns(new QueueGrainState());
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Log });
        var sut = new QueueGrain(persistent, logger, options);

        await sut.ShutdownAsync();

        await persistent.Received(1).WriteStateAsync();
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsStateSnapshot()
    {
        var persistent = Substitute.For<IPersistentState<QueueGrainState>>();
        var state = new QueueGrainState { LastReadMessage = 11 };
        state.Messages.Enqueue(TestHelpers.NewBatch(1));
        state.PendingMessages.Enqueue(TestHelpers.NewBatch(2));
        state.DroppedMessages.Enqueue(TestHelpers.NewBatch(3));
        state.ReplayMessages.Enqueue(TestHelpers.NewBatch(4));
        persistent.State.Returns(state);
        persistent.Etag.Returns("etag-1");
        var logger = Substitute.For<ILogger<QueueGrain>>();
        var options = Options.Create(new GrainsQueueOptions { DeadLetterStrategy = DeadLetterStrategyType.Log });
        var sut = new QueueGrain(persistent, logger, options);

        var status = await sut.GetStatusAsync();

        Assert.Equal("etag-1", status.ETag);
        Assert.Equal(11, status.LastReadMessage);
        Assert.Equal(1, status.MessageCount);
        Assert.Equal(1, status.PendingMessagesCount);
        Assert.Equal(1, status.DroppedMessagesCount);
        Assert.Equal(1, status.ReplayMessagesCount);
    }
}
