# Brief

## Ucel projektu
Projekt [Orleans.Streams.Grains](../Orleans.Streams.Grains/Orleans.Streams.Grains.csproj) je Orleans persistent stream provider nad stateful grains. Cilem je provozovat fronty streamu bez externiho brokeru a zustat kompatibilni se semantikou Orleans runtime.

## Klicove schopnosti
- Provider `GrainsQueueStorage` registruje adapter pro silo i klienta pres hosting extension metody ([SiloBuilderGrainsStreamExtensions](../Orleans.Streams.Grains/Hosting/SiloBuilderGrainsStreamExtensions.cs), [ClusterClientGrainsStreamExtensions](../Orleans.Streams.Grains/Hosting/ClusterClientGrainsStreamExtensions.cs)).
- Mapovani streamu na queue pres consistent ring mapper ([GrainsStreamQueueMapper](../Orleans.Streams.Grains/GrainsStreamQueueMapper.cs)).
- Rewindable stream behavior (`IsRewindable = true`) s Orleans kompatibilni cache cestou ([GrainsQueueAdapter](../Orleans.Streams.Grains/GrainsQueueAdapter.cs), [GrainsQueueAdapterFactory](../Orleans.Streams.Grains/GrainsQueueAdapterFactory.cs)).
- Replay retention okno potvrzenych batchi per queue (`ReplayRetentionBatchCount`) v persisted grain state ([QueueGrain](../Orleans.Streams.Grains/QueueGrain.cs), [QueueGrainState](../Orleans.Streams.Grains/QueueGrainState.cs), [GrainsQueueReplayWindow](../Orleans.Streams.Grains/GrainsQueueReplayWindow.cs)).
- Potvrzovani doruceni a dead-letter strategie (`Log`, `Requeue`, `DeadLetterQueue`) v queue grainu ([DeadLetterStrategyType](../Orleans.Streams.Grains/DeadLetterStrategyType.cs)).
- Monitoring stavu front pres [IGrainsQueueService](../Orleans.Streams.Grains/IGrainsQueueService.cs) vcetne `ReplayMessagesCount`.

## Scope repozitare
- Produkcni kod: [Orleans.Streams.Grains/](../Orleans.Streams.Grains/)
- Testy: [Orleans.Streams.Grains.Tests/](../Orleans.Streams.Grains.Tests/)
- CI release pipeline: [publish workflow](../.github/workflows/publish.yml)

## Aktualni stav kvality
- VS-MCP `ExecuteCommand(build)` na solution: uspech, 0 chyb, 0 warningu.
- VS-MCP `ExecuteAsyncTest(Orleans.Streams.Grains.Tests)`: `71/71` green.
- Pokryte kriticke scenare:
  - zatez pro ruzne pocty zapisovatelu/odberatelu,
  - deaktivace a reaktivace queue grainu,
  - primarni/sekundarni subscriber handoff z ulozeneho kurzoru.

## Memory Bank
- Korenska slozka znalosti projektu: [memory-bank/](./)
