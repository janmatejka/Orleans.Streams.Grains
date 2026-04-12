using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Orleans.Streams.Grains.Hosting;

/// <summary>
/// Helps set up an individual stream provider on a silo.
/// </summary>
public class SiloGrainsStreamConfigurator : SiloPersistentStreamConfigurator
{
    public SiloGrainsStreamConfigurator(string name, Action<Action<IServiceCollection>> configureDelegate) : base(
        ThrowIfNull(name, nameof(name)),
        ThrowIfNull(configureDelegate, nameof(configureDelegate)),
        GrainsQueueAdapterFactory.Create)
    {
        ConfigureDelegate(services =>
        {
            services
                .ConfigureNamedOptionForLogging<GrainsStreamOptions>(name)
                .ConfigureNamedOptionForLogging<SimpleQueueCacheOptions>(name)
                .AddTransient<IConfigurationValidator>(sp =>
                    new GrainsStreamOptionsValidator(sp.GetOptionsByName<GrainsStreamOptions>(name), name));
        });
    }

    public SiloGrainsStreamConfigurator ConfigureGrains(Action<OptionsBuilder<GrainsStreamOptions>> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        this.Configure(configureOptions);

        return this;
    }

    public SiloGrainsStreamConfigurator ConfigureCache(int cacheSize = SimpleQueueCacheOptions.DEFAULT_CACHE_SIZE)
    {
        this.Configure<SimpleQueueCacheOptions>(ob => ob.Configure(options => options.CacheSize = cacheSize));

        return this;
    }

    private static T ThrowIfNull<T>(T value, string paramName) where T : class
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }
}
