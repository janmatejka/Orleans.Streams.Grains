namespace Orleans.Streams.Grains;

public class GrainsQueueService(string providerName, IStreamQueueMapper streamQueueMapper, IClusterClient client)
    : IGrainsQueueService
{
    public string ProviderName { get; } = providerName;

    public Task QueueMessageBatchAsync(QueueId queueId, GrainsQueueBatchContainer message)
    {
        return client.GetGrain<IQueueGrain>(queueId.ToString()).QueueMessageBatchAsync(message);
    }

    public Task<IList<GrainsQueueBatchContainer>> GetQueueMessagesAsync(QueueId queueId, int maxCount)
    {
        return client.GetGrain<IQueueGrain>(queueId.ToString()).GetQueueMessagesAsync(maxCount);
    }

    public Task<GrainsQueueReplayWindow> GetReplayWindowAsync(QueueId queueId, int maxCount)
    {
        return client.GetGrain<IQueueGrain>(queueId.ToString()).GetReplayWindowAsync(maxCount);
    }

    public Task DeleteQueueMessageAsync(QueueId queueId, GrainsQueueBatchContainer message)
    {
        return client.GetGrain<IQueueGrain>(queueId.ToString()).DeleteQueueMessageAsync(message);
    }

    public Task ShutdownAsync(QueueId queueId)
    {
        return client.GetGrain<IQueueGrain>(queueId.ToString()).ShutdownAsync();
    }

    public async Task<GrainsQueueStatus> GetQueueStatusAsync(QueueId? queueId = null)
    {
        var result = new GrainsQueueStatus();
        if (!queueId.HasValue)
        {
            foreach (var q in streamQueueMapper.GetAllQueues())
            {
                result.Add(q, await client.GetGrain<IQueueGrain>(q.ToString()).GetStatusAsync());
            }
        }
        else
        {
            result.Add(queueId.Value, await client.GetGrain<IQueueGrain>(queueId.Value.ToString()).GetStatusAsync());
        }

        return result;
    }
}
