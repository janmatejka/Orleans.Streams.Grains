# Brief

## Ucel projektu
Projekt [Orleans.Streams.Grains](../Orleans.Streams.Grains/Orleans.Streams.Grains.csproj) je knihovna, ktera implementuje Orleans stream provider nad ulozistem postavenym na stateful grains. Cilem je odstranit zavislost na externi queue infrastruktuře a pouzit Orleans storage jako frontu persistentnich streamu.

## Klicove schopnosti
- Provider `GrainsQueueStorage` registruje stream adapter pro silo i klienta pres hosting extension metody ([SiloBuilderGrainsStreamExtensions](../Orleans.Streams.Grains/Hosting/SiloBuilderGrainsStreamExtensions.cs), [ClusterClientGrainsStreamExtensions](../Orleans.Streams.Grains/Hosting/ClusterClientGrainsStreamExtensions.cs)).
- Mapovani streamu na queue pres consistent ring mapper ([GrainsStreamQueueMapper](../Orleans.Streams.Grains/GrainsStreamQueueMapper.cs)).
- Ukladani batch zpráv do grainu fronty ([QueueGrain](../Orleans.Streams.Grains/QueueGrain.cs), [QueueGrainState](../Orleans.Streams.Grains/QueueGrainState.cs)).
- Potvrzovani doruceni a dead-letter strategie (`Log`, `Requeue`, `DeadLetterQueue`) v receiveru + grainu ([GrainsQueueAdapterReceiver](../Orleans.Streams.Grains/GrainsQueueAdapterReceiver.cs), [DeadLetterStrategyType](../Orleans.Streams.Grains/DeadLetterStrategyType.cs)).
- Jednoduche monitorovani stavu front pres [IGrainsQueueService](../Orleans.Streams.Grains/IGrainsQueueService.cs).

## Scope repozitare
- Produkcni kod: [Orleans.Streams.Grains/](../Orleans.Streams.Grains/)
- Test harness: [Orleans.Streams.Grains.Tests/](../Orleans.Streams.Grains.Tests/)
- CI release pipeline: [publish workflow](../.github/workflows/publish.yml)

## Aktualni stav kvality
- `dotnet build Orleans.Streams.Grains.slnx -c Release`: prochazi bez chyb.
- `dotnet test Orleans.Streams.Grains.slnx -c Release`: adapter testu bezi, ale test assembly neobsahuje zadne test case.

## Memory Bank
- Korenska slozka znalosti projektu: [memory-bank/](./)
