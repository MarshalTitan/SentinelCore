# Sentinel Core

Sentinel Core is the opt-in development foundation for future MarshalTitan Sentinel plugins. It is a set of versioned libraries, not an installable Dalamud plugin, not a catalog entry, and not a shared runtime service.

The intended first consumer is a future plugin such as **SentinelHUD**. Existing plugins remain independent until they are deliberately migrated in separate, tested work.

## Packages

| Package | Target | Purpose |
|---|---|---|
| `MarshalTitan.SentinelCore` | `net10.0` | Identity/versioning, configuration coordination, diagnostics, lifecycle safety, job-role models, and IPC contracts |
| `MarshalTitan.SentinelCore.Dalamud` | `net10.0-windows` / Dalamud API 15 | Dalamud configuration and logging adapters plus dynamic `ClassJob` metadata |
| `MarshalTitan.SentinelCore.UI` | `net10.0-windows` / Dalamud API 15 | Sentinel palette, role colours, balanced style scopes, and small ImGui helpers |

All three libraries carry assembly/file version `0.1.0.0`. NuGet packages use the normalized package version `0.1.0`; releases and Git tags use the Sentinel four-part tag `v0.1.0.0`.

## Design rules

- No `IDalamudPlugin` implementation, plugin manifest, installable ZIP, or `repo.json` entry.
- No existing Sentinel plugin is required to reference Core.
- Consumers pin an exact release or commit. Upgrades are explicit per plugin.
- The generic assembly has no Dalamud dependency.
- FFXIV job metadata is discovered from the live `ClassJob` sheet; job IDs are not hard-coded.
- Optional IPC calls fail closed and report a structured result.
- Disposable resources are unwound in reverse registration order and one failure does not prevent later cleanup.

See [ADOPTION.md](ADOPTION.md) for the exact opt-in workflow and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for component boundaries.

## Build and test

The complete solution needs .NET 10 and current Dalamud development files. Set `DALAMUD_HOME` to the directory containing `Dalamud.dll`, then run:

```powershell
dotnet restore SentinelCore.slnx -m:1
dotnet build SentinelCore.slnx -c Release --no-restore -m:1
dotnet run --project tests/SentinelCore.Tests/SentinelCore.Tests.csproj -c Release --no-build
dotnet pack src/SentinelCore/SentinelCore.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SentinelCore.Dalamud/SentinelCore.Dalamud.csproj -c Release -p:Platform=x64 --no-build -o artifacts/packages
dotnet pack src/SentinelCore.UI/SentinelCore.UI.csproj -c Release -p:Platform=x64 --no-build -o artifacts/packages
```

The single-node restore/build avoids a NuGet SDK resolver race when two Dalamud SDK projects are evaluated together. GitHub Actions downloads current Dalamud development files and performs this build on every push and pull request.
