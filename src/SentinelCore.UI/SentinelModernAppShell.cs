using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

public readonly record struct SentinelModernIconDrawContext(
    ImDrawListPtr DrawList,
    Vector2 Minimum,
    Vector2 Maximum,
    float Scale);

public enum SentinelModernAppSurfaceStyle
{
    Unified = 0,
    Segmented = 1,
}

public readonly record struct SentinelModernAppShellOptions(
    string Id,
    string Title,
    string SelectedPageId)
{
    public string? PluginGlyph { get; init; }

    public string? ContextLabel { get; init; }

    public SentinelModernStatusPillOptions? Status { get; init; }

    public Action<SentinelModernIconDrawContext>? DrawPluginIcon { get; init; }

    public Action? RequestCollapse { get; init; }

    public Action? RequestClose { get; init; }

    public string CollapseTooltip { get; init; } = "Minimize";

    public string CloseTooltip { get; init; } = "Close";

    public float Scale { get; init; } = 1f;

    public float DeltaTime { get; init; } = 1f / 60f;

    public bool ReducedMotion { get; init; }

    /// <summary>
    /// Enables dragging the consumer-owned top-level ImGui window from unused header space.
    /// Consumers should combine this with <see cref="SentinelModernWindowChrome.UseCustomHeader"/>.
    /// </summary>
    public bool EnableWindowDragging { get; init; }

    public bool DrawAmbientBackground { get; init; } = true;

    public float AmbientIntensity { get; init; } = 0.9f;

    public SentinelModernAppSurfaceStyle SurfaceStyle { get; init; }
        = SentinelModernAppSurfaceStyle.Unified;

    public SentinelModernAppLayoutOptions Layout { get; init; } = SentinelModernAppLayoutOptions.Default;
}

/// <summary>
/// Canonical Sentinel Modern 2 application shell. It draws inside an already-open Dalamud window
/// and never calls the top-level ImGui Begin/End pair.
/// </summary>
public static class SentinelModernAppShell
{
    public static void Draw(
        SentinelModernAppShellOptions options,
        SentinelModernAppShellState state,
        IReadOnlyList<SentinelModernNavItem> primaryNavigation,
        Action<string> selectPage,
        Action drawPage,
        Action? drawSecondaryNavigation = null,
        Action? drawActionDock = null)
    {
        Validate(options);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(primaryNavigation);
        ArgumentNullException.ThrowIfNull(selectPage);
        ArgumentNullException.ThrowIfNull(drawPage);
        if (primaryNavigation.Count == 0)
            throw new ArgumentException("At least one primary navigation item is required.", nameof(primaryNavigation));

        state.BeginFrame(options.SelectedPageId, options.DeltaTime, options.ReducedMotion);
        var origin = ImGui.GetCursorScreenPos();
        var available = ImGui.GetContentRegionAvail();
        var layout = SentinelModernAppLayout.Resolve(
            available,
            options.Scale,
            drawSecondaryNavigation is not null,
            drawActionDock is not null,
            options.Layout);
        var maximum = origin + available;

        DrawApplicationSurface(origin, maximum, options.Scale, options.SurfaceStyle);

        if (options.DrawAmbientBackground)
        {
            SentinelModernAmbient.DrawAnimated(
                state.Motion,
                origin,
                maximum,
                options.Scale,
                options.AmbientIntensity,
                options.ReducedMotion);
        }

        ImGui.PushID(options.Id);
        try
        {
            DrawHeader(options, state, origin, new Vector2(available.X, layout.HeaderHeight));

            var bodyTop = origin.Y + layout.HeaderHeight;
            var railMinimum = new Vector2(origin.X, bodyTop);
            var railMaximum = railMinimum + new Vector2(layout.RailWidth, layout.BodyHeight);
            DrawRail(
                options,
                state,
                primaryNavigation,
                selectPage,
                railMinimum,
                railMaximum,
                layout.RailButtonSize);

            var contentX = railMaximum.X;
            if (drawSecondaryNavigation is not null)
            {
                var sidebarMinimum = new Vector2(contentX, bodyTop);
                var sidebarMaximum = sidebarMinimum + new Vector2(
                    layout.SecondarySidebarWidth,
                    layout.BodyHeight);
                DrawSecondarySidebar(
                    drawSecondaryNavigation,
                    sidebarMinimum,
                    sidebarMaximum,
                    options.Scale,
                    options.SurfaceStyle);
                contentX = sidebarMaximum.X;
            }

            DrawPage(
                drawPage,
                state,
                new Vector2(contentX, bodyTop),
                new Vector2(layout.ContentWidth, layout.ContentHeight),
                layout.ContentPadding,
                options.Scale);

            if (drawActionDock is not null)
            {
                DrawActionDock(
                    drawActionDock,
                    new Vector2(origin.X, bodyTop + layout.BodyHeight),
                    new Vector2(available.X, layout.ActionDockHeight),
                    options.Scale,
                    options.SurfaceStyle);
            }
        }
        finally
        {
            ImGui.PopID();
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(available);
    }

    private static void DrawApplicationSurface(
        Vector2 minimum,
        Vector2 maximum,
        float scale,
        SentinelModernAppSurfaceStyle surfaceStyle)
    {
        var top = surfaceStyle == SentinelModernAppSurfaceStyle.Unified
            ? SentinelModernPaint.Lighten(SentinelModernPalette.Canvas, 0.025f)
            : SentinelModernPalette.Canvas;
        SentinelModernPaint.GradientSurface(
            ImGui.GetWindowDrawList(),
            minimum,
            maximum,
            top,
            SentinelModernPalette.Canvas,
            9f * scale);
    }

    private static void DrawHeader(
        SentinelModernAppShellOptions options,
        SentinelModernAppShellState state,
        Vector2 minimum,
        Vector2 size)
    {
        var maximum = minimum + size;
        var scale = options.Scale;
        var drawList = ImGui.GetWindowDrawList();
        var unified = options.SurfaceStyle == SentinelModernAppSurfaceStyle.Unified;
        SentinelModernPaint.GradientSurface(
            drawList,
            minimum,
            maximum,
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.SurfaceGlassTop,
                unified ? 0.34f : 0.84f),
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.SurfaceGlassBottom,
                unified ? 0.42f : 0.88f),
            9f * scale);
        SentinelModernPaint.Hairline(
            drawList,
            new Vector2(minimum.X, maximum.Y - 0.5f),
            new Vector2(maximum.X, maximum.Y - 0.5f),
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.Border,
                unified ? 0.38f : 0.62f));

        ImGui.SetCursorScreenPos(minimum);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        try
        {
            var dragging = false;
            var visible = ImGui.BeginChild(
                "##Modern2Header",
                size,
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            try
            {
                if (!visible)
                    return;

                const float logicalPadding = 14f;
                const float logicalIcon = 30f;
                const float logicalButton = 30f;
                const float logicalGap = 6f;
                var padding = logicalPadding * scale;
                var iconSize = logicalIcon * scale;
                var buttonSize = logicalButton * scale;
                var buttonGap = logicalGap * scale;
                var centreY = minimum.Y + (size.Y * 0.5f);
                var iconMinimum = new Vector2(minimum.X + padding, centreY - (iconSize * 0.5f));
                var iconMaximum = iconMinimum + new Vector2(iconSize);

                SentinelModernPaint.GlassCard(
                    drawList,
                    iconMinimum,
                    iconMaximum,
                    8f * scale,
                    SentinelModernPalette.Violet,
                    0.34f);
                if (options.DrawPluginIcon is not null)
                {
                    options.DrawPluginIcon(new SentinelModernIconDrawContext(
                        drawList,
                        iconMinimum,
                        iconMaximum,
                        scale));
                }
                else if (!string.IsNullOrWhiteSpace(options.PluginGlyph))
                {
                    var glyphSize = ImGui.CalcTextSize(options.PluginGlyph);
                    drawList.AddText(
                        iconMinimum + ((new Vector2(iconSize) - glyphSize) * 0.5f),
                        ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Text),
                        options.PluginGlyph);
                }

                var actionCount = (options.RequestCollapse is null ? 0 : 1)
                    + (options.RequestClose is null ? 0 : 1);
                var actionsWidth = actionCount == 0
                    ? 0f
                    : (actionCount * buttonSize) + ((actionCount - 1) * buttonGap);
                var actionsX = maximum.X - padding - actionsWidth;
                var titleX = iconMaximum.X + (11f * scale);
                var titleSize = ImGui.CalcTextSize(options.Title);
                drawList.AddText(
                    new Vector2(titleX, centreY - (titleSize.Y * 0.5f)),
                    ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Text),
                    options.Title);
                var cursorX = titleX + titleSize.X + (12f * scale);
                var rightLimit = actionCount == 0 ? maximum.X - padding : actionsX - (10f * scale);

                if (options.Status is { } status)
                {
                    var pillSize = SentinelModernStatusPill.Measure(status, scale);
                    if (cursorX + pillSize.X <= rightLimit)
                    {
                        ImGui.SetCursorScreenPos(new Vector2(cursorX, centreY - (pillSize.Y * 0.5f)));
                        SentinelModernStatusPill.Draw(status, state.Motion, scale);
                        cursorX += pillSize.X + (12f * scale);
                    }
                }

                if (!string.IsNullOrWhiteSpace(options.ContextLabel))
                {
                    var contextSize = ImGui.CalcTextSize(options.ContextLabel);
                    if (cursorX + contextSize.X <= rightLimit)
                    {
                        drawList.AddText(
                            new Vector2(cursorX, centreY - (contextSize.Y * 0.5f)),
                            ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Muted),
                            options.ContextLabel);
                        cursorX += contextSize.X + (12f * scale);
                    }
                }

                if (options.EnableWindowDragging)
                {
                    var dragMinimumX = MathF.Min(cursorX, rightLimit);
                    var dragWidth = MathF.Max(0f, rightLimit - dragMinimumX);
                    if (dragWidth >= 12f * scale)
                    {
                        ImGui.SetCursorScreenPos(new Vector2(dragMinimumX, minimum.Y));
                        ImGui.InvisibleButton(
                            "##Modern2WindowDrag",
                            new Vector2(dragWidth, size.Y));
                        dragging = ImGui.IsItemActive()
                            && ImGui.IsMouseDragging(ImGuiMouseButton.Left);
                    }
                }

                var actionX = actionsX;
                if (options.RequestCollapse is not null)
                {
                    if (DrawHeaderButton(
                            "##Modern2Collapse",
                            "−",
                            options.CollapseTooltip,
                            new Vector2(actionX, centreY - (buttonSize * 0.5f)),
                            buttonSize,
                            scale))
                        options.RequestCollapse();
                    actionX += buttonSize + buttonGap;
                }

                if (options.RequestClose is not null
                    && DrawHeaderButton(
                        "##Modern2Close",
                        "×",
                        options.CloseTooltip,
                        new Vector2(actionX, centreY - (buttonSize * 0.5f)),
                        buttonSize,
                        scale))
                    options.RequestClose();
            }
            finally
            {
                ImGui.EndChild();
            }

            if (dragging)
            {
                var mouseDelta = ImGui.GetIO().MouseDelta;
                if (mouseDelta != Vector2.Zero)
                    ImGui.SetWindowPos(ImGui.GetWindowPos() + mouseDelta);
            }
        }
        finally
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }
    }

    private static bool DrawHeaderButton(
        string id,
        string glyph,
        string tooltip,
        Vector2 minimum,
        float size,
        float scale)
    {
        ImGui.SetCursorScreenPos(minimum);
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, SentinelModernPalette.SurfaceHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, SentinelModernPalette.SurfaceActive);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8f * scale);
        bool clicked;
        try
        {
            ImGui.PushID(id);
            try
            {
                clicked = ImGui.Button(glyph, new Vector2(size));
            }
            finally
            {
                ImGui.PopID();
            }
        }
        finally
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
        }

        if (ImGui.IsItemHovered() && !string.IsNullOrWhiteSpace(tooltip))
        {
            ImGui.BeginTooltip();
            try
            {
                ImGui.TextUnformatted(tooltip);
            }
            finally
            {
                ImGui.EndTooltip();
            }
        }

        return clicked;
    }

    private static void DrawRail(
        SentinelModernAppShellOptions options,
        SentinelModernAppShellState state,
        IReadOnlyList<SentinelModernNavItem> navigation,
        Action<string> selectPage,
        Vector2 minimum,
        Vector2 maximum,
        float buttonSize)
    {
        var size = maximum - minimum;
        var unified = options.SurfaceStyle == SentinelModernAppSurfaceStyle.Unified;
        SentinelModernPaint.GradientSurface(
            ImGui.GetWindowDrawList(),
            minimum,
            maximum,
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.SurfaceRail,
                unified ? 0.34f : 0.94f),
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.Canvas,
                unified ? 0.18f : 0.92f),
            0f);
        SentinelModernPaint.Hairline(
            ImGui.GetWindowDrawList(),
            new Vector2(maximum.X - 0.5f, minimum.Y),
            new Vector2(maximum.X - 0.5f, maximum.Y),
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.Border,
                unified ? 0.32f : 0.55f));

        ImGui.SetCursorScreenPos(minimum);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        try
        {
            var visible = ImGui.BeginChild(
                "##Modern2Rail",
                size,
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            try
            {
                if (!visible)
                    return;
                var clicked = SentinelModernIconRail.Draw(
                    navigation,
                    options.SelectedPageId,
                    state.Motion,
                    buttonSize,
                    options.Scale);
                if (clicked is not null)
                    selectPage(clicked);
            }
            finally
            {
                ImGui.EndChild();
            }
        }
        finally
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }
    }

    private static void DrawSecondarySidebar(
        Action draw,
        Vector2 minimum,
        Vector2 maximum,
        float scale,
        SentinelModernAppSurfaceStyle surfaceStyle)
    {
        if (surfaceStyle == SentinelModernAppSurfaceStyle.Segmented)
        {
            SentinelModernPaint.GlassCard(
                ImGui.GetWindowDrawList(),
                minimum,
                maximum,
                0f,
                SentinelModernPalette.Accent,
                0.03f);
        }
        else
        {
            ImGui.GetWindowDrawList().AddRectFilled(
                minimum,
                maximum,
                ImGui.ColorConvertFloat4ToU32(SentinelModernPaint.WithAlpha(
                    SentinelModernPalette.SurfaceGlassBottom,
                    0.14f)));
        }

        SentinelModernPaint.Hairline(
            ImGui.GetWindowDrawList(),
            new Vector2(maximum.X - 0.5f, minimum.Y),
            new Vector2(maximum.X - 0.5f, maximum.Y),
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.Border,
                surfaceStyle == SentinelModernAppSurfaceStyle.Unified ? 0.32f : 0.55f));
        DrawChild(
            "##Modern2Secondary",
            minimum,
            maximum - minimum,
            new Vector2(12f, 14f) * scale,
            draw);
    }

    private static void DrawPage(
        Action draw,
        SentinelModernAppShellState state,
        Vector2 minimum,
        Vector2 size,
        float padding,
        float scale)
    {
        ImGui.SetCursorScreenPos(minimum);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(padding));
        try
        {
            var visible = ImGui.BeginChild(
                "##Modern2Page",
                size,
                false,
                ImGuiWindowFlags.AlwaysUseWindowPadding);
            try
            {
                if (!visible)
                    return;
                using var transition = SentinelModernPageTransition.Begin(
                    state.PageRevealProgress,
                    scale,
                    10f);
                draw();
            }
            finally
            {
                ImGui.EndChild();
            }
        }
        finally
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }
    }

    private static void DrawActionDock(
        Action draw,
        Vector2 minimum,
        Vector2 size,
        float scale,
        SentinelModernAppSurfaceStyle surfaceStyle)
    {
        var maximum = minimum + size;
        SentinelModernPaint.GradientSurface(
            ImGui.GetWindowDrawList(),
            minimum,
            maximum,
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.SurfaceDock,
                surfaceStyle == SentinelModernAppSurfaceStyle.Unified ? 0.58f : 0.98f),
            SentinelModernPaint.WithAlpha(
                SentinelModernPalette.Canvas,
                surfaceStyle == SentinelModernAppSurfaceStyle.Unified ? 0.52f : 0.98f),
            9f * scale);
        SentinelModernPaint.Hairline(
            ImGui.GetWindowDrawList(),
            minimum,
            new Vector2(maximum.X, minimum.Y),
            SentinelModernPaint.WithAlpha(SentinelModernPalette.BorderBright, 0.42f));
        DrawChild(
            "##Modern2Dock",
            minimum,
            size,
            new Vector2(16f, 14f) * scale,
            draw);
    }

    private static void DrawChild(
        string id,
        Vector2 minimum,
        Vector2 size,
        Vector2 padding,
        Action draw)
    {
        ImGui.SetCursorScreenPos(minimum);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);
        try
        {
            var visible = ImGui.BeginChild(
                id,
                size,
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.AlwaysUseWindowPadding);
            try
            {
                if (visible)
                    draw();
            }
            finally
            {
                ImGui.EndChild();
            }
        }
        finally
        {
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
        }
    }

    private static void Validate(SentinelModernAppShellOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.SelectedPageId);
        if (!float.IsFinite(options.Scale) || options.Scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(options), "Scale must be finite and positive.");
        if (!float.IsFinite(options.DeltaTime) || options.DeltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(options), "Delta time must be finite and non-negative.");
        if (!float.IsFinite(options.AmbientIntensity)
            || options.AmbientIntensity < 0f
            || options.AmbientIntensity > 1f)
            throw new ArgumentOutOfRangeException(nameof(options), "Ambient intensity must be between zero and one.");
        if (options.DrawPluginIcon is null && string.IsNullOrWhiteSpace(options.PluginGlyph))
            throw new ArgumentException("Provide either PluginGlyph or DrawPluginIcon.", nameof(options));
        if (options.Status is { } status)
            _ = SentinelModernStatusPill.ResolveColour(status);
        if (!Enum.IsDefined(options.SurfaceStyle))
            throw new ArgumentOutOfRangeException(nameof(options), "Surface style is not defined.");
    }
}
