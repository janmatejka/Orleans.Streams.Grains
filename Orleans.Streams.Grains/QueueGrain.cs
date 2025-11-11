using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;

namespace Orleans.Streams.Grains;

public class QueueGrain(
    [PersistentState(GrainsStreamProviderConsts.QueueStateName, GrainsStreamProviderConsts.QueueStorageName)]
    IPersistentState<QueueGrainState> persistentState,
    ILogger<QueueGrain> logger,
    IOptions<GrainsQueueOptions> options)
    : Grain, IQueueGrain
{
    public Task QueueMessageBatchAsync(GrainsQueueBatchContainer message)
    {
        message.RealSequenceToken = new EventSequenceTokenV2(persistentState.State.LastReadMessage++);
        persistentState.State.Messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public Task<IList<GrainsQueueBatchContainer>> GetQueueMessagesAsync(int maxCount)
    {
        var messages = new List<GrainsQueueBatchContainer>();
        while (persistentState.State.Messages.Count > 0 && messages.Count < maxCount)
        {
            var message = persistentState.State.Messages.Dequeue();
            persistentState.State.PendingMessages.Enqueue(message);
            messages.Add(message);
        }

        return Task.FromResult<IList<GrainsQueueBatchContainer>>(messages);
    }

    public Task DeleteQueueMessageAsync(GrainsQueueBatchContainer message)
    {
        var pending = persistentState.State.PendingMessages.Dequeue();
        while (!pending.SequenceToken.Equals(message.SequenceToken))
        {
            switch (options.Value.DeadLetterStrategy)
            {
                case DeadLetterStrategyType.Requeue:
                    persistentState.State.Messages.Enqueue(message);
                    break;
                case DeadLetterStrategyType.DeadLetterQueue:
                    persistentState.State.DroppedMessages.Enqueue(message);
                    break;
                case DeadLetterStrategyType.Log:
                default:
                    logger.LogWarning(
                        new QueueMessageDroppedException(message.ToString()),
                        "Message with token {Token} was not found in the pending messages queue.",
                        message.SequenceToken);
                    break;
            }

            pending = persistentState.State.PendingMessages.Dequeue();
        }

        return Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        await persistentState.WriteStateAsync();
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await persistentState.WriteStateAsync(cancellationToken);
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task<QueueStatus> GetStatusAsync()
    {
        var status = new QueueStatus(persistentState.Etag,
            persistentState.State.LastReadMessage,
            persistentState.State.Messages.Count,
            persistentState.State.PendingMessages.Count,
            persistentState.State.DroppedMessages.Count);

        return Task.FromResult(status);
    }
}