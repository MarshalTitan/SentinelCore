using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

/// <summary>Presentation shared by the primary rail and custom window controls.</summary>
public static class SentinelModernNavigationPresentation
{
    public static void Tooltip(string text, float scale = 1f, bool besideItem = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var padding = new Vector2(12f, 8f) * scale;
        var size = ImGui.CalcTextSize(text) + padding * 2f;
        var viewport = ImGui.GetMainViewport();
        var lower = viewport.WorkPos + new Vector2(4f * scale);
        var upper = Vector2.Max(lower, viewport.WorkPos + viewport.WorkSize - size - new Vector2(4f * scale));
        var itemMin = ImGui.GetItemRectMin();
        var itemMax = ImGui.GetItemRectMax();
        var position = besideItem
            ? new Vector2(itemMax.X + 10f * scale, (itemMin.Y + itemMax.Y - size.Y) * 0.5f)
            : new Vector2(itemMin.X, itemMax.Y + 8f * scale);
        if (besideItem && position.X > upper.X) position.X = itemMin.X - size.X - 10f * scale;
        ImGui.SetNextWindowPos(Vector2.Clamp(position, lower, upper), ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 10f * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 0f);
        ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.025f, 0.03f, 0.045f, 0.88f));
        try
        {
            ImGui.BeginTooltip();
            try { ImGui.TextUnformatted(text); }
            finally { ImGui.EndTooltip(); }
        }
        finally { ImGui.PopStyleColor(); ImGui.PopStyleVar(3); }
    }

    public static void Chevron(ImDrawListPtr draw, Vector2 center, float size, uint colour, bool up)
    {
        var half = size * 0.5f;
        var direction = up ? -1f : 1f;
        var edge = center.Y - direction * size * 0.22f;
        var middle = center.Y + direction * size * 0.22f;
        draw.AddLine(new(center.X - half, edge), new(center.X, middle), colour, 1.8f * size / 12f);
        draw.AddLine(new(center.X, middle), new(center.X + half, edge), colour, 1.8f * size / 12f);
    }
}
