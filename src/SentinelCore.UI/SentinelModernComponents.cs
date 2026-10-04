using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

public static class SentinelModernNavigation
{
    public static void GroupLabel(string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ImGui.TextColored(SentinelModernPalette.Subtle, label);
    }

    public static bool Item(string id, string label, bool selected, float scale = 1f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ValidateScale(scale);

        ImGui.PushID(id);
        try
        {
            if (selected)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, SentinelModernPalette.NavigationSelected);
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, SentinelModernPalette.NavigationSelectedHover);
                ImGui.PushStyleColor(ImGuiCol.Text, SentinelModernPalette.Text);
            }

            bool clicked;
            try
            {
                clicked = ImGui.Selectable(
                    label,
                    selected,
                    ImGuiSelectableFlags.None,
                    new Vector2(0f, 31f * scale));
            }
            finally
            {
                if (selected)
                    ImGui.PopStyleColor(3);
            }

            if (selected)
            {
                var minimum = ImGui.GetItemRectMin();
                var maximum = ImGui.GetItemRectMax();
                ImGui.GetWindowDrawList().AddRectFilled(
                    minimum,
                    new Vector2(minimum.X + (4f * scale), maximum.Y),
                    ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Accent),
                    3f * scale);
            }

            return clicked;
        }
        finally
        {
            ImGui.PopID();
        }
    }

    private static void ValidateScale(float scale)
    {
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));
    }
}

public static class SentinelModernControls
{
    public static bool Toggle(
        string id,
        string label,
        ref bool value,
        float scale = 1f,
        float minimumWidth = 180f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ValidatePositiveFinite(scale, nameof(scale));
        ValidatePositiveFinite(minimumWidth, nameof(minimumWidth));

        var availableWidth = ImGui.GetContentRegionAvail().X;
        var width = MathF.Max(minimumWidth * scale, availableWidth);
        var rowHeight = 30f * scale;
        var trackWidth = 40f * scale;
        var trackHeight = 20f * scale;
        var origin = ImGui.GetCursorScreenPos();

        ImGui.PushID(id);
        bool changed;
        try
        {
            ImGui.InvisibleButton("##Toggle", new Vector2(width, rowHeight));
            changed = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        }
        finally
        {
            ImGui.PopID();
        }

        if (changed)
            value = !value;

        var drawList = ImGui.GetWindowDrawList();
        if (ImGui.IsItemHovered())
        {
            drawList.AddRectFilled(
                origin,
                origin + new Vector2(width, rowHeight),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.14f, 0.25f, 0.58f)),
                7f * scale);
        }

        var textSize = ImGui.CalcTextSize(label);
        drawList.AddText(
            origin + new Vector2(4f * scale, (rowHeight - textSize.Y) * 0.5f),
            ImGui.ColorConvertFloat4ToU32(SentinelModernPalette.Text),
            label);

        var trackMin = origin + new Vector2(
            width - trackWidth - (4f * scale),
            (rowHeight - trackHeight) * 0.5f);
        var trackMax = trackMin + new Vector2(trackWidth, trackHeight);
        drawList.AddRectFilled(
            trackMin,
            trackMax,
            ImGui.ColorConvertFloat4ToU32(
                value ? SentinelModernPalette.Accent : SentinelModernPalette.ToggleOff),
            trackHeight * 0.5f);

        var knobCenter = new Vector2(
            value ? trackMax.X - (10f * scale) : trackMin.X + (10f * scale),
            trackMin.Y + (trackHeight * 0.5f));
        drawList.AddCircleFilled(
            knobCenter,
            7f * scale,
            ImGui.ColorConvertFloat4ToU32(
                value ? SentinelModernPalette.ToggleKnobOn : SentinelModernPalette.ToggleKnobOff),
            24);

        return changed;
    }

    public static bool CollapsingSection(
        string title,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ImGui.Spacing();
        ImGui.PushStyleColor(ImGuiCol.Header, SentinelModernPalette.SurfaceRaised);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, SentinelModernPalette.SurfaceHover);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(0.125f, 0.225f, 0.420f, 1f));
        bool open;
        try
        {
            open = ImGui.CollapsingHeader(title, flags);
        }
        finally
        {
            ImGui.PopStyleColor(3);
        }

        if (open)
            ImGui.Spacing();
        return open;
    }

    private static void ValidatePositiveFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value) || value <= 0f)
            throw new ArgumentOutOfRangeException(parameterName);
    }
}

public static class SentinelModernUi
{
    public static void PageHeading(string title, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);
        ImGui.TextColored(SentinelModernPalette.Text, title);
        ImGui.PushStyleColor(ImGuiCol.Text, SentinelModernPalette.Muted);
        try
        {
            ImGui.TextWrapped(description);
        }
        finally
        {
            ImGui.PopStyleColor();
        }
    }

    public static void SectionHeader(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ImGui.TextColored(SentinelModernPalette.AccentStrong, title);
        ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.20f, 0.34f, 0.58f, 0.82f));
        try
        {
            ImGui.Separator();
        }
        finally
        {
            ImGui.PopStyleColor();
        }

        ImGui.Spacing();
    }

    public static Vector2 StatusChip(
        string text,
        SentinelModernStatusTone tone = SentinelModernStatusTone.Neutral,
        float scale = 1f)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (!Enum.IsDefined(tone))
            throw new ArgumentOutOfRangeException(nameof(tone));
        if (!float.IsFinite(scale) || scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(scale));

        var textSize = ImGui.CalcTextSize(text);
        var padding = new Vector2(9f, 4f) * scale;
        var size = textSize + (padding * 2f);
        var minimum = ImGui.GetCursorScreenPos();
        var maximum = minimum + size;
        var toneColour = SentinelModernPalette.ForStatus(tone);
        var drawList = ImGui.GetWindowDrawList();

        drawList.AddRectFilled(
            minimum,
            maximum,
            ImGui.ColorConvertFloat4ToU32(WithAlpha(toneColour, 0.58f)),
            size.Y * 0.5f);
        drawList.AddRectFilled(
            minimum + new Vector2(1f, 1f) * scale,
            maximum - new Vector2(1f, 1f) * scale,
            ImGui.ColorConvertFloat4ToU32(WithAlpha(SentinelModernPalette.SurfaceRaised, 0.96f)),
            (size.Y * 0.5f) - scale);
        drawList.AddText(
            minimum + padding,
            ImGui.ColorConvertFloat4ToU32(toneColour),
            text);
        ImGui.Dummy(size);
        return size;
    }

    private static Vector4 WithAlpha(Vector4 colour, float alpha)
        => new(colour.X, colour.Y, colour.Z, alpha);
}

public static class SentinelModernCard
{
    public static SentinelModernCardScope Begin(
        string id,
        Vector2 size = default,
        bool border = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var visible = ImGui.BeginChild(id, size, border);
        return new SentinelModernCardScope(visible);
    }
}

public ref struct SentinelModernCardScope
{
    private bool disposed;

    internal SentinelModernCardScope(bool isVisible)
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
        disposed = true;
    }
}
