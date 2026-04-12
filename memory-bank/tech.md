# Tech

## Platforma a build
- Target framework: `net9.0` ([Orleans.Streams.Grains.csproj](../Orleans.Streams.Grains/Orleans.Streams.Grains.csproj), [Orleans.Streams.Grains.Tests.csproj](../Orleans.Streams.Grains.Tests/Orleans.Streams.Grains.Tests.csproj))
- SDK v lokalnim prostredi: .NET SDK 10.0.201 (kompatibilne buildi `net9.0`)
- Nullable: `enable`
- Implicit usings: `enable`
- Warnings as errors: zapnuto v knihovne

## Zavislosti
### Produkcni knihovna
- `Microsoft.Orleans.Streaming` 9.2.1

### Test projekt
- `Microsoft.NET.Test.Sdk` 17.14.1
- `Microsoft.Orleans.TestingHost` 9.2.1
- `NSubstitute` 5.3.0
- `NSubstitute.Analyzers.CSharp` 1.0.17
- `Shouldly` 4.3.0
- `xunit` 2.9.3
- `xunit.extensibility.execution` 2.9.3
- `xunit.runner.visualstudio` 3.1.4

## Konfigurace stream provideru
- Provider type konstanta: `GrainsQueueStorage` ([GrainsStreamProviderConsts](../Orleans.Streams.Grains/GrainsStreamProviderConsts.cs))
- Konfigurovatelne options:
  - `MaxStreamNamespaceQueueCount`
  - `NamespaceQueue[]`
  - `StreamFailureHandlerFactory`
- Volitelne `CacheSize` v config section (zpracovava [GrainsQueueStreamProviderBuilder](../Orleans.Streams.Grains/Hosting/GrainsQueueStreamProviderBuilder.cs)).

## Build/Test prikazy
- `dotnet restore Orleans.Streams.Grains.slnx`
- `dotnet build Orleans.Streams.Grains.slnx -c Release`
- `dotnet test Orleans.Streams.Grains.slnx -c Release`

## Stav verifikace (po implementaci testu)
- Test project obsahuje aktivni test sadu s pokrytim API knihovny i integrovaneho rewind behavioru.
- Posledni overeni pres VS-MCP:
  - `ExecuteCommand(build)`: uspech, 0 chyb, 0 warningu
  - `ExecuteAsyncTest(Orleans.Streams.Grains.Tests)`: 71/71 passed
- Overene provozni scenare:
  - soubezni writer/reader kombinace pro ruzne pocty producentu a konzumentu,
  - deaktivace/reaktivace queue grainu se zachovanim replay windowu,
  - handoff kurzoru mezi primarnim a sekundarnim subscriberem.
- Dulezita oprava v produkcnim kodu:
  - `ThrowIfNull` helper v hosting konfiguratorech pro korektni `ArgumentNullException` pri neplatnych vstupech.

## CI/CD
- Workflow: [publish.yml](../.github/workflows/publish.yml)
- Trigger: push na `main`
- Kroky: restore -> build -> test -> pack -> push GitHub Packages -> push NuGet -> git tag z `<Version>`.

## Artefakty
- NuGet balicek se generuje pri build knihovny (`GeneratePackageOnBuild=true`).
- Package metadata: `PackageId=Orleans.Streams.Grains`, `Version=1.4.0`.
