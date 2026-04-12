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
}
