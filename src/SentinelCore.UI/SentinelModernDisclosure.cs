using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

/// <summary>A focusable presentation-only disclosure. The caller owns its state and contents.</summary>
public static class SentinelModernDisclosure
{
    public static float Measure(string label, float width, float scale = 1f)
        => MathF.Max(32f * scale, ImGui.CalcTextSize(label, false, MathF.Max(1f, width - 32f * scale)).Y + 14f * scale);

    public static bool Draw(string id, string label, ref bool expanded, float scale = 1f, bool accent = true)
    {
        var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var size = new Vector2(width, Measure(label, width, scale));
        var minimum = ImGui.GetCursorScreenPos();
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
        bool changed;
        try { changed = ImGui.Button("##Disclosure." + id, size); }
        finally { ImGui.PopStyleVar(); ImGui.PopStyleColor(3); }
        if (changed) expanded = !expanded;
        var draw = ImGui.GetWindowDrawList();
        var colour = accent ? SentinelModernPalette.Accent : SentinelModernPalette.Text;
        if (ImGui.IsItemHovered() || SentinelModernNavigationPresentation.HasVisibleNavigationFocus())
            draw.AddRectFilled(minimum, minimum + size,
                ImGui.ColorConvertFloat4ToU32(SentinelModernPaint.WithAlpha(SentinelModernPalette.Accent, 0.12f)), 8f * scale);
        if (SentinelModernNavigationPresentation.HasVisibleNavigationFocus())
            draw.AddRect(minimum, minimum + size, ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Accent),
                8f * scale, ImDrawFlags.None, scale);
        var center = minimum + new Vector2(10f * scale, size.Y * 0.5f);
        var packed = ImGui.ColorConvertFloat4ToU32(colour);
        if (expanded) SentinelModernNavigationPresentation.Chevron(draw, center, 8f * scale, packed, false);
        else
        {
            draw.AddLine(center + new Vector2(-2f, -4f) * scale, center + new Vector2(2f, 0f) * scale, packed, 1.5f * scale);
            draw.AddLine(center + new Vector2(2f, 0f) * scale, center + new Vector2(-2f, 4f) * scale, packed, 1.5f * scale);
        }
        draw.AddText(ImGui.GetFont(), ImGui.GetFontSize(), minimum + new Vector2(25f, 7f) * scale,
            packed, label, MathF.Max(1f, width - 32f * scale));
        return changed;
    }
}
