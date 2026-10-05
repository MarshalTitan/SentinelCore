# Adopting Sentinel Core

Adoption is optional and per plugin. A Core upgrade in one plugin cannot change another plugin because each consumer pins and packages its own chosen Core build. There is no installed Sentinel Core plugin and no process-wide service to coordinate.

## Recommended first Modern 2 adoption: SentinelHUD

SentinelHUD supplied the live-tested Sentinel Modern reference implementation and is the intended first consumer of the promoted shared UI. Updating SentinelHUD to consume Core remains a separate plugin task; this release does not alter SentinelHUD or any other plugin.

## Option A: exact release packages

1. Download the three `.nupkg` files attached to the chosen Sentinel Core GitHub release.
2. Commit only the package or packages the plugin actually consumes beneath a plugin-local feed, for example `.packages/SentinelCore/v0.3.0.0/`.
3. Add that directory as a package source in the consumer repository's `NuGet.Config`.
4. Pin the normalized package version exactly; do not use a wildcard or version range.

```xml
<ItemGroup>
  <PackageReference Include="MarshalTitan.SentinelCore" Version="0.3.0" />
  <PackageReference Include="MarshalTitan.SentinelCore.Dalamud" Version="0.3.0" />
  <PackageReference Include="MarshalTitan.SentinelCore.UI" Version="0.3.0" />
</ItemGroup>
```

Dalamud plugin packaging copies the selected library dependencies into that plugin's output. It does not install a global Core component.

## Option B: pinned source reference

Add Sentinel Core as a submodule pinned to an immutable release tag or commit, then reference only the required projects:

```powershell
git submodule add https://github.com/MarshalTitan/SentinelCore.git external/SentinelCore
git -C external/SentinelCore checkout v0.3.0.0
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

## Existing Sentinel Modern configuration shell

Reference `MarshalTitan.SentinelCore.UI` `0.3.0` and keep the consumer's persisted default at Classic.
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

The `0.2.1` `SentinelModernConfigurationShell`, `SentinelModernNavigation`,
`SentinelModernCard`, `SentinelModernControls`, and `SentinelModernUi` APIs remain available and
unchanged in `0.3.0`. Merely updating the package does not switch a consumer to Modern 2.

## Opting into Sentinel Modern 2

Retain one shell state and callback set for the lifetime of the window. This prevents frame-by-frame
delegate and animation-state rebuilding.

```csharp
private readonly SentinelModernAppShellState shellState = new();
private readonly SentinelModernNavItem[] primaryNavigation =
[
    new("Home", "H", "Home"),
    new("Settings", "S", "Settings"),
    new("Diagnostics", "D", "Diagnostics")
    {
        Badge = new SentinelModernNavBadge(SentinelModernPalette.Rose),
    },
];

private readonly Action<string> selectPage;
private readonly Action drawPage;
private readonly Action drawSettingsCategories;
private readonly Action drawDock;
private readonly Action closeWindow;

// Assign these once in the window constructor.
selectPage = id => selectedPageId = id;
drawPage = DrawSelectedPage;
drawSettingsCategories = DrawSettingsCategories;
drawDock = DrawActionDock;
closeWindow = () => IsOpen = false;
```

Draw the shell inside the existing Dalamud `Window.Draw` method:

```csharp
var options = new SentinelModernAppShellOptions(
    "MyPlugin.Modern2",
    "My Sentinel Plugin",
    selectedPageId)
{
    PluginGlyph = "S", // Or use DrawPluginIcon for a consumer-owned texture.
    ContextLabel = "Sentinel Modern 2",
    Status = new SentinelModernStatusPillOptions("READY", SentinelModernPillTone.Ready),
    Scale = ImGuiHelpers.GlobalScale,
    DeltaTime = ImGui.GetIO().DeltaTime,
    ReducedMotion = pluginInterface.UiBuilder.ShouldUseReducedMotion,
    AmbientIntensity = 0.72f,
    RequestClose = closeWindow,
};

SentinelModernAppShell.Draw(
    options,
    shellState,
    primaryNavigation,
    selectPage,
    drawPage,
    selectedPageId == "Settings" ? drawSettingsCategories : null,
    selectedPageId == "Home" ? drawDock : null);
```

Simple plugins should omit both optional callbacks for `icon rail → content`. Complex settings may
provide the secondary callback for `icon rail → categories → content`. The category callback uses
`SentinelModernSecondaryNavigation.GroupLabel` and `.Item`. The dock callback may use
`SentinelModernActionDock.PrimaryButton`, `.DangerButton`, and `.Status`.

Use `SentinelModernGlassCard.Begin` for content groups, `SentinelModernSettingsRow.Draw` for compact
label/control rows, `SentinelModernSwitch.Draw` for a controller-aware switch, and
`SentinelModernStatusPill.Draw` for page-level statuses. `SentinelModernPaint` exposes the shared
surface primitives for plugin-specific visualizations without duplicating palette or paint logic.

Reduced motion must be passed from `UiBuilder.ShouldUseReducedMotion`. Core then makes page and
selection transitions immediate, freezes ambient drift, and suppresses status/badge pulses without
changing any functional input or state behavior.

Adoption does not require a Sentinel Core plugin, a catalog entry, global configuration, or changes to
any other installed Sentinel plugin.

## Upgrade procedure

1. Change the pinned package version, tag, or submodule commit in one consumer repository.
2. Read Core release notes for source or behavior changes.
3. Build and run that plugin's own tests.
4. Verify the packaged ZIP contains the expected Core assemblies.
5. Field-test the consumer normally.
6. Release only that consumer after its version is incremented.

When that installable consumer releases, update its own authoritative `repo.json`. Do not add
SentinelCore to the Dalamud catalog and do not restore direct child-to-central catalog writes.

Do not update the live Sentinel catalog merely because Core changed. The catalog changes only when an installable consumer plugin releases a new version.

## Existing plugins

SRankSentinel, ClassySentinel, SentinelProfiles, SentinelRelay, and PvPSentinel intentionally remain unmodified by the initial Core work. Any future migration must be a separate task scoped to one plugin, with behavior-preservation tests and its own release decision.
