using Orleans.Hosting;
using Orleans.Streams.Grains.Hosting;

namespace Orleans.Streams.Grains.Tests;

public class HostingApiTests
{
    [Fact]
    public void SiloBuilder_AddGrainsStreams_ThrowsForNullName_OptionsOverload()
    {
        ISiloBuilder builder = null!;
        Action<GrainsStreamOptions> configure = _ => { };

        Assert.Throws<ArgumentNullException>(() =>
            builder.AddGrainsStreams(null, configure));
    }

    [Fact]
    public void SiloBuilder_AddGrainsStreams_ThrowsForNullName_ConfiguratorOverload()
    {
        ISiloBuilder builder = null!;
        Action<SiloGrainsStreamConfigurator> configure = _ => { };

        Assert.Throws<ArgumentNullException>(() =>
            builder.AddGrainsStreams(null, configure));
    }

    [Fact]
    public void ClientBuilder_AddGrainsStreams_ThrowsForNullName_OptionsOverload()
    {
        IClientBuilder builder = null!;
        Action<GrainsStreamOptions> configure = _ => { };

        Assert.Throws<ArgumentNullException>(() =>
            builder.AddGrainsStreams(null, configure));
    }

    [Fact]
    public void ClientBuilder_AddGrainsStreams_ThrowsForNullName_ConfiguratorOverload()
    {
        IClientBuilder builder = null!;
        Action<ClusterClientGrainsStreamConfigurator> configure = _ => { };

        Assert.Throws<ArgumentNullException>(() =>
            builder.AddGrainsStreams(null, configure));
    }

    [Fact]
    public void SiloGrainsStreamConfigurator_Ctor_ThrowsForInvalidArgs()
    {
        Action<Action<Microsoft.Extensions.DependencyInjection.IServiceCollection>> configureDelegate = _ => { };

        Assert.Throws<ArgumentNullException>(() => new SiloGrainsStreamConfigurator(null!, configureDelegate));
        Assert.Throws<ArgumentNullException>(() => new SiloGrainsStreamConfigurator("provider", null!));
    }

    [Fact]
    public void ClusterClientGrainsStreamConfigurator_Ctor_ThrowsForInvalidArgs()
    {
        IClientBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() => new ClusterClientGrainsStreamConfigurator(null!, builder));
        Assert.Throws<ArgumentNullException>(() => new ClusterClientGrainsStreamConfigurator("provider", null!));
    }

    [Fact]
    public void GrainsQueueStreamProviderBuilder_CanBeConstructed()
    {
        var sut = new GrainsQueueStreamProviderBuilder();

        Assert.NotNull(sut);
    }
}
