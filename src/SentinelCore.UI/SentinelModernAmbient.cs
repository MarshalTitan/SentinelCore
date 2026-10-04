using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

/// <summary>
/// Procedural, asset-free ambient background treatment for Sentinel Modern windows.
/// Call immediately after the window begins and before drawing child surfaces.
/// </summary>
public static class SentinelModernAmbient
{
    public static void DrawRings(float scale = 1f, float intensity = 1f)
    {
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(intensity) || intensity < 0f || intensity > 1f)
            throw new ArgumentOutOfRangeException(nameof(intensity));
        if (intensity <= 0f)
            return;

        var drawList = ImGui.GetWindowDrawList();
        var position = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();

        DrawGlow(
            drawList,
            position + new Vector2(size.X * 0.18f, size.Y * 0.18f),
            MathF.Max(105f * scale, size.X * 0.20f),
            new Vector3(0.07f, 0.28f, 0.63f),
            0.15f * intensity);
        DrawGlow(
            drawList,
            position + new Vector2(size.X * 0.80f, size.Y * 0.27f),
            MathF.Max(95f * scale, size.X * 0.17f),
            new Vector3(0.57f, 0.12f, 0.58f),
            0.13f * intensity);
        DrawGlow(
            drawList,
            position + new Vector2(size.X * 0.59f, size.Y * 0.88f),
            MathF.Max(115f * scale, size.X * 0.21f),
            new Vector3(0.12f, 0.48f, 0.49f),
            0.10f * intensity);
    }

    private static void DrawGlow(
        ImDrawListPtr drawList,
        Vector2 centre,
        float radius,
        Vector3 colour,
        float opacity)
    {
        drawList.AddCircleFilled(
            centre,
            radius,
            ImGui.ColorConvertFloat4ToU32(new Vector4(colour, opacity * 0.38f)),
            64);
        drawList.AddCircleFilled(
            centre,
            radius * 0.67f,
            ImGui.ColorConvertFloat4ToU32(new Vector4(colour, opacity * 0.62f)),
            64);
        drawList.AddCircleFilled(
            centre,
            radius * 0.36f,
            ImGui.ColorConvertFloat4ToU32(new Vector4(colour, opacity)),
            64);
    }
}
