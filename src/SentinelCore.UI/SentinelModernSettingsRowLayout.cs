using System.Numerics;

namespace SentinelCore.UI;

/// <summary>
/// Logical sizing policy for a responsive Sentinel Modern settings row.
/// </summary>
public readonly record struct SentinelModernSettingsRowLayoutOptions(
    float PreferredControlWidth = 180f,
    float MinimumControlWidth = 120f,
    float MinimumTextWidth = 180f,
    float HorizontalPadding = 13f,
    float VerticalPadding = 10f,
    float ColumnGap = 18f,
    float StackedControlGap = 10f,
    float DescriptionGap = 4f,
    float MinimumHeight = 42f)
{
    public static SentinelModernSettingsRowLayoutOptions Default { get; } = new(
        180f,
        120f,
        180f,
        13f,
        10f,
        18f,
        10f,
        4f,
        42f);
}

/// <summary>
/// Horizontal regions resolved before wrapped text is measured.
/// </summary>
public readonly record struct SentinelModernSettingsRowColumns(
    bool IsStacked,
    float TextWidth,
    float ControlWidth,
    float HorizontalPadding,
    float ColumnGap);

/// <summary>
/// Final renderer-independent settings-row geometry.
/// </summary>
public readonly record struct SentinelModernSettingsRowLayoutResult(
    Vector2 Size,
    bool IsStacked,
    Vector2 TextOffset,
    float TextWidth,
    Vector2 ControlOffset,
    float ControlWidth);

/// <summary>
/// Resolves a two-column settings row when it fits and a safe vertical stack when it does not.
/// </summary>
public static class SentinelModernSettingsRowLayout
{
    public static SentinelModernSettingsRowColumns ResolveColumns(
        float availableWidth,
        float scale = 1f,
        SentinelModernSettingsRowLayoutOptions options = default)
    {
        ValidatePositive(availableWidth, nameof(availableWidth));
        ValidatePositive(scale, nameof(scale));
        options = Normalize(options);
        Validate(options);

        var horizontalPadding = options.HorizontalPadding * scale;
        var columnGap = options.ColumnGap * scale;
        var usableWidth = MathF.Max(1f, availableWidth - (horizontalPadding * 2f));
        var minimumTextWidth = options.MinimumTextWidth * scale;
        var minimumControlWidth = options.MinimumControlWidth * scale;
        var isStacked = usableWidth < minimumTextWidth + columnGap + minimumControlWidth;

        if (isStacked)
        {
            return new SentinelModernSettingsRowColumns(
                true,
                usableWidth,
                usableWidth,
                horizontalPadding,
                columnGap);
        }

        var preferredControlWidth = options.PreferredControlWidth * scale;
        var controlWidth = MathF.Min(
            preferredControlWidth,
            MathF.Max(minimumControlWidth, usableWidth - columnGap - minimumTextWidth));
        var textWidth = MathF.Max(1f, usableWidth - columnGap - controlWidth);
        return new SentinelModernSettingsRowColumns(
            false,
            textWidth,
            controlWidth,
            horizontalPadding,
            columnGap);
    }

    public static SentinelModernSettingsRowLayoutResult Resolve(
        float availableWidth,
        float labelHeight,
        float descriptionHeight,
        float controlHeight,
        bool hasDescription,
        float scale = 1f,
        SentinelModernSettingsRowLayoutOptions options = default)
    {
        ValidatePositive(availableWidth, nameof(availableWidth));
        ValidateNonNegative(labelHeight, nameof(labelHeight));
        ValidateNonNegative(descriptionHeight, nameof(descriptionHeight));
        ValidatePositive(controlHeight, nameof(controlHeight));
        ValidatePositive(scale, nameof(scale));
        options = Normalize(options);
        Validate(options);

        var columns = ResolveColumns(availableWidth, scale, options);
        var verticalPadding = options.VerticalPadding * scale;
        var descriptionGap = hasDescription ? options.DescriptionGap * scale : 0f;
        var textHeight = labelHeight + descriptionGap + (hasDescription ? descriptionHeight : 0f);
        var minimumHeight = options.MinimumHeight * scale;

        if (!columns.IsStacked)
        {
            var contentHeight = MathF.Max(textHeight, controlHeight);
            var height = MathF.Max(minimumHeight, contentHeight + (verticalPadding * 2f));
            return new SentinelModernSettingsRowLayoutResult(
                new Vector2(availableWidth, height),
                false,
                new Vector2(columns.HorizontalPadding, (height - textHeight) * 0.5f),
                columns.TextWidth,
                new Vector2(
                    availableWidth - columns.HorizontalPadding - columns.ControlWidth,
                    (height - controlHeight) * 0.5f),
                columns.ControlWidth);
        }

        var stackedGap = options.StackedControlGap * scale;
        var stackedHeight = MathF.Max(
            minimumHeight,
            verticalPadding + textHeight + stackedGap + controlHeight + verticalPadding);
        return new SentinelModernSettingsRowLayoutResult(
            new Vector2(availableWidth, stackedHeight),
            true,
            new Vector2(columns.HorizontalPadding, verticalPadding),
            columns.TextWidth,
            new Vector2(columns.HorizontalPadding, verticalPadding + textHeight + stackedGap),
            columns.ControlWidth);
    }

    private static SentinelModernSettingsRowLayoutOptions Normalize(
        SentinelModernSettingsRowLayoutOptions options)
        => options == default ? SentinelModernSettingsRowLayoutOptions.Default : options;

    private static void Validate(SentinelModernSettingsRowLayoutOptions options)
    {
        ValidatePositive(options.PreferredControlWidth, nameof(options));
        ValidatePositive(options.MinimumControlWidth, nameof(options));
        ValidatePositive(options.MinimumTextWidth, nameof(options));
        ValidatePositive(options.HorizontalPadding, nameof(options));
        ValidatePositive(options.VerticalPadding, nameof(options));
        ValidatePositive(options.ColumnGap, nameof(options));
        ValidatePositive(options.StackedControlGap, nameof(options));
        ValidatePositive(options.DescriptionGap, nameof(options));
        ValidatePositive(options.MinimumHeight, nameof(options));
        if (options.PreferredControlWidth < options.MinimumControlWidth)
            throw new ArgumentException(
                "Preferred control width cannot be smaller than the minimum control width.",
                nameof(options));
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    private static void ValidateNonNegative(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value < 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}
