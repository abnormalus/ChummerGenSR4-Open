# Apple Silicon version

See [docs/MAC_PORT.md](docs/MAC_PORT.md) for application installation, build instructions, validation and remaining limitations.

`Chummer.Mac` is the native ARM64 preview project. `Chummer` is the original Windows application. Both character creation and career tracking are included in the Mac project's migrated code.

Build a standalone Mac application with the .NET 10 SDK:

```bash
bash scripts/build-mac.sh
```

The output is `artifacts/mac/ChummerSR4.app`.

## Local fork integration

The Mac project carries its matching catalogs, translations, sheets and sample characters in `Chummer.Mac/assets`. The upstream Windows source remains unchanged. Build with `dotnet build Chummer.Mac.sln -c Release`.
