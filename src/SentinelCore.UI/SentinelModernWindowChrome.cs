using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

/// <summary>
/// Top-level window policy for consumers that use the Modern application header as their only
/// visible title bar. Core does not call the consumer's top-level Begin/End pair.
/// </summary>
public static class SentinelModernWindowChrome
{
    public const ImGuiWindowFlags CustomHeaderFlags =
        ImGuiWindowFlags.NoTitleBar
        | ImGuiWindowFlags.NoCollapse
        | ImGuiWindowFlags.NoScrollbar
        | ImGuiWindowFlags.NoScrollWithMouse;

    /// <summary>
    /// Adds the flags required for a single custom application header without discarding any
    /// plugin-specific flags already configured by the consumer.
    /// </summary>
    public static ImGuiWindowFlags UseCustomHeader(ImGuiWindowFlags existing)
        => existing | CustomHeaderFlags;

    public static bool HasSingleCustomHeader(ImGuiWindowFlags flags)
        => (flags & CustomHeaderFlags) == CustomHeaderFlags;
}
