using System.Numerics;

namespace SentinelCore.UI;

/// <summary>
/// Logical, pre-scale dimensions for the Sentinel Modern application shell.
/// Navigation is always horizontal: rail, optional sidebar, then content.
/// </summary>
public readonly record struct SentinelModernAppLayoutOptions(
    float HeaderHeight = 56f,
    float RailWidth = 64f,
    float RailButtonSize = 42f,
    float SecondarySidebarWidth = 196f,
    float ActionDockHeight = 72f,
    float ContentPadding = 20f,
    float MinimumContentWidth = 320f,
    float MinimumContentHeight = 280f)
{
    public static SentinelModernAppLayoutOptions Default { get; } = new(
        56f,
        64f,
        42f,
        196f,
        72f,
        20f,
        320f,
        280f);
}

public readonly record struct SentinelModernAppLayoutResult(
    Vector2 AvailableSize,
    float HeaderHeight,
    float BodyHeight,
    float RailWidth,
    float RailButtonSize,
    float SecondarySidebarWidth,
    float ContentWidth,
    float ContentHeight,
    float ActionDockHeight,
    float ContentPadding,
    bool IsUsable,
    bool NavigationIsStacked,
    bool HeaderAllowsScrolling);

/// <summary>
/// Renderer-independent geometry for Sentinel Modern 2. The resolver never stacks navigation.
/// </summary>
public static class SentinelModernAppLayout
{
    public static SentinelModernAppLayoutResult Resolve(
        Vector2 available,
        float scale = 1f,
        bool hasSecondarySidebar = false,
        bool hasActionDock = false,
        SentinelModernAppLayoutOptions options = default)
    {
        ValidateAvailable(available);
        ValidatePositive(scale, nameof(scale));
        options = Normalize(options);
        Validate(options);

        var header = options.HeaderHeight * scale;
        var rail = options.RailWidth * scale;
        var railButton = options.RailButtonSize * scale;
        var secondary = hasSecondarySidebar ? options.SecondarySidebarWidth * scale : 0f;
        var dock = hasActionDock ? options.ActionDockHeight * scale : 0f;
        var padding = options.ContentPadding * scale;
        var bodyHeight = MathF.Max(0f, available.Y - header - dock);
        var contentWidth = MathF.Max(0f, available.X - rail - secondary);
        var contentHeight = bodyHeight;
        var usable = contentWidth >= options.MinimumContentWidth * scale
            && contentHeight >= options.MinimumContentHeight * scale;

        return new SentinelModernAppLayoutResult(
            available,
            header,
            bodyHeight,
            rail,
            railButton,
            secondary,
            contentWidth,
            contentHeight,
            dock,
            padding,
            usable,
            false,
            false);
    }

    public static Vector2 MinimumWindowSize(
        float scale = 1f,
        bool hasSecondarySidebar = false,
        bool hasActionDock = false,
        SentinelModernAppLayoutOptions options = default)
    {
        ValidatePositive(scale, nameof(scale));
        options = Normalize(options);
        Validate(options);

        var width = options.RailWidth
            + (hasSecondarySidebar ? options.SecondarySidebarWidth : 0f)
            + options.MinimumContentWidth;
        var height = options.HeaderHeight
            + (hasActionDock ? options.ActionDockHeight : 0f)
            + options.MinimumContentHeight;
        return new Vector2(width, height) * scale;
    }

    private static SentinelModernAppLayoutOptions Normalize(SentinelModernAppLayoutOptions options)
        => options == default ? SentinelModernAppLayoutOptions.Default : options;

    private static void ValidateAvailable(Vector2 value)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || value.X < 0f || value.Y < 0f)
            throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void Validate(SentinelModernAppLayoutOptions options)
    {
        ValidatePositive(options.HeaderHeight, nameof(options));
        ValidatePositive(options.RailWidth, nameof(options));
        ValidatePositive(options.RailButtonSize, nameof(options));
        ValidatePositive(options.SecondarySidebarWidth, nameof(options));
        ValidatePositive(options.ActionDockHeight, nameof(options));
        ValidatePositive(options.ContentPadding, nameof(options));
        ValidatePositive(options.MinimumContentWidth, nameof(options));
        ValidatePositive(options.MinimumContentHeight, nameof(options));
        if (options.RailButtonSize > options.RailWidth)
            throw new ArgumentException("Rail button size cannot exceed rail width.", nameof(options));
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
