using System.Numerics;

namespace SentinelCore.UI;

public readonly record struct SentinelModernLayoutOptions(
    float SidebarRatio = 0.225f,
    float MinimumSidebarWidth = 182f,
    float MaximumSidebarWidth = 218f,
    float CompactBreakpoint = 680f,
    float CompactNavigationHeight = 210f)
{
    public static SentinelModernLayoutOptions Default { get; } = new(
        0.225f,
        182f,
        218f,
        680f,
        210f);
}

public readonly record struct SentinelModernLayoutResult(
    bool IsCompact,
    Vector2 NavigationSize,
    Vector2 ContentSize);

/// <summary>
/// Renderer-independent responsive layout calculations for Sentinel Modern configuration windows.
/// </summary>
public static class SentinelModernLayout
{
    public static SentinelModernLayoutResult Resolve(
        Vector2 available,
        float scale = 1f)
        => Resolve(available, scale, SentinelModernLayoutOptions.Default);

    public static SentinelModernLayoutResult Resolve(
        Vector2 available,
        float scale,
        SentinelModernLayoutOptions options)
    {
        ValidatePositiveFinite(scale, nameof(scale));
        ValidateAvailable(available);
        ValidateOptions(options);

        var compact = available.X < options.CompactBreakpoint * scale;
        if (compact)
        {
            var navigationHeight = Math.Clamp(
                options.CompactNavigationHeight * scale,
                0f,
                MathF.Max(0f, available.Y * 0.42f));
            return new SentinelModernLayoutResult(
                true,
                new Vector2(available.X, navigationHeight),
                new Vector2(available.X, MathF.Max(0f, available.Y - navigationHeight)));
        }

        var sidebarWidth = Math.Clamp(
            available.X * options.SidebarRatio,
            options.MinimumSidebarWidth * scale,
            options.MaximumSidebarWidth * scale);
        return new SentinelModernLayoutResult(
            false,
            new Vector2(sidebarWidth, available.Y),
            new Vector2(MathF.Max(0f, available.X - sidebarWidth), available.Y));
    }

    private static void ValidateAvailable(Vector2 available)
    {
        if (!float.IsFinite(available.X) || !float.IsFinite(available.Y)
            || available.X < 0f || available.Y < 0f)
            throw new ArgumentOutOfRangeException(nameof(available));
    }

    private static void ValidateOptions(SentinelModernLayoutOptions options)
    {
        ValidatePositiveFinite(options.SidebarRatio, nameof(options));
        ValidatePositiveFinite(options.MinimumSidebarWidth, nameof(options));
        ValidatePositiveFinite(options.MaximumSidebarWidth, nameof(options));
        ValidatePositiveFinite(options.CompactBreakpoint, nameof(options));
        ValidatePositiveFinite(options.CompactNavigationHeight, nameof(options));
        if (options.MinimumSidebarWidth > options.MaximumSidebarWidth)
            throw new ArgumentException("Minimum sidebar width cannot exceed maximum sidebar width.", nameof(options));
    }

    private static void ValidatePositiveFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
