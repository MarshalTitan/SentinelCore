using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

public sealed class SentinelStyleScope : IDisposable
{
    private readonly int colourCount;
    private readonly int variableCount;
    private bool disposed;

    private SentinelStyleScope(int colourCount, int variableCount)
    {
        this.colourCount = colourCount;
        this.variableCount = variableCount;
    }

    public static SentinelStyleScope PushWindow(float scale = 1f)
    {
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));

        ImGui.PushStyleColor(ImGuiCol.WindowBg, SentinelPalette.WindowBackground);
        ImGui.PushStyleColor(ImGuiCol.Border, SentinelPalette.Border);
        ImGui.PushStyleColor(ImGuiCol.Text, SentinelPalette.Text);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 9f) * scale);
        return new SentinelStyleScope(3, 3);
    }

    public static SentinelStyleScope PushButton(SentinelRolePalette palette, float scale = 1f)
    {
        ArgumentNullException.ThrowIfNull(palette);
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));

        ImGui.PushStyleColor(ImGuiCol.Button, palette.Idle);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, palette.Hover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, palette.Active);
        ImGui.PushStyleColor(ImGuiCol.Border, palette.Border);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f * scale);
        return new SentinelStyleScope(4, 2);
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        if (variableCount > 0)
            ImGui.PopStyleVar(variableCount);
        if (colourCount > 0)
            ImGui.PopStyleColor(colourCount);
    }
}

