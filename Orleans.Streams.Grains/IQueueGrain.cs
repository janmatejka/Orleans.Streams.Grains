namespace Orleans.Streams.Grains;

[Alias("Orleans.Streams.Grains.IQueueGrain")]
public interface IQueueGrain : IGrainWithStringKey
{
    [Alias("QueueMessageBatchAsync")]
    Task QueueMessageBatchAsync(GrainsQueueBatchContainer message);

    [Alias("GetQueueMessagesAsync")]
    Task<IList<GrainsQueueBatchContainer>> GetQueueMessagesAsync(int maxCount);

    [Alias("GetReplayWindowAsync")]
    Task<GrainsQueueReplayWindow> GetReplayWindowAsync(int maxCount);

    [Alias("DeleteQueueMessageAsync")]
    Task DeleteQueueMessageAsync(GrainsQueueBatchContainer message);

    Task ShutdownAsync();
    Task<QueueStatus> GetStatusAsync();
}
