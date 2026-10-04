# Adopting Sentinel Core

Adoption is optional and per plugin. A Core upgrade in one plugin cannot change another plugin because each consumer pins and packages its own chosen Core build. There is no installed Sentinel Core plugin and no process-wide service to coordinate.

## Recommended first adoption: SentinelHUD

SentinelHUD supplied the live-tested Sentinel Modern reference implementation and is the intended first consumer of the promoted shared UI. Updating SentinelHUD to consume Core remains a separate plugin task; this release does not alter SentinelHUD or any other plugin.

## Option A: exact release packages

1. Download the three `.nupkg` files attached to the chosen Sentinel Core GitHub release.
2. Commit only the package or packages the plugin actually consumes beneath a plugin-local feed, for example `.packages/SentinelCore/v0.2.0.0/`.
3. Add that directory as a package source in the consumer repository's `NuGet.Config`.
4. Pin the normalized package version exactly; do not use a wildcard or version range.

```xml
<ItemGroup>
  <PackageReference Include="MarshalTitan.SentinelCore" Version="0.2.0" />
  <PackageReference Include="MarshalTitan.SentinelCore.Dalamud" Version="0.2.0" />
  <PackageReference Include="MarshalTitan.SentinelCore.UI" Version="0.2.0" />
</ItemGroup>
```

Dalamud plugin packaging copies the selected library dependencies into that plugin's output. It does not install a global Core component.

## Option B: pinned source reference

Add Sentinel Core as a submodule pinned to an immutable release tag or commit, then reference only the required projects:

```powershell
git submodule add https://github.com/MarshalTitan/SentinelCore.git external/SentinelCore
git -C external/SentinelCore checkout v0.2.0.0
```

```xml
<ItemGroup>
  <ProjectReference Include="external/SentinelCore/src/SentinelCore/SentinelCore.csproj" />
  <ProjectReference Include="external/SentinelCore/src/SentinelCore.Dalamud/SentinelCore.Dalamud.csproj" />
</ItemGroup>
```

Commit the submodule pointer. Never track Core's moving `main` branch from a released plugin.

## Minimal usage

```csharp
using SentinelCore.Configuration;
using SentinelCore.Dalamud.Configuration;
using SentinelCore.Lifecycle;

var lifetime = new DisposableBag(exception =>
    pluginLog.Error(exception, "Sentinel Core disposal failed."));
var store = new DalamudConfigurationStore<MyConfiguration>(pluginInterface);
var configuration = new ConfigurationCoordinator<MyConfiguration>(
    store,
    static () => new MyConfiguration(),
    static value => value);

lifetime.Add(configuration);
```

The normalization callback is the plugin's migration/repair boundary. It should preserve old user values, advance the plugin's own schema version, and return a valid configuration.

## Opting into Sentinel Modern

Reference `MarshalTitan.SentinelCore.UI` `0.2.0` and keep the consumer's persisted default at Classic.
The plugin can then expose `Classic` and `Sentinel Modern` as an explicit configuration choice.

Create one `SentinelModernStyleScope` per window and reuse it:

```csharp
private readonly SentinelModernStyleScope modernStyle = new();

public override void PreDraw() => modernStyle.Push(currentDalamudUiScale);
public override void PostDraw() => modernStyle.Pop();
```

Draw `SentinelModernConfigurationShell` inside the consumer's existing Dalamud `Window.Draw` method.
The shell does not own the top-level window, so normal title-bar collapse, close, resizing, and saved
position behavior remain intact. See [docs/MODERN_THEME.md](docs/MODERN_THEME.md) for a complete window,
navigation, card, switch, chip, scaling, and theme-state example.

Adoption does not require a Sentinel Core plugin, a catalog entry, global configuration, or changes to
any other installed Sentinel plugin.

## Upgrade procedure

1. Change the pinned package version, tag, or submodule commit in one consumer repository.
2. Read Core release notes for source or behavior changes.
3. Build and run that plugin's own tests.
4. Verify the packaged ZIP contains the expected Core assemblies.
5. Field-test the consumer normally.
6. Release only that consumer after its version is incremented.

Do not update the live Sentinel catalog merely because Core changed. The catalog changes only when an installable consumer plugin releases a new version.

## Existing plugins

SRankSentinel, ClassySentinel, SentinelProfiles, SentinelRelay, and PvPSentinel intentionally remain unmodified by the initial Core work. Any future migration must be a separate task scoped to one plugin, with behavior-preservation tests and its own release decision.
