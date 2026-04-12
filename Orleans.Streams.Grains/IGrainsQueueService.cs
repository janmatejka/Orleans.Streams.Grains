namespace Orleans.Streams.Grains;

public interface IGrainsQueueService
{
    Task QueueMessageBatchAsync(QueueId queueId, GrainsQueueBatchContainer message);

    Task<IList<GrainsQueueBatchContainer>> GetQueueMessagesAsync(QueueId queueId, int maxCount);

    Task<GrainsQueueReplayWindow> GetReplayWindowAsync(QueueId queueId, int maxCount);

    Task DeleteQueueMessageAsync(QueueId queueId, GrainsQueueBatchContainer message);

    Task ShutdownAsync(QueueId queueId);

    Task<GrainsQueueStatus> GetQueueStatusAsync(QueueId? queueId = null);
}
