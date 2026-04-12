using Microsoft.Extensions.Logging;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Streams.Grains;

public sealed class GrainsRewindableQueueCache : IQueueCache
{
    private readonly QueueId _queueId;
    private readonly string _providerName;
    private readonly int _replayRetentionBatchCount;
    private readonly ILogger _logger;
    private readonly LinkedList<CachedBatch> _cachedMessages = new();
    private readonly object _gate = new();

    public GrainsRewindableQueueCache(
        QueueId queueId,
        string providerName,
        int replayRetentionBatchCount,
        ILogger logger)
    {
        _queueId = queueId;
        _providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        _replayRetentionBatchCount = Math.Max(replayRetentionBatchCount, 1);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddToCache(IList<IBatchContainer> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        lock (_gate)
        {
            foreach (var message in messages)
            {
                _cachedMessages.AddFirst(new CachedBatch(message.StreamId, message.SequenceToken, message));
            }

            while (_cachedMessages.Count > _replayRetentionBatchCount)
            {
                _cachedMessages.RemoveLast();
            }
        }
    }

    public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
    {
        purgedItems = Array.Empty<IBatchContainer>();
        return false;
    }

    public IQueueCacheCursor GetCacheCursor(StreamId streamId, StreamSequenceToken token)
    {
        lock (_gate)
        {
            var matchingMessages = GetMatchingMessages(streamId);
            var cursor = new GrainsRewindableQueueCacheCursor(this, streamId);

            if (matchingMessages.Count == 0)
            {
                cursor.Unset(token);
                return cursor;
            }

            var newestToken = matchingMessages[0].SequenceToken;
            var oldestToken = matchingMessages[^1].SequenceToken;
            var requestedToken = token ?? newestToken;

            if (requestedToken.Newer(newestToken))
            {
                cursor.Unset(requestedToken);
                return cursor;
            }

            if (requestedToken.Older(oldestToken))
            {
                _logger.LogWarning(
                    "Queue cache miss on provider {ProviderName} queue {QueueId}. Requested {RequestedToken}, low {LowToken}, high {HighToken}.",
                    _providerName,
                    _queueId,
                    requestedToken,
                    oldestToken,
                    newestToken);

                throw new QueueCacheMissException(requestedToken, oldestToken, newestToken);
            }

            cursor.SetCurrent(FindCursorStart(matchingMessages, requestedToken)!);
            return cursor;
        }
    }

    public bool IsUnderPressure()
    {
        lock (_gate)
        {
            return _cachedMessages.Count >= _replayRetentionBatchCount;
        }
    }

    public int GetMaxAddCount()
    {
        return _replayRetentionBatchCount;
    }

    internal bool TryAdvanceCursor(GrainsRewindableQueueCacheCursor cursor)
    {
        lock (_gate)
        {
            var currentToken = cursor.CurrentToken;
            if (currentToken is null)
            {
                return false;
            }

            var next = GetMatchingMessages(cursor.StreamId)
                .FirstOrDefault(message => message.SequenceToken.Older(currentToken));

            if (next is null)
            {
                cursor.Unset(null);
                return false;
            }

            cursor.SetCurrent(next);
            return true;
        }
    }

    internal void RefreshCursor(GrainsRewindableQueueCacheCursor cursor, StreamSequenceToken token)
    {
        lock (_gate)
        {
            var matchingMessages = GetMatchingMessages(cursor.StreamId);
            if (matchingMessages.Count == 0)
            {
                cursor.Unset(token);
                return;
            }

            var newestToken = matchingMessages[0].SequenceToken;
            var oldestToken = matchingMessages[^1].SequenceToken;
            var effectiveToken = token ?? cursor.CurrentToken ?? newestToken;

            if (effectiveToken.Newer(newestToken))
            {
                cursor.Unset(effectiveToken);
                return;
            }

            if (effectiveToken.Older(oldestToken))
            {
                _logger.LogWarning(
                    "Queue cache miss on provider {ProviderName} queue {QueueId}. Requested {RequestedToken}, low {LowToken}, high {HighToken}.",
                    _providerName,
                    _queueId,
                    effectiveToken,
                    oldestToken,
                    newestToken);

                throw new QueueCacheMissException(effectiveToken, oldestToken, newestToken);
            }

            cursor.SetCurrent(FindCursorStart(matchingMessages, effectiveToken)!);
        }
    }

    private List<CachedBatch> GetMatchingMessages(StreamId streamId)
    {
        return _cachedMessages
            .Where(message => message.StreamId.Equals(streamId))
            .ToList();
    }

    private static CachedBatch? FindCursorStart(List<CachedBatch> matchingMessages, StreamSequenceToken token)
    {
        for (var index = 0; index < matchingMessages.Count; index++)
        {
            if (!matchingMessages[index].SequenceToken.Newer(token))
            {
                return matchingMessages[index];
            }
        }

        return null;
    }

    internal sealed class GrainsRewindableQueueCacheCursor : IQueueCacheCursor
    {
        private readonly GrainsRewindableQueueCache _cache;
        private IBatchContainer? _current;
        private bool _isSet;

        public GrainsRewindableQueueCacheCursor(GrainsRewindableQueueCache cache, StreamId streamId)
        {
            _cache = cache;
            StreamId = streamId;
        }

        public StreamId StreamId { get; }

        public StreamSequenceToken? CurrentToken { get; private set; }

        public IBatchContainer GetCurrent(out Exception exception)
        {
            exception = null!;
            return _current!;
        }

        public bool MoveNext()
        {
            if (!_isSet)
            {
                return false;
            }

            return _cache.TryAdvanceCursor(this);
        }

        public void Refresh(StreamSequenceToken token)
        {
            _cache.RefreshCursor(this, token);
        }

        public void RecordDeliveryFailure()
        {
        }

        public void Dispose()
        {
        }

        internal void SetCurrent(CachedBatch batch)
        {
            _current = batch.Batch;
            CurrentToken = batch.SequenceToken;
            _isSet = true;
        }

        internal void Unset(StreamSequenceToken? token)
        {
            _current = null;
            CurrentToken = token;
            _isSet = false;
        }
    }

    internal sealed record CachedBatch(StreamId StreamId, StreamSequenceToken SequenceToken, IBatchContainer Batch);
}
