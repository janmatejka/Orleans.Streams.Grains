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

    public Task<GrainsQueueReplayWindow> GetReplayWindowAsync(int maxCount)
    {
        if (maxCount <= 0 || persistentState.State.ReplayMessages.Count == 0)
        {
            return Task.FromResult(new GrainsQueueReplayWindow());
        }

        var skip = Math.Max(persistentState.State.ReplayMessages.Count - maxCount, 0);
        var messages = persistentState.State.ReplayMessages.Skip(skip).ToList();
        var warmupCutoffToken = messages.Count == 0 ? null : messages[^1].SequenceToken;

        return Task.FromResult(new GrainsQueueReplayWindow
        {
            Messages = messages,
            WarmupCutoffToken = warmupCutoffToken
        });
    }

    public Task DeleteQueueMessageAsync(GrainsQueueBatchContainer message)
    {
        var target = NormalizeSequenceToken(message.SequenceToken);
        var mutated = false;

        while (persistentState.State.PendingMessages.Count > 0)
        {
            var pending = persistentState.State.PendingMessages.Peek();

            if (pending.SequenceToken.Newer(target))
            {
                if (mutated)
                {
                    return persistentState.WriteStateAsync();
                }

                return Task.CompletedTask;
            }

            pending = persistentState.State.PendingMessages.Dequeue();

            if (pending.SequenceToken.Equals(target))
            {
                persistentState.State.ReplayMessages.Enqueue(pending);
                TrimReplayMessages();
                return persistentState.WriteStateAsync();
            }

            HandleGap(pending);
            mutated = true;
        }

        return mutated
            ? persistentState.WriteStateAsync()
            : Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        await persistentState.WriteStateAsync();
    }

    public Task DeactivateAsync()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
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
            persistentState.State.DroppedMessages.Count,
            persistentState.State.ReplayMessages.Count);

        return Task.FromResult(status);
    }

    private void HandleGap(GrainsQueueBatchContainer pending)
    {
        switch (options.Value.DeadLetterStrategy)
        {
            case DeadLetterStrategyType.Requeue:
                persistentState.State.Messages.Enqueue(pending);
                break;
            case DeadLetterStrategyType.DeadLetterQueue:
                persistentState.State.DroppedMessages.Enqueue(pending);
                break;
            case DeadLetterStrategyType.Log:
            default:
                logger.LogWarning(
                    new QueueMessageDroppedException(pending.ToString()),
                    "Message with token {Token} was not found in the pending messages queue.",
                    pending.SequenceToken);
                break;
        }
    }

    private void TrimReplayMessages()
    {
        while (persistentState.State.ReplayMessages.Count > options.Value.ReplayRetentionBatchCount)
        {
            persistentState.State.ReplayMessages.Dequeue();
        }
    }

    private static StreamSequenceToken NormalizeSequenceToken(StreamSequenceToken sequenceToken)
    {
        if (sequenceToken is EventSequenceTokenV2 eventSequenceToken && eventSequenceToken.EventIndex > 0)
        {
            return new EventSequenceTokenV2(eventSequenceToken.SequenceNumber);
        }

        return sequenceToken;
    }
}
