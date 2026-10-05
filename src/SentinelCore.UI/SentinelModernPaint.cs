using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

/// <summary>
/// Low-allocation draw-list primitives used by Sentinel Modern 2 surfaces.
/// </summary>
public static class SentinelModernPaint
{
    public static void GradientSurface(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        Vector4 top,
        Vector4 bottom,
        float rounding)
    {
        ValidateRectangle(minimum, maximum);
        ValidateNonNegative(rounding, nameof(rounding));

        var vertexStart = drawList.VtxBuffer.Size;
        drawList.AddRectFilled(
            minimum,
            maximum,
            Colour(new Vector4(1f, 1f, 1f, MathF.Max(top.W, bottom.W))),
            rounding);
        var vertexEnd = drawList.VtxBuffer.Size;
        ImGuiP.ShadeVertsLinearColorGradientKeepAlpha(
            drawList,
            vertexStart,
            vertexEnd,
            minimum,
            new Vector2(minimum.X, maximum.Y),
            Opaque(top),
            Opaque(bottom));
    }

    public static void GlassCard(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        float rounding,
        Vector4? accent = null,
        float accentStrength = 0f,
        bool elevated = false)
    {
        ValidateUnit(accentStrength, nameof(accentStrength));
        var tint = accent ?? SentinelModernPalette.Accent;
        if (elevated)
            DropShadow(drawList, minimum, maximum, rounding, 8f, 0.34f);

        var top = Vector4.Lerp(SentinelModernPalette.SurfaceGlassTop, tint, accentStrength * 0.24f);
        var bottom = Vector4.Lerp(SentinelModernPalette.SurfaceGlassBottom, tint, accentStrength * 0.12f);
        GradientSurface(drawList, minimum, maximum, top, bottom, rounding);
        TopHighlight(drawList, minimum, maximum, rounding);
        drawList.AddRect(
            minimum,
            maximum,
            Colour(Vector4.Lerp(SentinelModernPalette.Border, tint, accentStrength * 0.55f)),
            rounding,
            ImDrawFlags.RoundCornersAll,
            1f);
    }

    public static void DropShadow(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        float rounding,
        float spread,
        float opacity)
    {
        ValidateRectangle(minimum, maximum);
        ValidateNonNegative(rounding, nameof(rounding));
        ValidateNonNegative(spread, nameof(spread));
        ValidateUnit(opacity, nameof(opacity));

        for (var layer = 3; layer >= 1; layer--)
        {
            var amount = spread * layer / 3f;
            var grow = new Vector2(amount, amount * 0.72f);
            var offset = new Vector2(0f, amount * 0.48f);
            var alpha = opacity * (4 - layer) / 12f;
            drawList.AddRectFilled(
                minimum - grow + offset,
                maximum + grow + offset,
                Colour(WithAlpha(SentinelModernPalette.Shadow, alpha)),
                rounding + amount);
        }
    }

    public static void AccentGlow(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        float rounding,
        Vector4 colour,
        float intensity = 1f)
    {
        ValidateUnit(intensity, nameof(intensity));
        for (var layer = 3; layer >= 1; layer--)
        {
            var grow = new Vector2(layer * 2.5f);
            drawList.AddRectFilled(
                minimum - grow,
                maximum + grow,
                Colour(WithAlpha(colour, intensity * (4 - layer) * 0.025f)),
                rounding + grow.X);
        }
    }

    public static void TopHighlight(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        float rounding,
        float opacity = 1f)
    {
        ValidateUnit(opacity, nameof(opacity));
        var inset = MathF.Min(rounding, (maximum.X - minimum.X) * 0.25f);
        drawList.AddLine(
            new Vector2(minimum.X + inset, minimum.Y + 1f),
            new Vector2(maximum.X - inset, minimum.Y + 1f),
            Colour(WithAlpha(SentinelModernPalette.Highlight, SentinelModernPalette.Highlight.W * opacity)),
            1f);
    }

    public static void Hairline(
        ImDrawListPtr drawList,
        Vector2 start,
        Vector2 end,
        Vector4? colour = null,
        float thickness = 1f)
    {
        if (!float.IsFinite(thickness) || thickness <= 0f)
            throw new ArgumentOutOfRangeException(nameof(thickness));
        drawList.AddLine(start, end, Colour(colour ?? SentinelModernPalette.Border), thickness);
    }

    public static void Pill(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        Vector4 fill,
        Vector4 border)
    {
        ValidateRectangle(minimum, maximum);
        var rounding = (maximum.Y - minimum.Y) * 0.5f;
        drawList.AddRectFilled(minimum, maximum, Colour(fill), rounding);
        drawList.AddRect(minimum, maximum, Colour(border), rounding, ImDrawFlags.RoundCornersAll, 1f);
    }

    public static void ProgressBar(
        ImDrawListPtr drawList,
        Vector2 minimum,
        Vector2 maximum,
        float fraction,
        Vector4? colour = null)
    {
        ValidateRectangle(minimum, maximum);
        if (!float.IsFinite(fraction))
            throw new ArgumentOutOfRangeException(nameof(fraction));

        fraction = Math.Clamp(fraction, 0f, 1f);
        var height = maximum.Y - minimum.Y;
        var rounding = height * 0.5f;
        drawList.AddRectFilled(minimum, maximum, Colour(WithAlpha(SentinelModernPalette.Surface, 0.90f)), rounding);
        drawList.AddRect(minimum, maximum, Colour(WithAlpha(SentinelModernPalette.Border, 0.70f)), rounding, ImDrawFlags.RoundCornersAll, 1f);
        if (fraction <= 0f)
            return;

        var fillMaximum = new Vector2(
            minimum.X + MathF.Max(height, (maximum.X - minimum.X) * fraction),
            maximum.Y);
        GradientSurface(
            drawList,
            minimum,
            fillMaximum,
            Lighten(colour ?? SentinelModernPalette.Accent, 0.18f),
            colour ?? SentinelModernPalette.Accent,
            rounding);
    }

    public static void StatusDot(
        ImDrawListPtr drawList,
        Vector2 centre,
        float radius,
        Vector4 colour,
        float haloOpacity = 0.20f)
    {
        if (!float.IsFinite(radius) || radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));
        ValidateUnit(haloOpacity, nameof(haloOpacity));
        drawList.AddCircleFilled(centre, radius * 2.15f, Colour(WithAlpha(colour, haloOpacity)), 24);
        drawList.AddCircleFilled(centre, radius, Colour(colour), 20);
    }

    public static Vector4 WithAlpha(Vector4 colour, float alpha)
        => new(colour.X, colour.Y, colour.Z, Math.Clamp(alpha, 0f, 1f));

    public static Vector4 Lighten(Vector4 colour, float amount)
    {
        ValidateUnit(amount, nameof(amount));
        return Vector4.Lerp(colour, new Vector4(1f, 1f, 1f, colour.W), amount);
    }

    private static uint Colour(Vector4 colour) => ImGui.ColorConvertFloat4ToU32(colour);

    private static uint Opaque(Vector4 colour)
        => Colour(new Vector4(colour.X, colour.Y, colour.Z, 1f));

    private static void ValidateRectangle(Vector2 minimum, Vector2 maximum)
    {
        if (!float.IsFinite(minimum.X) || !float.IsFinite(minimum.Y)
            || !float.IsFinite(maximum.X) || !float.IsFinite(maximum.Y)
            || maximum.X < minimum.X || maximum.Y < minimum.Y)
            throw new ArgumentOutOfRangeException(nameof(maximum));
    }

    private static void ValidateUnit(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidateNonNegative(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
