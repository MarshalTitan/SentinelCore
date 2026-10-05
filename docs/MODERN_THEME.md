# Sentinel Modern UI

Sentinel Modern is the opt-in configuration-window design system promoted from the live-tested
Sentinel HUD `0.8.3.0` preview. It preserves the midnight-navy, electric-blue, violet, teal, and
rose visual language without copying third-party assets, branding, or source.

The Classic Sentinel palette and helpers remain available. Installing or updating Core does not
switch any consumer automatically; each plugin chooses and persists its own theme.

## Sentinel Modern 2 application shell

Modern 2 is a new, opt-in application-shell layer in package `0.3.0`. It does not redefine the
existing `SentinelModernConfigurationShell`; consumers can upgrade their package first and migrate
their window later.

The default logical geometry is:

| Region | Size |
|---|---:|
| Compact header | 56px |
| Primary icon rail | 64px |
| Icon button | 42px |
| Optional secondary sidebar | 196px |
| Optional action dock | 72px |
| Content padding | 20px |

Every value scales with the supplied Dalamud UI scale. Navigation never stacks above content. A
simple plugin uses `rail → content`; category-heavy settings use `rail → secondary sidebar →
content`. The action dock is present only when the consumer supplies its drawing callback.

### Modern 2 component map

| API | Purpose |
|---|---|
| `SentinelModernAppShell` | Compact header, primary rail, content, optional secondary sidebar, and optional dock |
| `SentinelModernAppShellState` | Per-window page transition and bounded motion-channel ownership |
| `SentinelModernAppLayout` | Tested logical sizing, scaling, minimum-size calculation, and non-stacking policy |
| `SentinelModernIconRail` | Icon buttons, tooltips, animated indicator, hover state, and optional badge |
| `SentinelModernSecondaryNavigation` | Canonical icon/label category rows |
| `SentinelModernMotion` | Frame-rate-independent smoothing, hover, wave, pulse, and reduced-motion behavior |
| `SentinelModernPaint` | Gradients, glass, shadow, glow, highlight, separator, pill, progress, and status dot |
| `SentinelModernGlassCard` | Balanced, translucent card child scope |
| `SentinelModernSettingsRow` | Compact label/description/control row |
| `SentinelModernSwitch` | Standard ImGui activation with Core-owned switch rendering and motion |
| `SentinelModernStatusPill` | Ready, enabled, running, warning, error, neutral, accent, and custom pills |
| `SentinelModernPageTransition` | Optional 240ms content-only fade and 10px slide |
| `SentinelModernActionDock` | Optional primary, danger, and status helpers for consumer-owned actions |
| `SentinelModernAmbient.DrawAnimated` | Three low-opacity drifting blue/violet/teal procedural glows |

The primary and secondary navigation and the Modern 2 switch use standard ImGui buttons as their
input targets. Mouse, keyboard, and controller activation therefore remain functional even though
their appearance is drawn with the window draw list.

Pass `pluginInterface.UiBuilder.ShouldUseReducedMotion` into the shell options every frame. With
reduced motion enabled, selection and page changes become immediate, ambient centres stay fixed,
and optional pulses become static. Layout and input behavior do not change.

The custom header supports a consumer-owned icon callback or glyph, compact status pill, context
label, and optional minimize/close callbacks. Core still does not own the top-level window: the
consumer decides whether to retain the native Dalamud title bar or use its own window flags and bind
the shared header callbacks. Header and navigation children explicitly disallow scrolling.

See [ADOPTION.md](../ADOPTION.md) for a complete retained-state integration example.

## Original configuration-shell component map

| API | Purpose |
|---|---|
| `SentinelModernPalette` | Semantic colour tokens for canvas, surfaces, borders, accents, text, toggles, and status tones |
| `SentinelModernStyleScope` | Reusable, balanced styling for windows, children, buttons, combos, sliders, scrollbars, resize grips, and collapsing headers |
| `SentinelModernConfigurationShell` | Non-scrolling header, persistent left navigation rail, and right content area inside an existing Dalamud window |
| `SentinelModernNavigation` | Group labels and selected/unselected navigation items |
| `SentinelModernCard` | Rounded child-panel/card scope with guaranteed `EndChild` balancing |
| `SentinelModernControls` | Modern switch and collapsing-section helpers |
| `SentinelModernUi` | Page headings, descriptions, section headings, and status chips |
| `SentinelModernAmbient` | Optional procedural blue, violet, and teal background rings; no image assets |
| `SentinelThemeState<TPage>` | Renderer-independent opt-in theme and selected-page state |
| `SentinelModernLayout` | Tested sidebar sizing with an explicit opt-in stacked layout for non-standard consumers |

## Window integration

Keep Dalamud's `Window` as the owner of the top-level window. The Core shell only draws inside it,
so title-bar close, collapse, resizing, size constraints, and position persistence continue to work.

Create and retain the style scope and drawing delegates once. Reusing them avoids rebuilding assets
or allocating captured callbacks every frame.

```csharp
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using SentinelCore.UI;

public sealed class ConfigurationWindow : Window
{
    private readonly SentinelModernStyleScope modernStyle = new();
    private readonly Action drawNavigation;
    private readonly Action drawContent;
    private ConfigurationPage selectedPage;

    public ConfigurationWindow()
        : base("My Sentinel Plugin Configuration##MySentinelPlugin-Configuration")
    {
        Size = new Vector2(920f, 720f);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(620f, 520f) };
        drawNavigation = DrawNavigation;
        drawContent = DrawContent;
    }

    public override void PreDraw() => modernStyle.Push(ImGuiHelpers.GlobalScale);

    public override void PostDraw() => modernStyle.Pop();

    public override void Draw()
    {
        var options = new SentinelModernShellOptions(
            "MySentinelPlugin",
            "MARSHALTITAN  /  SENTINEL",
            "MY SENTINEL PLUGIN",
            "A short description of the plugin")
        {
            Scale = ImGuiHelpers.GlobalScale,
            ContextLabel = "Sentinel Modern",
            Status = new SentinelModernStatus("READY", SentinelModernStatusTone.Success),
        };

        SentinelModernConfigurationShell.Draw(options, drawNavigation, drawContent);
    }

    private void DrawNavigation()
    {
        SentinelModernNavigation.GroupLabel("SETTINGS");
        if (SentinelModernNavigation.Item(
                "General", "General", selectedPage == ConfigurationPage.General,
                ImGuiHelpers.GlobalScale))
            selectedPage = ConfigurationPage.General;

        ImGui.Spacing();
        SentinelModernNavigation.GroupLabel("TOOLS");
        if (SentinelModernNavigation.Item(
                "Diagnostics", "Diagnostics", selectedPage == ConfigurationPage.Diagnostics,
                ImGuiHelpers.GlobalScale))
            selectedPage = ConfigurationPage.Diagnostics;
    }

    private void DrawContent()
    {
        SentinelModernUi.PageHeading("General", "Global plugin state and shared behaviour.");
        ImGui.Spacing();
        using var card = SentinelModernCard.Begin("GeneralCard");
        if (!card.IsVisible)
            return;

        SentinelModernUi.SectionHeader("GENERAL");
        var enabled = true;
        if (SentinelModernControls.Toggle(
                "Enabled", "Enable plugin", ref enabled, ImGuiHelpers.GlobalScale))
        {
            // Persist through the consumer's own configuration coordinator.
        }
    }

    private enum ConfigurationPage
    {
        General,
        Diagnostics,
    }
}
```

If a plugin supports both themes, push either its existing Classic scope or the reusable Modern
scope in `PreDraw`, and pop/dispose the matching scope in `PostDraw`. Never push both at once.

## Persisted opt-in selection

Keep the consumer's persisted default at Classic for backward compatibility. A new setting can map
to `SentinelThemeKind` directly because `Classic` is `0` and `Modern` is `1`.

```csharp
var selected = SentinelThemeState<MyPage>.NormalizeTheme(configuration.Theme);
var state = new SentinelThemeState<MyPage>(MyPage.General, selected);
```

Unknown future or corrupt persisted values normalize safely to Classic. Core does not save this
state globally and cannot change another plugin's selection.

## Scaling and resizing

- Pass the current Dalamud UI scale to the style scope, shell, navigation items, switches, and chips.
- The shell always uses a bounded proportional left sidebar by default, including on narrow windows.
- Keep a practical consumer minimum width (the example uses `620f`) so the right content pane remains useful.
- The standard header is at least `84f` high before UI scaling and never displays its own scrollbar.
- Non-standard consumers may explicitly set
  `Layout = SentinelModernLayoutOptions.Default with { AllowStackedNavigation = true }`; Sentinel
  configuration windows should not enable this.
- Consumers still set their own first-use size and minimum size through Dalamud's `Window` API.
- Set `DrawAmbientBackground = false` for a flat canvas, or reduce `AmbientIntensity` for a quieter effect.

## Push/pop safety

- `SentinelModernStyleScope.Push` rejects nested use and records every colour and style variable.
- `Pop` is idempotent and balances the full set; `Dispose` safely pops an active scope.
- Navigation and section helpers use `try/finally` around temporary pushes.
- `SentinelModernCardScope` always calls `EndChild` when disposed, even when `BeginChild` reports that
  its contents are clipped.

Do not retain a card scope across frames. The reusable window style scope is the only long-lived
scope and should be created once per window.
