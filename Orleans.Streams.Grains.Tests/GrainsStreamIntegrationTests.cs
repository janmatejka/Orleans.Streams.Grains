using System.Collections.Concurrent;
using System.Diagnostics;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.Streams.Grains.Tests;

[Collection(ClusterCollection.Name)]
public sealed class GrainsStreamIntegrationTests(ClusterFixture fixture)
{
    [Fact]
    public async Task QueueService_PersistsReplayWindowAndStatusThroughCluster()
    {
        var streamNamespace = $"orders-{Guid.NewGuid():N}";
        var queueId = TestHelpers.NewQueueId(streamNamespace);
        var service = CreateService(streamNamespace);
        var first = TestHelpers.NewBatch(1, streamNamespace, "a");
        var second = TestHelpers.NewBatch(2, streamNamespace, "b");

        await service.QueueMessageBatchAsync(queueId, first);
        await service.QueueMessageBatchAsync(queueId, second);

        var pending = await service.GetQueueMessagesAsync(queueId, 2);

        Assert.Equal(2, pending.Count);

        await service.DeleteQueueMessageAsync(queueId, pending[0]);
        await service.DeleteQueueMessageAsync(queueId, pending[1]);

        var status = await service.GetQueueStatusAsync(queueId);
        var window = await service.GetReplayWindowAsync(queueId, 10);

        Assert.Equal(2, status[queueId].ReplayMessagesCount);
        Assert.Equal(2, window.Messages.Count);
        Assert.Equal(first.StreamId, window.Messages[0].StreamId);
        Assert.Equal(second.StreamId, window.Messages[1].StreamId);
        Assert.Equal(window.Messages[1].SequenceToken.ToString(), window.WarmupCutoffToken!.ToString());
    }

    [Fact]
    public async Task QueueService_TrimsReplayWindowToRetentionCount()
    {
        var streamNamespace = $"orders-{Guid.NewGuid():N}";
        var queueId = TestHelpers.NewQueueId(streamNamespace);
        var service = CreateService(streamNamespace);
        var batches = new[]
        {
            TestHelpers.NewBatch(1, streamNamespace, "a"),
            TestHelpers.NewBatch(2, streamNamespace, "b"),
            TestHelpers.NewBatch(3, streamNamespace, "c")
        };

        foreach (var batch in batches)
        {
            await service.QueueMessageBatchAsync(queueId, batch);
        }

        var pending = await service.GetQueueMessagesAsync(queueId, 3);

        foreach (var batch in pending)
        {
            await service.DeleteQueueMessageAsync(queueId, batch);
        }

        var status = await service.GetQueueStatusAsync(queueId);
        var window = await service.GetReplayWindowAsync(queueId, 10);

        Assert.Equal(2, status[queueId].ReplayMessagesCount);
        Assert.Equal(2, window.Messages.Count);
        Assert.Equal(batches[1].StreamId, window.Messages[0].StreamId);
        Assert.Equal(batches[2].StreamId, window.Messages[1].StreamId);
        Assert.Equal(window.Messages[1].SequenceToken.ToString(), window.WarmupCutoffToken!.ToString());
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(40, 30)]
    public async Task QueueService_ProcessesConcurrentWritersAndReaders(int writerCount, int readerCount)
    {
        const int messagesPerWriter = 4;
        var streamNamespace = $"load-{Guid.NewGuid():N}";
        var queueId = TestHelpers.NewQueueId(streamNamespace);
        var service = CreateService(streamNamespace);
        var expectedPayloads = Enumerable.Range(0, writerCount)
            .SelectMany(writerIndex =>
                Enumerable.Range(0, messagesPerWriter)
                    .Select(messageIndex => $"writer-{writerIndex}-message-{messageIndex}"))
            .ToHashSet();
        var seenCounts = new ConcurrentDictionary<string, int>();
        var processedCount = 0;
        var writersRemaining = writerCount;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stopwatch = Stopwatch.StartNew();

        var readerTasks = Enumerable.Range(0, readerCount)
            .Select(async _ =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (Volatile.Read(ref writersRemaining) == 0 && Volatile.Read(ref processedCount) >= expectedPayloads.Count)
                    {
                        return;
                    }

                    var batches = await service.GetQueueMessagesAsync(queueId, 1);
                    if (batches.Count == 0)
                    {
                        await Task.Delay(10, cts.Token);
                        continue;
                    }

                    foreach (var batch in batches)
                    {
                        var payload = batch.GetEvents<string>().Single().Item1;
                        seenCounts.AddOrUpdate(payload, 1, (_, current) => current + 1);
                        await service.DeleteQueueMessageAsync(queueId, batch);
                        Interlocked.Increment(ref processedCount);
                    }
                }
            })
            .ToArray();

        var writerTasks = Enumerable.Range(0, writerCount)
            .Select(async writerIndex =>
            {
                try
                {
                    for (var messageIndex = 0; messageIndex < messagesPerWriter; messageIndex++)
                    {
                        var payload = $"writer-{writerIndex}-message-{messageIndex}";
                        await service.QueueMessageBatchAsync(
                            queueId,
                            TestHelpers.NewBatch(
                                writerIndex * messagesPerWriter + messageIndex + 1,
                                streamNamespace,
                                payload));

                        if ((messageIndex & 1) == 0)
                        {
                            await Task.Yield();
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref writersRemaining);
                }
            })
            .ToArray();

        await Task.WhenAll(writerTasks);
        await Task.WhenAll(readerTasks);

        stopwatch.Stop();

        var status = await service.GetQueueStatusAsync(queueId);

        Assert.Equal(expectedPayloads.Count, processedCount);
        Assert.Equal(expectedPayloads.Count, seenCounts.Count);
        Assert.Equal(expectedPayloads.OrderBy(value => value), seenCounts.Keys.OrderBy(value => value));
        Assert.All(seenCounts, entry => Assert.Equal(1, entry.Value));
        Assert.Equal(0, status[queueId].MessageCount);
        Assert.Equal(0, status[queueId].PendingMessagesCount);
        Assert.Equal(0, status[queueId].DroppedMessagesCount);
        Assert.Equal(ClusterFixture.ReplayRetentionBatchCount, status[queueId].ReplayMessagesCount);
        Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task QueueService_PersistsStateAcrossDeactivateAndReactivation()
    {
        var streamNamespace = $"lifecycle-{Guid.NewGuid():N}";
        var queueId = TestHelpers.NewQueueId(streamNamespace);
        var service = CreateService(streamNamespace);
        var queue = fixture.Cluster.GrainFactory.GetGrain<IQueueGrain>(queueId.ToString());
        var management = fixture.Cluster.GrainFactory.GetGrain<IManagementGrain>(0);
        var first = TestHelpers.NewBatch(1, streamNamespace, "a");
        var second = TestHelpers.NewBatch(2, streamNamespace, "b");

        await service.QueueMessageBatchAsync(queueId, first);
        await service.QueueMessageBatchAsync(queueId, second);

        var pending = await service.GetQueueMessagesAsync(queueId, 2);

        Assert.Equal(2, pending.Count);

        await service.DeleteQueueMessageAsync(queueId, pending[0]);
        await service.DeleteQueueMessageAsync(queueId, pending[1]);

        var statusBefore = await service.GetQueueStatusAsync(queueId);
        var windowBefore = await service.GetReplayWindowAsync(queueId, 10);
        Assert.NotNull(await management.GetActivationAddress(queue));

        await queue.DeactivateAsync();
        await Task.Delay(100);

        var statusAfter = await service.GetQueueStatusAsync(queueId);
        var windowAfter = await service.GetReplayWindowAsync(queueId, 10);

        await WaitForActivationAddressAsync(management, queue, expectedPresent: true);

        Assert.Equal(statusBefore[queueId].LastReadMessage, statusAfter[queueId].LastReadMessage);
        Assert.Equal(statusBefore[queueId].MessageCount, statusAfter[queueId].MessageCount);
        Assert.Equal(statusBefore[queueId].PendingMessagesCount, statusAfter[queueId].PendingMessagesCount);
        Assert.Equal(statusBefore[queueId].DroppedMessagesCount, statusAfter[queueId].DroppedMessagesCount);
        Assert.Equal(statusBefore[queueId].ReplayMessagesCount, statusAfter[queueId].ReplayMessagesCount);
        Assert.Equal(windowBefore.Messages.Select(message => message.StreamId), windowAfter.Messages.Select(message => message.StreamId));
        Assert.Equal(
            windowBefore.Messages.Select(message => message.SequenceToken.ToString()),
            windowAfter.Messages.Select(message => message.SequenceToken.ToString()));
        Assert.Equal(windowBefore.WarmupCutoffToken?.ToString(), windowAfter.WarmupCutoffToken?.ToString());
    }

    [Fact]
    public async Task StreamProvider_PrimaryAndSecondaryResumeFromSavedCursor()
    {
        var streamProvider = fixture.Cluster.Client.GetStreamProvider(ClusterFixture.ProviderName);
        var stream = streamProvider.GetStream<string>(Guid.NewGuid());
        var payloads = Enumerable.Range(1, 10).Select(index => $"message-{index:00}").ToList();
        StreamSubscriptionHandle<string>? primaryHandle = null;
        StreamSubscriptionHandle<string>? secondaryHandle = null;

        Assert.True(streamProvider.IsRewindable);

        foreach (var payload in payloads)
        {
            await stream.OnNextAsync(payload);
        }

        try
        {
            var primaryReceived = new List<string>();
            var primaryGate = new object();
            var primaryFirstFive = new TaskCompletionSource<StreamSequenceToken>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var primaryAllTen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            primaryHandle = await stream.SubscribeAsync((item, token) =>
            {
                lock (primaryGate)
                {
                    primaryReceived.Add(item);
                    if (primaryReceived.Count == 5)
                    {
                        primaryFirstFive.TrySetResult(token);
                    }

                    if (primaryReceived.Count == payloads.Count)
                    {
                        primaryAllTen.TrySetResult();
                    }
                }

                return Task.CompletedTask;
            }, new EventSequenceTokenV2(0));

            var fifthMessageCursor = await primaryFirstFive.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await primaryAllTen.Task.WaitAsync(TimeSpan.FromSeconds(15));

            Assert.Equal(payloads, primaryReceived);

            var secondaryReceived = new List<string>();
            var secondaryGate = new object();
            var secondaryExpected = payloads.Skip(5).ToList();
            var secondaryFive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondaryStartCursor = CursorAfter(fifthMessageCursor);

            secondaryHandle = await stream.SubscribeAsync((item, _) =>
            {
                lock (secondaryGate)
                {
                    secondaryReceived.Add(item);
                    if (secondaryReceived.Count == secondaryExpected.Count)
                    {
                        secondaryFive.TrySetResult();
                    }
                }

                return Task.CompletedTask;
            }, secondaryStartCursor);

            await secondaryFive.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await Task.Delay(100);

            Assert.Equal(secondaryExpected, secondaryReceived);
        }
        finally
        {
            if (secondaryHandle is not null)
            {
                await secondaryHandle.UnsubscribeAsync();
            }

            if (primaryHandle is not null)
            {
                await primaryHandle.UnsubscribeAsync();
            }
        }
    }

    private static StreamSequenceToken CursorAfter(StreamSequenceToken cursor)
    {
        return cursor switch
        {
            EventSequenceTokenV2 eventSequenceToken => new EventSequenceTokenV2(eventSequenceToken.SequenceNumber + 1),
            _ => cursor
        };
    }

    private GrainsQueueService CreateService(string streamNamespace)
    {
        var mapper = new GrainsStreamQueueMapper(new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1,
            NamespaceQueue =
            [
                new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
                {
                    Namespace = streamNamespace,
                    QueueCount = 1
                }
            ]
        });

        return new GrainsQueueService("integration", mapper, fixture.Cluster.Client);
    }

    private static async Task WaitForActivationAddressAsync(
        IManagementGrain management,
        IAddressable grain,
        bool expectedPresent)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        while (true)
        {
            var address = await management.GetActivationAddress(grain);
            if (expectedPresent ? address is not null : address is null)
            {
                return;
            }

            await Task.Delay(50, cts.Token);
        }
    }
}
