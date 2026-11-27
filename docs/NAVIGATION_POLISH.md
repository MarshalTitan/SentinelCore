# Navigation presentation - 0.4.1.3

The existing Modern 2 application shell owns a borderless primary icon rail and header controls. Icons retain their original hit areas, native keyboard/gamepad activation and consumer-owned callbacks. A soft circular blue/teal hover highlight is animated through existing motion state; selection remains visible after hover leaves. Reduced motion remains immediate. Keyboard/controller focus has an explicit outline; mouse activation does not retain a focus bubble after the pointer leaves.

`SentinelModernNavigationPresentation.Tooltip` measures text with the current font, adds 12-by-8 logical padding, uses a translucent charcoal popup with rounded corners, and places labels beside the rail within viewport bounds. Tooltips are separate ImGui windows, so the rail's child clipping cannot trim the label. The window header uses drawn chevrons and a larger X; `IsCollapsed` selects the chevron without controlling collapse behavior. The selected navigation label supplies header context when the consumer does not override it.

`SentinelModernDisclosure` renders a native focusable button with a wrapping label and chevron. Its only state is the caller-owned expanded flag. It cannot enable plugins, alter configuration values, invoke gameplay actions or acquire leases. Consumers choose persistence and contents.

This is an additive UI patch. Existing shell constructors, callbacks, identities, theme support and assembly binding version are retained. Generic Core runtime source is unchanged. Consumers pin immutable packages and rebuild; no library is installed as a Dalamud plugin.

Native ImGui regression coverage exercises 24 size/UI-scale/text-scale combinations, each exercised expanded and collapsed, rail clicks and persistent selection, minimize/restore/close callbacks, viewport-edge labels and balanced styles. Consumer suites retain keyboard/controller and persistence checks. Actual FFXIV visual acceptance remains distinct from native renderer validation.
