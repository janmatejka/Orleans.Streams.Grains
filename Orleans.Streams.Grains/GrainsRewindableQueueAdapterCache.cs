using Microsoft.Extensions.Logging;
using Orleans.Providers.Streams.Common;

namespace Orleans.Streams.Grains;

public sealed class GrainsRewindableQueueAdapterCache : IQueueAdapterCache
{
    private readonly string _providerName;
    private readonly int _replayRetentionBatchCount;
    private readonly ILoggerFactory _loggerFactory;

    public GrainsRewindableQueueAdapterCache(
        string providerName,
        int replayRetentionBatchCount,
        ILoggerFactory loggerFactory)
    {
        _providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        _replayRetentionBatchCount = Math.Max(replayRetentionBatchCount, 1);
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public IQueueCache CreateQueueCache(QueueId queueId)
    {
        return new GrainsRewindableQueueCache(
            queueId,
            _providerName,
            _replayRetentionBatchCount,
            _loggerFactory.CreateLogger<GrainsRewindableQueueCache>());
    }
}
