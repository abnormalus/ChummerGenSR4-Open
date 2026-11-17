# Apple Silicon version

Start with [CONTRIBUTING.md](CONTRIBUTING.md) for the supported shared build and verification commands. [docs/MAC_PORT.md](docs/MAC_PORT.md) records the original port and its historical limitations; use current CI and maintenance guidance for present acceptance status.

Despite its name, `Chummer.Mac` is the shared .NET 10 application for Mac and Windows, including character creation and career tracking. `Chummer` is the historical .NET Framework application; it is not the current Windows release build. Build the shared `Chummer.Mac.sln` for either platform.

Build a standalone Mac application with the .NET 10 SDK:

```bash
bash scripts/build-mac.sh
```

The output is `artifacts/mac/ChummerSR4.app`.

## Local fork integration

The Mac project carries its matching catalogs, translations, sheets and sample characters in `Chummer.Mac/assets`. The upstream Windows source remains unchanged. Build with `dotnet build Chummer.Mac.sln -c Release`.
