using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

/// <summary>
/// Reusable, balanced ImGui style scope for Sentinel Modern surfaces.
/// Create it once per window, call <see cref="Push"/> in PreDraw, and call
/// <see cref="Pop"/> in PostDraw. Disposing also pops an active scope.
/// </summary>
public sealed class SentinelModernStyleScope : IDisposable
{
    private int colourCount;
    private int variableCount;
    private bool active;
    private bool disposed;

    public bool IsActive => active;

    public void Push(float scale = 1f)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (active)
            throw new InvalidOperationException("The Sentinel Modern style scope is already active.");
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));

        try
        {
            PushColour(ImGuiCol.WindowBg, SentinelModernPalette.Canvas);
            PushColour(ImGuiCol.ChildBg, WithAlpha(SentinelModernPalette.Surface, 0.76f));
            PushColour(ImGuiCol.PopupBg, SentinelModernPalette.SurfaceRaised);
            PushColour(ImGuiCol.Border, SentinelModernPalette.Border);
            PushColour(ImGuiCol.BorderShadow, Vector4.Zero);
            PushColour(ImGuiCol.Text, SentinelModernPalette.Text);
            PushColour(ImGuiCol.TextDisabled, SentinelModernPalette.Muted);
            PushColour(ImGuiCol.TitleBg, new Vector4(0.035f, 0.045f, 0.090f, 1f));
            PushColour(ImGuiCol.TitleBgActive, new Vector4(0.045f, 0.057f, 0.110f, 1f));
            PushColour(ImGuiCol.TitleBgCollapsed, new Vector4(0.035f, 0.045f, 0.090f, 1f));
            PushColour(ImGuiCol.FrameBg, SentinelModernPalette.SurfaceRaised);
            PushColour(ImGuiCol.FrameBgHovered, SentinelModernPalette.SurfaceHover);
            PushColour(ImGuiCol.FrameBgActive, SentinelModernPalette.SurfaceActive);
            PushColour(ImGuiCol.CheckMark, SentinelModernPalette.AccentStrong);
            PushColour(ImGuiCol.SliderGrab, SentinelModernPalette.Accent);
            PushColour(ImGuiCol.SliderGrabActive, SentinelModernPalette.AccentStrong);
            PushColour(ImGuiCol.Button, SentinelModernPalette.SurfaceRaised);
            PushColour(ImGuiCol.ButtonHovered, SentinelModernPalette.SurfaceHover);
            PushColour(ImGuiCol.ButtonActive, new Vector4(0.145f, 0.260f, 0.485f, 1f));
            PushColour(ImGuiCol.Header, SentinelModernPalette.SurfaceRaised);
            PushColour(ImGuiCol.HeaderHovered, SentinelModernPalette.SurfaceHover);
            PushColour(ImGuiCol.HeaderActive, new Vector4(0.120f, 0.205f, 0.375f, 1f));
            PushColour(ImGuiCol.Separator, SentinelModernPalette.Border);
            PushColour(ImGuiCol.SeparatorHovered, SentinelModernPalette.BorderBright);
            PushColour(ImGuiCol.SeparatorActive, SentinelModernPalette.Accent);
            PushColour(ImGuiCol.ScrollbarBg, new Vector4(0.020f, 0.026f, 0.055f, 0.55f));
            PushColour(ImGuiCol.ScrollbarGrab, new Vector4(0.175f, 0.225f, 0.365f, 0.95f));
            PushColour(ImGuiCol.ScrollbarGrabHovered, SentinelModernPalette.BorderBright);
            PushColour(ImGuiCol.ScrollbarGrabActive, SentinelModernPalette.Accent);
            PushColour(ImGuiCol.ResizeGrip, WithAlpha(SentinelModernPalette.Accent, 0.25f));
            PushColour(ImGuiCol.ResizeGripHovered, WithAlpha(SentinelModernPalette.Accent, 0.65f));
            PushColour(ImGuiCol.ResizeGripActive, SentinelModernPalette.Accent);

            PushVariable(ImGuiStyleVar.WindowRounding, 10f * scale);
            PushVariable(ImGuiStyleVar.ChildRounding, 9f * scale);
            PushVariable(ImGuiStyleVar.PopupRounding, 8f * scale);
            PushVariable(ImGuiStyleVar.FrameRounding, 7f * scale);
            PushVariable(ImGuiStyleVar.GrabRounding, 8f * scale);
            PushVariable(ImGuiStyleVar.ScrollbarRounding, 8f * scale);
            PushVariable(ImGuiStyleVar.WindowBorderSize, 1f * scale);
            PushVariable(ImGuiStyleVar.ChildBorderSize, 1f * scale);
            PushVariable(ImGuiStyleVar.FrameBorderSize, 1f * scale);
            PushVariable(ImGuiStyleVar.WindowPadding, new Vector2(12f, 10f) * scale);
            PushVariable(ImGuiStyleVar.FramePadding, new Vector2(10f, 6f) * scale);
            PushVariable(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 7f) * scale);
            PushVariable(ImGuiStyleVar.ItemInnerSpacing, new Vector2(7f, 5f) * scale);
            PushVariable(ImGuiStyleVar.ScrollbarSize, 11f * scale);
            PushVariable(ImGuiStyleVar.GrabMinSize, 12f * scale);
            active = true;
        }
        catch
        {
            PopCore();
            throw;
        }
    }

    public void Pop()
    {
        if (!active)
            return;

        PopCore();
        active = false;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        Pop();
        disposed = true;
    }

    private static Vector4 WithAlpha(Vector4 colour, float alpha)
        => new(colour.X, colour.Y, colour.Z, alpha);

    private void PushColour(ImGuiCol colour, Vector4 value)
    {
        ImGui.PushStyleColor(colour, value);
        colourCount++;
    }

    private void PushVariable(ImGuiStyleVar variable, float value)
    {
        ImGui.PushStyleVar(variable, value);
        variableCount++;
    }

    private void PushVariable(ImGuiStyleVar variable, Vector2 value)
    {
        ImGui.PushStyleVar(variable, value);
        variableCount++;
    }

    private void PopCore()
    {
        if (variableCount > 0)
        {
            ImGui.PopStyleVar(variableCount);
            variableCount = 0;
        }

        if (colourCount > 0)
        {
            ImGui.PopStyleColor(colourCount);
            colourCount = 0;
        }
    }
}
