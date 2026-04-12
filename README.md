# Orleans.Streams.Grains

`Orleans.Streams.Grains` is a persistent Orleans stream provider (`ProviderType = "GrainsQueueStorage"`) that stores stream queue state in Orleans grains.

## Implemented Functionality

- Persistent stream provider for Orleans (`IQueueAdapter` + `IQueueAdapterReceiver`) with read/write direction.
- Rewindable behavior enabled (`IsRewindable = true`).
- Replay warmup on receiver initialization:
  - reads a replay window from queue grain state first,
  - then continues with live queue polling,
  - filters duplicate live messages using a warmup cutoff token.
- Replay retention window (`ReplayRetentionBatchCount`) with queue-grain persistence.
- Queue state lifecycle:
  - queued messages,
  - pending messages,
  - dropped messages,
  - replay messages (confirmed batches).
- Dead-letter gap handling strategies in `QueueGrain`:
  - `Log`,
  - `Requeue`,
  - `DeadLetterQueue`.
- Queue status diagnostics via `IGrainsQueueService.GetQueueStatusAsync`, including `ReplayMessagesCount`.
- Namespace-based queue mapping (`MaxStreamNamespaceQueueCount` + optional `NamespaceQueue` overrides).

## Requirements

- .NET 9 (`net9.0`)
- Orleans Streaming 9.x
- A grain storage provider named:
  - `QueueStore` for queue grain state (`GrainsStreamProviderConsts.QueueStorageName`)
  - provider-name storage for Orleans stream metadata/pub-sub state (typical Orleans persistent stream setup)

## Configuration

### 1) Silo Configuration (code-first)

```csharp
using Orleans.Hosting;
using Orleans.Streams.Grains;
using Orleans.Streams.Grains.Hosting;

const string ProviderName = "OrdersProvider";

siloBuilder
    // Queue grain persistence (replace with durable storage in production)
    .AddMemoryGrainStorage(GrainsStreamProviderConsts.QueueStorageName)
    // Provider state storage (Orleans persistent stream infrastructure)
    .AddMemoryGrainStorage(ProviderName)
    .AddGrainsStreams(ProviderName, options =>
    {
        options.MaxStreamNamespaceQueueCount = 8;
        options.ReplayRetentionBatchCount = 1000;
        options.NamespaceQueue =
        [
            new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
            {
                Namespace = "orders",
                QueueCount = 4
            },
            new GrainsStreamOptions.GrainsStreamProviderNamespaceQueueOptions
            {
                Namespace = "payments",
                QueueCount = 2
            }
        ];
    })
    .ConfigureServices(services =>
    {
        // Optional: dead-letter behavior for pending-gap handling.
        services.Configure<GrainsQueueOptions>(o =>
        {
            o.DeadLetterStrategy = DeadLetterStrategyType.Log;
        });
    });
```

### 2) Client Configuration

```csharp
using Orleans.Hosting;
using Orleans.Streams.Grains.Hosting;

const string ProviderName = "OrdersProvider";

clientBuilder.AddGrainsStreams(ProviderName, options =>
{
    options.MaxStreamNamespaceQueueCount = 8;
    options.ReplayRetentionBatchCount = 1000;
});
```

### 3) appsettings Configuration (provider builder)

```json
{
  "Orleans": {
    "Streaming": {
      "OrdersProvider": {
        "ProviderType": "GrainsQueueStorage",
        "MaxStreamNamespaceQueueCount": 8,
        "ReplayRetentionBatchCount": 1000,
        "NamespaceQueue": [
          { "Namespace": "orders", "QueueCount": 4 },
          { "Namespace": "payments", "QueueCount": 2 }
        ],
        "CacheSize": 1000
      }
    }
  }
}
```

## Runtime Semantics

- Rewind is **batch-level**.
- Queue delete/finalization normalizes `EventSequenceTokenV2` with `EventIndex > 0` to the batch base token (`EventIndex = 0`).
- Replay window returns the newest confirmed batches up to `maxCount`.
- Warmup cutoff token is the highest token in the warmup replay window.

## Edge Cases and Limitations

- Retention is **count-based**, not time-based.
- Replay warmup returns only **confirmed (finalized/deleted)** batches, not all pending/live queue content.
- Namespace mapping is strict:
  - an unknown non-empty namespace throws `ArgumentException` in queue mapping.
- `GrainsQueueOptions.DeadLetterStrategy` is configured as options and is not provider-name scoped in the current implementation.
- `DeadLetterQueue` strategy stores dropped messages in queue grain state; there is no dedicated public retrieval API for those dropped entries.
- At-least-once delivery semantics still apply; duplicates can occur in failure/retry paths (standard Orleans streaming behavior).
- For durable replay across restarts, `QueueStore` must use durable grain storage. In-memory storage is for testing/dev only.
- Cache size is effectively aligned to replay retention (`max(CacheSize, ReplayRetentionBatchCount)` in adapter factory), so very large replay retention increases memory footprint.

## Notes

- If behavior is unclear, align with Orleans persistent stream/cache semantics (`SimpleQueueCache`, `SimpleQueueCacheCursor`, `PersistentStreamPullingAgent`) to stay compatible with the Orleans runtime.
