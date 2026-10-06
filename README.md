# Sentinel Core

Sentinel Core is the opt-in development foundation for future MarshalTitan Sentinel plugins. It is a set of versioned libraries, not an installable Dalamud plugin, not a catalog entry, and not a shared runtime service.

**SentinelHUD** supplied the first live-tested Sentinel Modern preview and remains the intended first
consumer of the opt-in Sentinel Modern 2 application shell. Existing plugins remain independent
until they are deliberately migrated in separate, tested work.

## Packages

| Package | Target | Purpose |
|---|---|---|
| `MarshalTitan.SentinelCore` | `net10.0` | Identity/versioning, configuration coordination, diagnostics, lifecycle safety, job-role models, and IPC contracts |
| `MarshalTitan.SentinelCore.Dalamud` | `net10.0-windows` / Dalamud API 15 | Dalamud configuration and logging adapters plus dynamic `ClassJob` metadata |
| `MarshalTitan.SentinelCore.UI` | `net10.0-windows` / Dalamud API 15 | Classic and opt-in Sentinel Modern palettes, application shell, navigation, motion, glass surfaces, controls, role colours, and balanced ImGui scopes |

All three libraries carry assembly/file version `0.3.1.0`. NuGet packages use the normalized package
version `0.3.1`; releases and Git tags use the Sentinel four-part tag `v0.3.1.0`.

## Sentinel Modern 2

Modern 2 adds a polished application-style shell without replacing the existing configuration
shell. Its canonical shared pieces include:

- a 56px compact header and 64px icon navigation rail;
- an optional 196px secondary settings sidebar;
- an optional 72px action/status dock;
- a unified, full-bleed application surface and single-custom-header window policy;
- retained custom icon rendering for Font Awesome glyphs or consumer-owned textures;
- clean text-only secondary navigation without letter placeholders;
- responsive settings rows that stack controls before text can overlap;
- frame-rate-independent hover, selection, page-reveal, pulse, and ambient motion;
- deterministic reduced-motion behavior;
- more visible procedural blue, violet, and teal background glows and rings;
- glass cards, gradient surfaces, shadows, glows, highlights, separators, pills, progress bars,
  status dots, settings rows, and controller-aware switches.

All measurements are logical pixels and scale with Dalamud UI scaling. Primary and secondary
navigation always remain on the left; the shared layout never stacks navigation above content.
The shell draws inside a consumer-owned Dalamud window, keeping window lifetime, saved position,
resizing, and close/collapse policy under the consumer's control. `SentinelModernWindowChrome`,
`SentinelModernStyleScope.PushAppShell`, and opt-in header dragging provide the canonical frameless
integration without changing older configuration-shell consumers.

## Design rules

- No `IDalamudPlugin` implementation, plugin manifest, installable ZIP, or `repo.json` entry.
- No existing Sentinel plugin is required to reference Core.
- Consumers pin an exact release or commit. Upgrades are explicit per plugin.
- The generic assembly has no Dalamud dependency.
- FFXIV job metadata is discovered from the live `ClassJob` sheet; job IDs are not hard-coded.
- Optional IPC calls fail closed and report a structured result.
- Disposable resources are unwound in reverse registration order and one failure does not prevent later cleanup.

See [ADOPTION.md](ADOPTION.md) for the exact opt-in workflow,
[docs/MODERN_THEME.md](docs/MODERN_THEME.md) for both Modern generations, and
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for component boundaries.

## Build and test

The complete solution needs .NET 10 and current Dalamud development files. Set `DALAMUD_HOME` to the directory containing `Dalamud.dll`, then run:

```powershell
dotnet restore SentinelCore.slnx -m:1
dotnet build SentinelCore.slnx -c Release --no-restore -m:1
dotnet run --project tests/SentinelCore.Tests/SentinelCore.Tests.csproj -c Release --no-build
dotnet run --project tests/SentinelCore.UI.Tests/SentinelCore.UI.Tests.csproj -c Release --no-build
dotnet pack src/SentinelCore/SentinelCore.csproj -c Release --no-build -o artifacts/packages
dotnet pack src/SentinelCore.Dalamud/SentinelCore.Dalamud.csproj -c Release -p:Platform=x64 --no-build -o artifacts/packages
dotnet pack src/SentinelCore.UI/SentinelCore.UI.csproj -c Release -p:Platform=x64 --no-build -o artifacts/packages
```

The single-node restore/build avoids a NuGet SDK resolver race when two Dalamud SDK projects are evaluated together. GitHub Actions downloads current Dalamud development files and performs this build on every push and pull request.
