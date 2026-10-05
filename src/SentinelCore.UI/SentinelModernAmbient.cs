using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

/// <summary>
/// Procedural, asset-free ambient background treatment for Sentinel Modern windows.
/// Call immediately after the window begins and before drawing child surfaces.
/// </summary>
public static class SentinelModernAmbient
{
    private const int AnimatedGlowLayers = 4;

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

    /// <summary>
    /// Draws the quieter, slowly drifting Sentinel Modern 2 ambience. Motion stops at a stable
    /// composition when <paramref name="reducedMotion"/> is enabled.
    /// </summary>
    public static void DrawAnimated(
        SentinelModernMotion motion,
        Vector2 minimum,
        Vector2 maximum,
        float scale = 1f,
        float intensity = 0.72f,
        bool reducedMotion = false)
    {
        ArgumentNullException.ThrowIfNull(motion);
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(intensity) || intensity < 0f || intensity > 1f)
            throw new ArgumentOutOfRangeException(nameof(intensity));
        if (!float.IsFinite(minimum.X) || !float.IsFinite(minimum.Y)
            || !float.IsFinite(maximum.X) || !float.IsFinite(maximum.Y)
            || maximum.X < minimum.X || maximum.Y < minimum.Y)
            throw new ArgumentOutOfRangeException(nameof(maximum));
        if (intensity <= 0f)
            return;

        var width = maximum.X - minimum.X;
        var height = maximum.Y - minimum.Y;
        var blueX = reducedMotion ? 0f : motion.Wave(19f, 0.2f);
        var blueY = reducedMotion ? 0f : motion.Wave(27f, 1.3f);
        var violetX = reducedMotion ? 0f : motion.Wave(23f, 2.1f);
        var violetY = reducedMotion ? 0f : motion.Wave(31f, 0.7f);
        var tealX = reducedMotion ? 0f : motion.Wave(29f, 4.1f);
        var tealY = reducedMotion ? 0f : motion.Wave(37f, 2.8f);
        var drawList = ImGui.GetWindowDrawList();

        drawList.PushClipRect(minimum, maximum, true);
        try
        {
            DrawAnimatedGlow(
                drawList,
                minimum + new Vector2(
                    width * (0.18f + (0.035f * blueX)),
                    height * (0.18f + (0.025f * blueY))),
                MathF.Max(125f * scale, width * 0.31f),
                SentinelModernPalette.Accent,
                0.052f * intensity);
            DrawAnimatedGlow(
                drawList,
                minimum + new Vector2(
                    width * (0.84f + (0.028f * violetX)),
                    height * (0.30f + (0.035f * violetY))),
                MathF.Max(110f * scale, width * 0.27f),
                SentinelModernPalette.Violet,
                0.038f * intensity);
            DrawAnimatedGlow(
                drawList,
                minimum + new Vector2(
                    width * (0.57f + (0.040f * tealX)),
                    height * (0.94f + (0.020f * tealY))),
                MathF.Max(135f * scale, width * 0.30f),
                SentinelModernPalette.Teal,
                0.032f * intensity);
        }
        finally
        {
            drawList.PopClipRect();
        }
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

    private static void DrawAnimatedGlow(
        ImDrawListPtr drawList,
        Vector2 centre,
        float radius,
        Vector4 colour,
        float peakOpacity)
    {
        for (var layer = AnimatedGlowLayers; layer >= 1; layer--)
        {
            var layerRatio = layer / (float)AnimatedGlowLayers;
            var layerRadius = radius * layerRatio;
            var alpha = peakOpacity * (1f - ((layer - 1f) / AnimatedGlowLayers));
            drawList.AddCircleFilled(
                centre,
                layerRadius,
                ImGui.ColorConvertFloat4ToU32(SentinelModernPaint.WithAlpha(colour, alpha)),
                48);
        }
    }
}
