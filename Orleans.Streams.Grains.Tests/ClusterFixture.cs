using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Orleans.Streams.Grains;
using Orleans.Streams.Grains.Hosting;
using Orleans.TestingHost;

namespace Orleans.Streams.Grains.Tests;

public sealed class ClusterFixture : IDisposable
{
    public const string ProviderName = "IntegrationGrains";
    public const int ReplayRetentionBatchCount = 2;

    public TestCluster Cluster { get; } = new TestClusterBuilder()
        .AddSiloBuilderConfigurator<GrainsStreamTestSiloConfigurator>()
        .AddClientBuilderConfigurator<GrainsStreamTestClientConfigurator>()
        .Build();

    public ClusterFixture() => Cluster.Deploy();

    void IDisposable.Dispose() => Cluster.StopAllSilos();
}

public sealed class GrainsStreamTestSiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage(GrainsStreamProviderConsts.QueueStorageName);
        siloBuilder.AddMemoryGrainStorage(ClusterFixture.ProviderName);
        siloBuilder.AddGrainsStreams(ClusterFixture.ProviderName, options =>
        {
            options.MaxStreamNamespaceQueueCount = 1;
            options.ReplayRetentionBatchCount = ClusterFixture.ReplayRetentionBatchCount;
        });
    }
}

public sealed class GrainsStreamTestClientConfigurator : IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
    {
        clientBuilder.AddGrainsStreams(ClusterFixture.ProviderName, options =>
        {
            options.MaxStreamNamespaceQueueCount = 1;
            options.ReplayRetentionBatchCount = ClusterFixture.ReplayRetentionBatchCount;
        });
    }
}
