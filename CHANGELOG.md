# Changelog

## 0.3.1.0

- Adds the canonical single-custom-header window policy, full-bleed app-shell style scope, and
  opt-in draggable header region so Modern consumers can remove the duplicate native title bar
  while retaining consumer-owned position, resizing, close, and collapse state.
- Refines Modern 2 into one continuous application surface with quieter separators instead of
  boxed rail/sidebar/content regions; the previous segmented treatment remains selectable.
- Adds retained primary-navigation icon render callbacks for scalable icon fonts or consumer-owned
  textures while Core continues to own hit targets, tooltips, state colours, badges, and motion.
- Adds clean text-only secondary navigation rows so category labels no longer need decorative
  letter prefixes; the existing icon-and-label overload remains available.
- Reworks settings-row geometry into a tested responsive resolver with wrapped descriptions,
  stable control columns, and safe narrow-width stacking so combos and sliders cannot cover text.
- Makes the procedural blue, violet, and teal ambient circles noticeably more visible with a small
  fixed number of layered fills and ring highlights while retaining deterministic reduced motion.
- Keeps Classic Sentinel, the original Modern configuration shell, and every `0.3.0` public entry
  point available so consumers can adopt the polished shell incrementally.

## 0.3.0.0

- Adds the opt-in `SentinelModernAppShell` application layout with a compact single-row header,
  fixed slim icon rail, optional secondary category sidebar, content pane, and optional action dock.
- Adds per-window, frame-rate-independent `SentinelModernMotion` for hover, selected navigation,
  page reveal, status pulse, and ambient drift with deterministic reduced-motion behavior.
- Adds the animated, clipped, asset-free Modern 2 ambient background while preserving the original
  static `DrawRings` API for existing consumers.
- Adds shared glass/gradient surfaces, shadows, glows, highlights, separators, pills, progress bars,
  status dots, glass cards, compact settings rows, and controller-aware switches.
- Adds refined ready, enabled, running, warning, error, neutral, accent, and custom status pills.
- Adds renderer-independent coverage for shell geometry, minimum sizes, non-stacking navigation,
  header scroll policy, UI scaling, reduced motion, motion state lifecycle, and status tones.
- Keeps Classic Sentinel and every `0.2.1` Sentinel Modern API source-compatible and behaviorally
  unchanged so consumers migrate explicitly.

## 0.2.1.0

- Keeps grouped Sentinel Modern navigation in a dedicated left sidebar at every supported window
  size, matching the approved Sentinel HUD configuration layout.
- Retains stacked navigation as an explicit opt-in for non-standard consumers rather than the
  shared shell's narrow-window default.
- Increases the standard header height and prevents the header child from showing scrollbars.
- Adds regression coverage for the left-sidebar default and optional stacked layout.

## 0.2.0.0

- Promotes the approved Sentinel HUD `0.8.3.0` visual language into `SentinelCore.UI` as the opt-in
  Sentinel Modern design system.
- Adds semantic Modern palette tokens, a reusable balanced style scope, responsive configuration
  shell, grouped navigation, cards, section and page headings, switches, status chips, and optional
  procedural ambience.
- Adds renderer-independent theme state and responsive-layout tests.
- Keeps every `0.1.0.0` API and the existing Classic Sentinel theme unchanged.

## 0.1.0.0

- Initial versioned Sentinel development libraries.
