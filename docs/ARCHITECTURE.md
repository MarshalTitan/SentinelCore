# Architecture

## Dependency direction

```text
MarshalTitan.SentinelCore.Dalamud ─┐
                                  ├──> MarshalTitan.SentinelCore
MarshalTitan.SentinelCore.UI ─────┘
```

`SentinelCore` is platform-neutral. `SentinelCore.Dalamud` and `SentinelCore.UI` compile against Dalamud API 15 but do not implement a plugin entry point.

## Generic library

- **Identity and versioning:** validated four-part Sentinel versions, stable internal/display identity, and assembly-derived identity.
- **Configuration:** a store abstraction plus a thread-safe coordinator with normalization/migration, delayed save, explicit flush, and final flush during disposal.
- **Diagnostics:** a bounded chronological diagnostic buffer and changed/throttled diagnostic tracker.
- **Lifecycle:** idempotent LIFO disposal that continues after individual cleanup failures.
- **Job metadata model:** neutral job categories and role hues shared by Dalamud and UI code.
- **IPC conventions:** namespaced, versioned endpoint names and structured unavailable/unsupported/rejected/failure results.

## Dalamud library

- Wraps `IDalamudPluginInterface.GetPluginConfig` and `SavePluginConfig` behind the generic store.
- Adapts `IPluginLog` to the generic Sentinel logger.
- Reads the current `Lumina.Excel.Sheets.ClassJob` sheet through `IDataManager` and derives roles dynamically. New jobs appear without a Core update when their game data uses an existing role/category.
- Provides guarded IPC invocation helpers; missing or unloaded providers are expected conditions rather than plugin-crashing exceptions.

## UI library

- Centralizes the dark charcoal/gold Sentinel surface palette.
- Exposes the established role hues: tank blue, healer green, melee red, physical ranged orange, magical ranged purple, and neutral grey.
- Uses disposable style scopes so every pushed ImGui colour/variable is popped exactly once.
- Provides only small primitives; plugins retain control of their window layouts and behavior.

## Compatibility policy

- Major/minor package changes follow semantic compatibility expectations.
- Assembly and release identity retain four numeric components to match Sentinel conventions.
- Existing members are not removed or behaviorally redefined in a patch release.
- Patch-sensitive game or Dalamud integrations stay in the Dalamud assembly, never the generic assembly.
- IPC endpoint names include a contract major version. A breaking payload change requires a new endpoint major.

