using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

public static class SentinelUi
{
    public static void SectionHeader(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ImGui.TextColored(SentinelPalette.HeaderGold, text.ToUpperInvariant());
        ImGui.Separator();
        ImGui.Spacing();
    }

    public static void MutedText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ImGui.TextColored(SentinelPalette.MutedText, text);
    }
}

