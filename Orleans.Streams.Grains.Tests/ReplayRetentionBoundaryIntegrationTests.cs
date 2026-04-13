using Microsoft.Extensions.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Hosting;
using Orleans.Streams.Grains.Hosting;
using Orleans.TestingHost;

namespace Orleans.Streams.Grains.Tests;

[CollectionDefinition(Name)]
public sealed class ReplayRetentionBoundaryCollection : ICollectionFixture<ReplayRetentionBoundaryFixture>
{
    public const string Name = "ReplayRetentionBoundary";
}

public sealed class ReplayRetentionBoundaryFixture : IDisposable
{
    public const string ProviderName = "ReplayRetentionBoundaryProvider";
    public const int ReplayRetentionBatchCount = 1000;

    public TestCluster Cluster { get; } = new TestClusterBuilder()
        .AddSiloBuilderConfigurator<ReplayRetentionBoundarySiloConfigurator>()
        .AddClientBuilderConfigurator<ReplayRetentionBoundaryClientConfigurator>()
        .Build();

    public ReplayRetentionBoundaryFixture() => Cluster.Deploy();

    void IDisposable.Dispose() => Cluster.StopAllSilos();
}

public sealed class ReplayRetentionBoundarySiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage(GrainsStreamProviderConsts.QueueStorageName);
        siloBuilder.AddMemoryGrainStorage(ReplayRetentionBoundaryFixture.ProviderName);
        siloBuilder.AddGrainsStreams(ReplayRetentionBoundaryFixture.ProviderName, options =>
        {
            options.MaxStreamNamespaceQueueCount = 1;
            options.ReplayRetentionBatchCount = ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount;
        });
    }
}

public sealed class ReplayRetentionBoundaryClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.AddGrainsStreams(ReplayRetentionBoundaryFixture.ProviderName, options =>
        {
            options.MaxStreamNamespaceQueueCount = 1;
            options.ReplayRetentionBatchCount = ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount;
        });
    }
}

[Collection(ReplayRetentionBoundaryCollection.Name)]
public sealed class ReplayRetentionBoundaryIntegrationTests(ReplayRetentionBoundaryFixture fixture)
{
    [Fact]
    public async Task QueueService_PreservesOnlyTheNewestThousandMessagesWhenReplayWindowRotates()
    {
        var streamNamespace = $"boundary-{Guid.NewGuid():N}";
        var queueId = TestHelpers.NewQueueId(streamNamespace);
        var service = CreateService(streamNamespace);

        for (var index = 0; index < ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount + 1; index++)
        {
            await service.QueueMessageBatchAsync(
                queueId,
                TestHelpers.NewBatch(index + 1, streamNamespace, $"message-{index + 1}"));
        }

        var pending = await service.GetQueueMessagesAsync(queueId, ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount + 1);

        Assert.Equal(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount + 1, pending.Count);

        for (var index = 0; index < pending.Count; index++)
        {
            await service.DeleteQueueMessageAsync(queueId, pending[index]);

            if (index == ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount - 2)
            {
                var windowBeforeCapacity = await service.GetReplayWindowAsync(queueId, ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount);

                Assert.Equal(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount - 1, windowBeforeCapacity.Messages.Count);
                Assert.Equal(new EventSequenceTokenV2(0).ToString(), windowBeforeCapacity.Messages[0].SequenceToken.ToString());
                Assert.Equal(new EventSequenceTokenV2(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount - 2).ToString(), windowBeforeCapacity.Messages[^1].SequenceToken.ToString());
            }

            if (index == ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount - 1)
            {
                var windowAtCapacity = await service.GetReplayWindowAsync(queueId, ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount);

                Assert.Equal(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount, windowAtCapacity.Messages.Count);
                Assert.Equal(new EventSequenceTokenV2(0).ToString(), windowAtCapacity.Messages[0].SequenceToken.ToString());
                Assert.Equal(new EventSequenceTokenV2(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount - 1).ToString(), windowAtCapacity.Messages[^1].SequenceToken.ToString());
            }
        }

        var windowAfterRotation = await service.GetReplayWindowAsync(queueId, ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount);

        Assert.Equal(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount, windowAfterRotation.Messages.Count);
        Assert.Equal(new EventSequenceTokenV2(1).ToString(), windowAfterRotation.Messages[0].SequenceToken.ToString());
        Assert.Equal(new EventSequenceTokenV2(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount).ToString(), windowAfterRotation.Messages[^1].SequenceToken.ToString());
        Assert.DoesNotContain(windowAfterRotation.Messages, message => message.SequenceToken.Equals(new EventSequenceTokenV2(0)));
        Assert.Equal(ReplayRetentionBoundaryFixture.ReplayRetentionBatchCount, (await service.GetQueueStatusAsync(queueId))[queueId].ReplayMessagesCount);
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

        return new GrainsQueueService("integration-boundary", mapper, fixture.Cluster.Client);
    }
}
