# Changelog

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
