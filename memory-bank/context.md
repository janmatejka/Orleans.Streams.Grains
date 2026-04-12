# Context

## Active Work

- **Proposal:** [Rewindable chovani pro GrainsQueueStorage provider](proposals/active/proposal_rewindable_stream_provider.md)
- **Started:** 2026-04-12
- **Status:** Implementation
- **Execution Mode:** Strict
- **Run Mode:** autonomous
- **Jira:** (bez tiketu)

## Progress

- [x] Krok 1: doplnen `ReplayRetentionBatchCount` do `GrainsStreamOptions` a validaci v `GrainsStreamOptionsValidator`.
- [x] Krok 2: pridan `ReplayMessages` buffer do `QueueGrainState`.
- [x] Krok 3: pridano `GetReplayWindowAsync` API a DTO `GrainsQueueReplayWindow`.
- [x] Krok 4: prepisan `QueueGrain.DeleteQueueMessageAsync` na prefix-finalizer s replay trimem a idempotenci.
- [x] Krok 5: pridan `GrainsRewindableQueueAdapterCache` a `GrainsRewindableQueueCache` s retention windowem.
- [x] Krok 6: doplnen warmup replay buffer do `GrainsQueueAdapterReceiver.Initialize` a live duplicate filtering.
- [x] Krok 7: prepnuto `GrainsQueueAdapter.IsRewindable` na rewindable path.
- [x] Krok 8: doplnen `ReplayMessagesCount` do `QueueStatus`, naplnen z `QueueGrain.GetStatusAsync` a dopsany test snapshotu/stavu.
