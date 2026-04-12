# Produkt

## Problem, ktery projekt resi
Orleans aplikace casto potrebuji persistentni stream provider, ale bez provozni zavislosti na externim brokeru. Tento projekt poskytuje queue vrstvu uvnitr Orleans ekosystemu pomoci grainu a persistent state.

## Nabizene reseni
- Stream events se serializuji do [GrainsQueueBatchContainer](../Orleans.Streams.Grains/GrainsQueueBatchContainer.cs) a ukladaji do queue grainu.
- [GrainsQueueAdapter](../Orleans.Streams.Grains/GrainsQueueAdapter.cs) + [GrainsQueueAdapterReceiver](../Orleans.Streams.Grains/GrainsQueueAdapterReceiver.cs) realizuji produkci i konzumaci batchi.
- [GrainsQueueAdapterFactory](../Orleans.Streams.Grains/GrainsQueueAdapterFactory.cs) sklada mapper, Orleans cache a queue service.
- [GrainsStreamOptions](../Orleans.Streams.Grains/GrainsStreamOptions.cs) umoznuji konfigurovat pocet queues globalne/per namespace i replay retention (`ReplayRetentionBatchCount`).
- Queue grain drzi rewind window potvrzenych batchi a receiver provadi replay warmup pred live pollingem ([QueueGrain](../Orleans.Streams.Grains/QueueGrain.cs), [GrainsQueueAdapterReceiver](../Orleans.Streams.Grains/GrainsQueueAdapterReceiver.cs)).

## Cilova skupina
- Vyvojari Orleans backendu, kteri chteji persistentni stream provider integrovany primo do Orleans clusteru.

## UX/Developer cile
- Minimalni registracni API: `AddGrainsStreams(...)` pro `ISiloBuilder` i `IClientBuilder`.
- Predvidatelna konfigurace z code-first i config-first pristupu ([GrainsQueueStreamProviderBuilder](../Orleans.Streams.Grains/Hosting/GrainsQueueStreamProviderBuilder.cs)).
- Jednoducha observabilita queue stavu pres `GetQueueStatusAsync`.
- Definovane chovani pri ztrate/nesouladu doruceni pomoci dead-letter strategie.

## Omezujici podminky
- Projekt je navrzen pro `net9.0`.
- Rewind funguje na batch-level tokenech (ne na jednotlivych event offsetech v ramci batch).
- Retention je count-based (`ReplayRetentionBatchCount`), neni time-based.
- Pro produkcni restart-safe replay je nutne pouzit durable grain storage pro `QueueStore`.
