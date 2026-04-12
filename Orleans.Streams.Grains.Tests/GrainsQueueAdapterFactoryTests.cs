using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;

namespace Orleans.Streams.Grains.Tests;

public class GrainsQueueAdapterFactoryTests
{
    [Fact]
    public async Task GetDeliveryFailureHandler_UsesDefaultNoOpHandler_AfterInit()
    {
        var clusterClient = Substitute.For<IClusterClient>();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var options = new GrainsStreamOptions { MaxStreamNamespaceQueueCount = 1 };
        var cacheOptions = new SimpleQueueCacheOptions();
        var sut = new GrainsQueueAdapterFactory("provider", options, cacheOptions, clusterClient, loggerFactory);
        sut.Init();

        var handler = await sut.GetDeliveryFailureHandler(TestHelpers.NewQueueId());

        Assert.IsType<NoOpStreamDeliveryFailureHandler>(handler);
    }

    [Fact]
    public async Task GetDeliveryFailureHandler_UsesConfiguredFactory()
    {
        var clusterClient = Substitute.For<IClusterClient>();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var expectedHandler = Substitute.For<IStreamFailureHandler>();
        var options = new GrainsStreamOptions
        {
            MaxStreamNamespaceQueueCount = 1,
            StreamFailureHandlerFactory = _ => Task.FromResult(expectedHandler)
        };
        var cacheOptions = new SimpleQueueCacheOptions();
        var sut = new GrainsQueueAdapterFactory("provider", options, cacheOptions, clusterClient, loggerFactory);
        sut.Init();

        var handler = await sut.GetDeliveryFailureHandler(TestHelpers.NewQueueId());

        Assert.Same(expectedHandler, handler);
    }

    [Fact]
    public async Task CreateAdapter_ReturnsGrainsQueueAdapter()
    {
        var clusterClient = Substitute.For<IClusterClient>();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var options = new GrainsStreamOptions { MaxStreamNamespaceQueueCount = 1 };
        var cacheOptions = new SimpleQueueCacheOptions();
        var sut = new GrainsQueueAdapterFactory("provider", options, cacheOptions, clusterClient, loggerFactory);
        sut.Init();

        var adapter = await sut.CreateAdapter();

        Assert.IsType<GrainsQueueAdapter>(adapter);
    }

    [Fact]
    public void ExposesQueueMapperQueueServiceAndCache()
    {
        var clusterClient = Substitute.For<IClusterClient>();
        var loggerFactory = LoggerFactory.Create(_ => { });
        var options = new GrainsStreamOptions { MaxStreamNamespaceQueueCount = 1 };
        var cacheOptions = new SimpleQueueCacheOptions();
        var sut = new GrainsQueueAdapterFactory("provider", options, cacheOptions, clusterClient, loggerFactory);

        Assert.NotNull(sut.GetStreamQueueMapper());
        Assert.NotNull(sut.GetGrainsQueueService());
        Assert.Equal("GrainsRewindableQueueAdapterCache", sut.GetQueueAdapterCache().GetType().Name);
    }

    [Fact]
    public void StaticCreate_BuildsFactoryFromServiceProvider()
    {
        var services = new ServiceCollection();
        var clusterClient = Substitute.For<IClusterClient>();
        services.AddSingleton(clusterClient);
        services.AddLogging();
        services.Configure<GrainsStreamOptions>("provider", options =>
        {
            options.MaxStreamNamespaceQueueCount = 1;
        });
        services.Configure<SimpleQueueCacheOptions>("provider", _ => { });
        var provider = services.BuildServiceProvider();

        var factory = GrainsQueueAdapterFactory.Create(provider, "provider");

        Assert.NotNull(factory);
    }
}
