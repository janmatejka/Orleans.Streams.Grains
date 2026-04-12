using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Orleans.Streams.Grains;

public class GrainsQueueAdapter : IQueueAdapter
{
    private readonly IStreamQueueMapper _streamQueueMapper;
    private readonly IGrainsQueueService _grainsQueueService;
    private readonly ILoggerFactory _loggerFactory;

    public string Name { get; }

    public bool IsRewindable => true;

    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

    public GrainsQueueAdapter(
        IStreamQueueMapper streamQueueMapper,
        IGrainsQueueService grainsQueueService,
        ILoggerFactory loggerFactory,
        string providerName)
    {
        Name = providerName;
        _streamQueueMapper = streamQueueMapper;
        _grainsQueueService = grainsQueueService;
        _loggerFactory = loggerFactory;
    }

    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token,
        Dictionary<string, object> requestContext)
    {
        var queueId = _streamQueueMapper.GetQueueForStream(streamId);
        var messageBatchContainer =
            new GrainsQueueBatchContainer(streamId, events.Cast<object>().ToList(), requestContext);

        await _grainsQueueService.QueueMessageBatchAsync(queueId, messageBatchContainer);
    }

    public IQueueAdapterReceiver CreateReceiver(QueueId queueId) =>
        new GrainsQueueAdapterReceiver(queueId, _grainsQueueService, _loggerFactory);
}
