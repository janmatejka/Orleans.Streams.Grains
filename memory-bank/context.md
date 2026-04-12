# Context

## Active Work

- **Proposal:** [Rewindable chovani pro GrainsQueueStorage provider](proposals/active/proposal_rewindable_stream_provider.md)
- **Started:** 2026-04-12
- **Status:** Implementation
- **Run Mode:** autonomous
- **Jira:** (bez tiketu)

## Progress

- [x] Review aktualni vetve proti `fc6babb974d62b498d9be4f85f2964541fe50f0b` hotova.
- [x] Overeni proti Orleans zdrojum (`c:\Users\matejka\source\repos\orleans`) hotove.
- [x] Potvrzeny problemy: cache pressure/purge, cursor semantika, replay window poradi, warmup hard-cap 32, dual source-of-truth retention.
- [x] Opravny checklist je zapsany v aktivnim proposalu (Strict mode, risk_score=3).
- [x] Realizovany kroky 1-6 z aktivniho proposalu.
- [x] Finalni gate probehla:
  - `dotnet build Orleans.Streams.Grains.slnx -c Release` -> 0 chyb, 0 warningu
  - `dotnet test Orleans.Streams.Grains.slnx -c Release -v minimal` -> 70/70 green
- [ ] Rozhodnout o prechodu na `@mb_done` (uzavreni navrhu) nebo dalsi iteraci.
