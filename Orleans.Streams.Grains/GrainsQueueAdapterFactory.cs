using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;

namespace Orleans.Streams.Grains;

public class GrainsQueueAdapterFactory : IQueueAdapterFactory
{
    private readonly string _providerName;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IQueueAdapterCache _adapterCache;
    private readonly IStreamQueueMapper _streamQueueMapper;
    private readonly GrainsQueueService _grainsQueueService;
    private readonly int _replayRetentionBatchCount;

    /// <summary>
    /// Application level failure handler override.
    /// </summary>
    protected Func<QueueId, Task<IStreamFailureHandler>>? StreamFailureHandlerFactory { private get; set; }

    public GrainsQueueAdapterFactory(string name,
        GrainsStreamOptions options,
        SimpleQueueCacheOptions cacheOptions,
        IClusterClient client,
        ILoggerFactory loggerFactory)
    {
        _providerName = name;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _replayRetentionBatchCount = Math.Max(1, options.ReplayRetentionBatchCount);
        var effectiveCacheOptions = new SimpleQueueCacheOptions
        {
            CacheSize = Math.Max(cacheOptions.CacheSize, _replayRetentionBatchCount)
        };
        _adapterCache = new SimpleQueueAdapterCache(effectiveCacheOptions, _providerName, _loggerFactory);
        _streamQueueMapper = new GrainsStreamQueueMapper(options);
        _grainsQueueService = new GrainsQueueService(
            _providerName,
            _streamQueueMapper,
            client);
        StreamFailureHandlerFactory = options.StreamFailureHandlerFactory;
    }

    /// <summary> Init the factory.</summary>
    public virtual void Init()
    {
        StreamFailureHandlerFactory ??= _ => Task.FromResult<IStreamFailureHandler>(
            new NoOpStreamDeliveryFailureHandler());
    }

    /// <summary>Creates the Grains Queue based adapter.</summary>
    public virtual Task<IQueueAdapter> CreateAdapter()
    {
        var adapter = new GrainsQueueAdapter(
            _streamQueueMapper,
            _grainsQueueService,
            _loggerFactory,
            _replayRetentionBatchCount,
            _providerName);
        return Task.FromResult<IQueueAdapter>(adapter);
    }

    /// <summary>Creates the adapter cache.</summary>
    public virtual IQueueAdapterCache GetQueueAdapterCache()
    {
        return _adapterCache;
    }

    public IGrainsQueueService GetGrainsQueueService()
    {
        return _grainsQueueService;
    }

    /// <summary>Creates the factory stream queue mapper.</summary>
    public IStreamQueueMapper GetStreamQueueMapper()
    {
        return _streamQueueMapper;
    }

    /// <summary>
    /// Creates a delivery failure handler for the specified queue.
    /// </summary>
    /// <param name="queueId"></param>
    /// <returns></returns>
    public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
    {
        return StreamFailureHandlerFactory!(queueId);
    }

    public static GrainsQueueAdapterFactory Create(IServiceProvider services, string name)
    {
        var options = services.GetOptionsByName<GrainsStreamOptions>(name);
        var cacheOptions = services.GetOptionsByName<SimpleQueueCacheOptions>(name);
        var f = ActivatorUtilities.CreateInstance<GrainsQueueAdapterFactory>(services, name, options, cacheOptions);
        f.Init();
        return f;
    }
}
