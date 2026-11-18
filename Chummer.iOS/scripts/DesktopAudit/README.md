# Independent desktop comparison

This optional console harness calls the production desktop Character, Weapon and
Skill implementations. It contains no copied mobile arithmetic. Inputs are read
only; settings are isolated under the output directory. JSON reports and optional
round-trip saves can contain private character data: keep them in ignored local
artifacts and do not upload them with CI results.

From the repository root, with the supported .NET SDK installed:

```sh
dotnet build Chummer.Mac.sln -c Release -p:UsedAvaloniaProducts=
dotnet build Chummer.iOS/scripts/DesktopAudit/DesktopAudit.csproj
cp Chummer.iOS/scripts/DesktopAudit/bin/Debug/net10.0/DesktopAudit.dll Chummer.Mac/bin/Release/net10.0/DossierAudit.dll
dotnet exec --depsfile Chummer.Mac/bin/Release/net10.0/ChummerMac.deps.json --runtimeconfig Chummer.Mac/bin/Release/net10.0/ChummerMac.runtimeconfig.json Chummer.Mac/bin/Release/net10.0/DossierAudit.dll INPUT_DIRECTORY OUTPUT_DIRECTORY
```

The harness audits every `.chum` in INPUT_DIRECTORY. Pass an optional third
ROUNDTRIP_INPUT argument to load it and save a separate `desktop.chum` under
OUTPUT_DIRECTORY. It never overwrites that input.

`TableAuditTests` is opt-in through DOSSIER_AUDIT_INPUT and
DOSSIER_AUDIT_EXPECTED. Its five local regression characters are Dialtone, Bandit,
Willow, Blue Smoke and Mongoose-Ready to play. Only those filenames are referenced
by the test; no private character contents are tracked. Compare imported attributes,
initiative/passes, condition capacities, weapons' displayed DV/AP and existing
skills' pools. Desktop-added skills absent from the input are outside that comparison.
This is a representative audit, not proof of every optional rule.

`TableCompanionTests/testPrivateManualEditsDesktopRoundTrip` accepts
DOSSIER_TABLE_INPUT (a copied Blue Smoke fixture) and DOSSIER_TABLE_OUTPUT. The
prepare phase writes `mobile.chum`; run it through the oracle's third argument,
then run the test with DOSSIER_TABLE_VERIFY=1. It checks attribute/rating edits,
magazine/reserve counts, notes-backed explicit DV and unchanged karma/nuyen after
production desktop save and mobile reimport.
