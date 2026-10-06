using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

public readonly record struct SentinelModernNavBadge(Vector4 Colour, bool Pulse = false);

public readonly record struct SentinelModernNavIconDrawContext(
    ImDrawListPtr DrawList,
    Vector2 Minimum,
    Vector2 Maximum,
    Vector4 Colour,
    float Scale);

public readonly record struct SentinelModernNavItem(
    string Id,
    string? Icon,
    string Label)
{
    public SentinelModernNavBadge? Badge { get; init; }

    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Optional retained icon renderer for Font Awesome glyphs, plugin textures, or other
    /// consumer-owned scalable icon sources. Core continues to own placement and state colours.
    /// </summary>
    public Action<SentinelModernNavIconDrawContext>? DrawIcon { get; init; }
}

/// <summary>
/// Shared icon-centred primary navigation for Sentinel Modern 2.
/// </summary>
public static class SentinelModernIconRail
{
    private const float TopPadding = 10f;
    private const float ButtonGap = 8f;

    public static string? Draw(
        IReadOnlyList<SentinelModernNavItem> items,
        string selectedId,
        SentinelModernMotion motion,
        float buttonSize,
        float scale = 1f)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedId);
        ArgumentNullException.ThrowIfNull(motion);
        ValidatePositive(buttonSize, nameof(buttonSize));
        ValidatePositive(scale, nameof(scale));

        var selectedIndex = -1;
        for (var index = 0; index < items.Count; index++)
        {
            Validate(items[index]);
            if (string.Equals(items[index].Id, selectedId, StringComparison.Ordinal))
                selectedIndex = index;
        }

        if (selectedIndex < 0)
            throw new ArgumentException("The selected page must exist in the primary navigation.", nameof(selectedId));

        var origin = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var x = origin.X + MathF.Max(0f, (availableWidth - buttonSize) * 0.5f);
        var top = origin.Y + (TopPadding * scale);
        var stride = buttonSize + (ButtonGap * scale);
        var indicatorIndex = motion.Approach("SentinelModern2.PrimaryIndicator", selectedIndex, 16f);
        var indicatorMinimum = new Vector2(x, top + (stride * indicatorIndex));
        var indicatorMaximum = indicatorMinimum + new Vector2(buttonSize);
        var drawList = ImGui.GetWindowDrawList();

        SentinelModernPaint.GlassCard(
            drawList,
            indicatorMinimum,
            indicatorMaximum,
            11f * scale,
            SentinelModernPalette.Accent,
            0.45f);
        drawList.AddRectFilled(
            new Vector2(origin.X, indicatorMinimum.Y + (buttonSize * 0.25f)),
            new Vector2(origin.X + (3f * scale), indicatorMinimum.Y + (buttonSize * 0.75f)),
            ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Accent),
            2f * scale);

        string? clickedId = null;
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var minimum = new Vector2(x, top + (stride * index));
            var maximum = minimum + new Vector2(buttonSize);
            ImGui.SetCursorScreenPos(minimum);
            ImGui.PushID(item.Id);
            bool clicked;
            try
            {
                PushTransparentButtonStyle(scale);
                try
                {
                    clicked = ImGui.Button("##PrimaryNavigation", new Vector2(buttonSize));
                }
                finally
                {
                    PopTransparentButtonStyle();
                }
            }
            finally
            {
                ImGui.PopID();
            }

            var hovered = ImGui.IsItemHovered();
            var hover = motion.Hover(
                HashCode.Combine(StringComparer.Ordinal.GetHashCode(item.Id), 0x534D3250),
                hovered && item.Enabled);
            if (index != selectedIndex && hover > 0.001f)
            {
                drawList.AddRectFilled(
                    minimum,
                    maximum,
                    ImGui.ColorConvertFloat4ToU32(SentinelModernPaint.WithAlpha(
                        SentinelModernPalette.SurfaceHover,
                        0.62f * hover)),
                    11f * scale);
            }

            var iconColour = !item.Enabled
                ? SentinelModernPalette.Subtle
                : index == selectedIndex
                    ? SentinelModernPalette.Text
                    : Vector4.Lerp(SentinelModernPalette.Muted, SentinelModernPalette.Text, hover);
            if (item.DrawIcon is not null)
            {
                item.DrawIcon(new SentinelModernNavIconDrawContext(
                    drawList,
                    minimum,
                    maximum,
                    iconColour,
                    scale));
            }
            else
            {
                var iconSize = ImGui.CalcTextSize(item.Icon!);
                drawList.AddText(
                    minimum + ((new Vector2(buttonSize) - iconSize) * 0.5f),
                    ImGui.ColorConvertFloat4ToU32(iconColour),
                    item.Icon!);
            }

            if (item.Badge is { } badge)
            {
                var pulse = badge.Pulse && !motion.ReducedMotion
                    ? 0.68f + (motion.Pulse() * 0.32f)
                    : 1f;
                SentinelModernPaint.StatusDot(
                    drawList,
                    new Vector2(maximum.X - (7f * scale), minimum.Y + (7f * scale)),
                    3f * scale,
                    SentinelModernPaint.WithAlpha(badge.Colour, badge.Colour.W * pulse),
                    0.16f * pulse);
            }

            if (hovered)
                ShowTooltip(item.Label);
            if (clicked && item.Enabled)
                clickedId = item.Id;
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(availableWidth, (TopPadding * scale) + (stride * items.Count)));
        return clickedId;
    }

    private static void PushTransparentButtonStyle(float scale)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 11f * scale);
    }

    private static void PopTransparentButtonStyle()
    {
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(3);
    }

    private static void Validate(SentinelModernNavItem item)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(item.Label);
        if (item.DrawIcon is null && string.IsNullOrWhiteSpace(item.Icon))
            throw new ArgumentException("Provide either an icon glyph or DrawIcon callback.", nameof(item));
    }

    private static void ShowTooltip(string text)
    {
        ImGui.BeginTooltip();
        try
        {
            ImGui.TextUnformatted(text);
        }
        finally
        {
            ImGui.EndTooltip();
        }
    }

    private static void ValidatePositive(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

/// <summary>
/// Optional second-level text navigation for category-heavy pages.
/// </summary>
public static class SentinelModernSecondaryNavigation
{
    public static void GroupLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ImGui.TextColored(SentinelModernPalette.Subtle, label.ToUpperInvariant());
    }

    public static bool Item(
        string id,
        string icon,
        string label,
        bool selected,
        SentinelModernMotion motion,
        float scale = 1f)
        => DrawItem(id, icon, label, selected, motion, scale);

    /// <summary>
    /// Draws a clean text-only secondary category row. Primary destinations belong in the icon
    /// rail; secondary categories do not require decorative letter prefixes.
    /// </summary>
    public static bool Item(
        string id,
        string label,
        bool selected,
        SentinelModernMotion motion,
        float scale = 1f)
        => DrawItem(id, null, label, selected, motion, scale);

    private static bool DrawItem(
        string id,
        string? icon,
        string label,
        bool selected,
        SentinelModernMotion motion,
        float scale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(motion);
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));

        var height = 34f * scale;
        var width = ImGui.GetContentRegionAvail().X;
        var minimum = ImGui.GetCursorScreenPos();
        var maximum = minimum + new Vector2(width, height);
        ImGui.PushID(id);
        bool clicked;
        try
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 7f * scale);
            try
            {
                clicked = ImGui.Button("##SecondaryNavigation", new Vector2(width, height));
            }
            finally
            {
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);
            }
        }
        finally
        {
            ImGui.PopID();
        }

        var hover = motion.Hover(
            HashCode.Combine(StringComparer.Ordinal.GetHashCode(id), 0x534D3253),
            ImGui.IsItemHovered());
        var drawList = ImGui.GetWindowDrawList();
        if (selected || hover > 0.001f)
        {
            var fill = selected
                ? SentinelModernPalette.NavigationSelected
                : SentinelModernPaint.WithAlpha(SentinelModernPalette.SurfaceHover, 0.58f * hover);
            drawList.AddRectFilled(minimum, maximum, ImGui.ColorConvertFloat4ToU32(fill), 7f * scale);
        }

        if (selected)
        {
            drawList.AddRectFilled(
                minimum,
                new Vector2(minimum.X + (3f * scale), maximum.Y),
                ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Accent),
                2f * scale);
        }

        var labelSize = ImGui.CalcTextSize(label);
        var colour = selected ? SentinelModernPalette.Text : SentinelModernPalette.Muted;
        var labelX = minimum.X + (12f * scale);
        if (!string.IsNullOrWhiteSpace(icon))
        {
            var iconSize = ImGui.CalcTextSize(icon);
            var iconPosition = new Vector2(labelX, minimum.Y + ((height - iconSize.Y) * 0.5f));
            drawList.AddText(iconPosition, ImGui.ColorConvertFloat4ToU32(colour), icon);
            labelX += iconSize.X + (9f * scale);
        }

        drawList.AddText(
            new Vector2(labelX, minimum.Y + ((height - labelSize.Y) * 0.5f)),
            ImGui.ColorConvertFloat4ToU32(colour),
            label);
        return clicked;
    }
}

public enum SentinelModernPillTone
{
    Neutral = 0,
    Ready = 1,
    Enabled = 2,
    Running = 3,
    Warning = 4,
    Error = 5,
    Accent = 6,
    Custom = 7,
}

public readonly record struct SentinelModernStatusPillOptions(
    string Text,
    SentinelModernPillTone Tone = SentinelModernPillTone.Neutral)
{
    public Vector4? CustomColour { get; init; }

    public bool Pulse { get; init; }

    public string? Tooltip { get; init; }
}

public static class SentinelModernStatusPill
{
    public static Vector2 Measure(SentinelModernStatusPillOptions options, float scale = 1f)
    {
        Validate(options, scale);
        var text = ImGui.CalcTextSize(options.Text);
        return new Vector2(text.X + (31f * scale), MathF.Max(text.Y + (8f * scale), 22f * scale));
    }

    public static Vector2 Draw(
        SentinelModernStatusPillOptions options,
        SentinelModernMotion motion,
        float scale = 1f)
    {
        Validate(options, scale);
        ArgumentNullException.ThrowIfNull(motion);
        var size = Measure(options, scale);
        var minimum = ImGui.GetCursorScreenPos();
        var maximum = minimum + size;
        var colour = ResolveColour(options);
        var pulse = options.Pulse && !motion.ReducedMotion
            ? 0.72f + (motion.Pulse() * 0.28f)
            : 1f;

        var drawList = ImGui.GetWindowDrawList();
        SentinelModernPaint.Pill(
            drawList,
            minimum,
            maximum,
            SentinelModernPaint.WithAlpha(colour, 0.12f + (0.04f * pulse)),
            SentinelModernPaint.WithAlpha(colour, 0.42f + (0.16f * pulse)));
        var centreY = minimum.Y + (size.Y * 0.5f);
        SentinelModernPaint.StatusDot(
            drawList,
            new Vector2(minimum.X + (11f * scale), centreY),
            3f * scale,
            SentinelModernPaint.WithAlpha(colour, colour.W * pulse),
            0.15f * pulse);
        var textSize = ImGui.CalcTextSize(options.Text);
        drawList.AddText(
            new Vector2(minimum.X + (20f * scale), centreY - (textSize.Y * 0.5f)),
            ImGui.ColorConvertFloat4ToU32(colour),
            options.Text);
        ImGui.Dummy(size);
        if (!string.IsNullOrWhiteSpace(options.Tooltip) && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            try
            {
                ImGui.TextUnformatted(options.Tooltip);
            }
            finally
            {
                ImGui.EndTooltip();
            }
        }

        return size;
    }

    public static Vector4 ResolveColour(SentinelModernStatusPillOptions options)
    {
        if (!Enum.IsDefined(options.Tone))
            throw new ArgumentOutOfRangeException(nameof(options));
        if (options.Tone == SentinelModernPillTone.Custom && options.CustomColour is null)
            throw new ArgumentException("A custom status pill requires CustomColour.", nameof(options));
        if (options.CustomColour is { } supplied
            && (!float.IsFinite(supplied.X) || !float.IsFinite(supplied.Y)
                || !float.IsFinite(supplied.Z) || !float.IsFinite(supplied.W)))
            throw new ArgumentOutOfRangeException(nameof(options), "CustomColour must be finite.");

        return options.Tone switch
        {
            SentinelModernPillTone.Ready => SentinelModernPalette.Teal,
            SentinelModernPillTone.Enabled => SentinelModernPalette.Teal,
            SentinelModernPillTone.Running => SentinelModernPalette.AccentStrong,
            SentinelModernPillTone.Warning => SentinelModernPalette.Rose,
            SentinelModernPillTone.Error => SentinelModernPalette.Error,
            SentinelModernPillTone.Accent => SentinelModernPalette.Violet,
            SentinelModernPillTone.Custom => options.CustomColour!.Value,
            _ => SentinelModernPalette.Muted,
        };
    }

    private static void Validate(SentinelModernStatusPillOptions options, float scale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Text);
        if (!Enum.IsDefined(options.Tone))
            throw new ArgumentOutOfRangeException(nameof(options));
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (options.Tone == SentinelModernPillTone.Custom && options.CustomColour is null)
            throw new ArgumentException("A custom status pill requires CustomColour.", nameof(options));
    }
}

public readonly record struct SentinelModernGlassCardOptions
{
    public SentinelModernGlassCardOptions()
    {
    }

    public Vector2 Size { get; init; }

    public Vector4? Accent { get; init; }

    public float AccentStrength { get; init; }

    public float Rounding { get; init; } = 10f;

    public Vector2 Padding { get; init; } = new(14f, 12f);

    public bool Elevated { get; init; }
}

public static class SentinelModernGlassCard
{
    public static SentinelModernGlassCardScope Begin(
        string id,
        SentinelModernGlassCardOptions options = default,
        float scale = 1f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(options.AccentStrength) || options.AccentStrength < 0f || options.AccentStrength > 1f)
            throw new ArgumentOutOfRangeException(nameof(options));

        var rounding = (options.Rounding <= 0f ? 10f : options.Rounding) * scale;
        var padding = (options.Padding == Vector2.Zero ? new Vector2(14f, 12f) : options.Padding) * scale;
        var size = options.Size;
        if (size.X <= 0f)
            size.X = ImGui.GetContentRegionAvail().X;
        else
            size.X *= scale;
        if (size.Y > 0f)
            size.Y *= scale;

        var minimum = ImGui.GetCursorScreenPos();
        var drawHeight = size.Y > 0f ? size.Y : ImGui.GetContentRegionAvail().Y;
        var maximum = minimum + new Vector2(size.X, MathF.Max(0f, drawHeight));
        SentinelModernPaint.GlassCard(
            ImGui.GetWindowDrawList(),
            minimum,
            maximum,
            rounding,
            options.Accent,
            options.AccentStrength,
            options.Elevated);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, rounding);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);
        try
        {
            var visible = ImGui.BeginChild(
                id,
                size,
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysUseWindowPadding);
            return new SentinelModernGlassCardScope(visible);
        }
        catch
        {
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor();
            throw;
        }
    }
}

public ref struct SentinelModernGlassCardScope
{
    private bool disposed;

    internal SentinelModernGlassCardScope(bool isVisible)
    {
        IsVisible = isVisible;
        disposed = false;
    }

    public bool IsVisible { get; }

    public void Dispose()
    {
        if (disposed)
            return;

        ImGui.EndChild();
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
        disposed = true;
    }
}

public static class SentinelModernSettingsRow
{
    public static void Draw(
        string id,
        string label,
        string? description,
        Action drawControl,
        float controlWidth = 180f,
        float scale = 1f)
        => Draw(
            id,
            label,
            description,
            drawControl,
            SentinelModernSettingsRowLayoutOptions.Default with
            {
                PreferredControlWidth = controlWidth,
                MinimumControlWidth = MathF.Min(
                    controlWidth,
                    SentinelModernSettingsRowLayoutOptions.Default.MinimumControlWidth),
            },
            scale);

    public static void Draw(
        string id,
        string label,
        string? description,
        Action drawControl,
        SentinelModernSettingsRowLayoutOptions layoutOptions,
        float scale = 1f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(drawControl);
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (layoutOptions == default)
            layoutOptions = SentinelModernSettingsRowLayoutOptions.Default;

        var width = ImGui.GetContentRegionAvail().X;
        var columns = SentinelModernSettingsRowLayout.ResolveColumns(width, scale, layoutOptions);
        var labelSize = ImGui.CalcTextSize(label);
        var hasDescription = !string.IsNullOrWhiteSpace(description);
        var descriptionSize = hasDescription
            ? ImGui.CalcTextSize(description, false, columns.TextWidth)
            : Vector2.Zero;
        var layout = SentinelModernSettingsRowLayout.Resolve(
            width,
            labelSize.Y,
            descriptionSize.Y,
            ImGui.GetFrameHeight(),
            hasDescription,
            scale,
            layoutOptions);
        var minimum = ImGui.GetCursorScreenPos();
        var maximum = minimum + layout.Size;
        SentinelModernPaint.GlassCard(
            ImGui.GetWindowDrawList(),
            minimum,
            maximum,
            9f * scale,
            SentinelModernPalette.Accent,
            0.04f);

        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        try
        {
            var visible = ImGui.BeginChild(
                id,
                layout.Size,
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            try
            {
                if (!visible)
                    return;

                var drawList = ImGui.GetWindowDrawList();
                var textPosition = minimum + layout.TextOffset;
                drawList.AddText(
                    textPosition,
                    ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Text),
                    label);
                if (hasDescription)
                {
                    var descriptionY = textPosition.Y
                        + labelSize.Y
                        + (layoutOptions.DescriptionGap * scale);
                    drawList.AddText(
                        ImGui.GetFont(),
                        ImGui.GetFontSize(),
                        new Vector2(textPosition.X, descriptionY),
                        ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Muted),
                        description!,
                        layout.TextWidth);
                }

                ImGui.SetCursorScreenPos(minimum + layout.ControlOffset);
                ImGui.SetNextItemWidth(layout.ControlWidth);
                drawControl();
            }
            finally
            {
                ImGui.EndChild();
            }
        }
        finally
        {
            ImGui.PopStyleColor();
        }
    }
}

/// <summary>
/// Controller/keyboard-aware Modern 2 switch. The standard ImGui button remains the interaction
/// target while Core owns the visual treatment and decorative knob motion.
/// </summary>
public static class SentinelModernSwitch
{
    public static bool Draw(
        string id,
        string label,
        ref bool value,
        SentinelModernMotion motion,
        float scale = 1f,
        float width = 0f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(motion);
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(width) || width < 0f)
            throw new ArgumentOutOfRangeException(nameof(width));

        var rowWidth = width > 0f ? width * scale : ImGui.GetContentRegionAvail().X;
        var height = 32f * scale;
        var minimum = ImGui.GetCursorScreenPos();
        var maximum = minimum + new Vector2(rowWidth, height);
        ImGui.PushID(id);
        bool clicked;
        try
        {
            ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f * scale);
            try
            {
                clicked = ImGui.Button("##Switch", new Vector2(rowWidth, height));
            }
            finally
            {
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);
            }
        }
        finally
        {
            ImGui.PopID();
        }

        if (clicked)
            value = !value;

        var hovered = ImGui.IsItemHovered();
        var hover = motion.Hover(
            HashCode.Combine(StringComparer.Ordinal.GetHashCode(id), 0x534D3257),
            hovered);
        var drawList = ImGui.GetWindowDrawList();
        if (hover > 0.001f)
        {
            drawList.AddRectFilled(
                minimum,
                maximum,
                ImGui.ColorConvertFloat4ToU32(SentinelModernPaint.WithAlpha(
                    SentinelModernPalette.SurfaceHover,
                    0.42f * hover)),
                8f * scale);
        }

        var labelSize = ImGui.CalcTextSize(label);
        drawList.AddText(
            new Vector2(minimum.X + (6f * scale), minimum.Y + ((height - labelSize.Y) * 0.5f)),
            ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Text),
            label);

        var trackSize = new Vector2(42f, 22f) * scale;
        var trackMinimum = new Vector2(
            maximum.X - trackSize.X - (5f * scale),
            minimum.Y + ((height - trackSize.Y) * 0.5f));
        var trackMaximum = trackMinimum + trackSize;
        var valueProgress = motion.Approach(
            HashCode.Combine(StringComparer.Ordinal.GetHashCode(id), 0x534D3254),
            value ? 1f : 0f,
            22f);
        var trackColour = Vector4.Lerp(
            SentinelModernPalette.ToggleOff,
            SentinelModernPalette.Accent,
            valueProgress);
        drawList.AddRectFilled(
            trackMinimum,
            trackMaximum,
            ImGui.ColorConvertFloat4ToU32(trackColour),
            trackSize.Y * 0.5f);

        var knobRadius = 8f * scale;
        var knobX = (trackMinimum.X + (11f * scale))
            + ((trackSize.X - (22f * scale)) * valueProgress);
        drawList.AddCircleFilled(
            new Vector2(knobX, trackMinimum.Y + (trackSize.Y * 0.5f)),
            knobRadius,
            ImGui.ColorConvertFloat4ToU32(Vector4.Lerp(
                SentinelModernPalette.ToggleKnobOff,
                SentinelModernPalette.ToggleKnobOn,
                valueProgress)),
            24);
        return clicked;
    }
}

public static class SentinelModernActionDock
{
    public static bool PrimaryButton(
        string id,
        string label,
        Vector2 size,
        float scale = 1f)
        => Button(id, label, size, SentinelModernPalette.Accent, scale);

    public static bool DangerButton(
        string id,
        string label,
        Vector2 size,
        float scale = 1f)
        => Button(id, label, size, SentinelModernPalette.Rose, scale);

    public static void Status(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ImGui.TextColored(SentinelModernPalette.Muted, text);
    }

    private static bool Button(string id, string label, Vector2 size, Vector4 accent, float scale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (size.X <= 0f)
            size.X = ImGui.GetContentRegionAvail().X;
        if (size.Y <= 0f)
            size.Y = 38f * scale;

        ImGui.PushID(id);
        try
        {
            ImGui.PushStyleColor(ImGuiCol.Button, SentinelModernPaint.WithAlpha(accent, 0.82f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SentinelModernPaint.Lighten(accent, 0.12f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, SentinelModernPaint.WithAlpha(accent, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, SentinelModernPalette.Text);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 9f * scale);
            try
            {
                return ImGui.Button(label, size);
            }
            finally
            {
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(4);
            }
        }
        finally
        {
            ImGui.PopID();
        }
    }
}

public static class SentinelModernPageTransition
{
    public static SentinelModernPageTransitionScope Begin(
        float progress,
        float scale = 1f,
        float slideDistance = 10f)
    {
        if (!float.IsFinite(progress) || progress < 0f || progress > 1f)
            throw new ArgumentOutOfRangeException(nameof(progress));
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
        if (!float.IsFinite(slideDistance) || slideDistance < 0f)
            throw new ArgumentOutOfRangeException(nameof(slideDistance));

        if (progress < 1f)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ((1f - progress) * slideDistance * scale));
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, MathF.Max(0.001f, ImGui.GetStyle().Alpha * progress));
        return new SentinelModernPageTransitionScope();
    }
}

public ref struct SentinelModernPageTransitionScope
{
    private bool disposed;

    public void Dispose()
    {
        if (disposed)
            return;
        ImGui.PopStyleVar();
        disposed = true;
    }
}
