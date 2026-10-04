using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SentinelCore.UI;

public readonly record struct SentinelModernStatus(
    string Text,
    SentinelModernStatusTone Tone = SentinelModernStatusTone.Neutral);

public readonly record struct SentinelModernShellOptions(
    string Id,
    string Brand,
    string Title,
    string Subtitle)
{
    public string? ContextLabel { get; init; }

    public SentinelModernStatus? Status { get; init; }

    public float Scale { get; init; } = 1f;

    public float HeaderHeight { get; init; } = 84f;

    public bool DrawAmbientBackground { get; init; } = true;

    public float AmbientIntensity { get; init; } = 1f;

    public SentinelModernLayoutOptions Layout { get; init; } = SentinelModernLayoutOptions.Default;
}

/// <summary>
/// Draws the reusable content shell inside an already-open Dalamud configuration window.
/// It deliberately does not call ImGui.Begin/End, so the consumer keeps standard title-bar,
/// collapse, close, sizing, and window persistence behavior.
/// </summary>
public static class SentinelModernConfigurationShell
{
    private const float MinimumHeaderHeight = 84f;

    public static void Draw(
        SentinelModernShellOptions options,
        Action drawNavigation,
        Action drawContent)
    {
        Validate(options);
        ArgumentNullException.ThrowIfNull(drawNavigation);
        ArgumentNullException.ThrowIfNull(drawContent);

        if (options.DrawAmbientBackground)
            SentinelModernAmbient.DrawRings(options.Scale, options.AmbientIntensity);

        ImGui.PushID(options.Id);
        try
        {
            DrawHeader(options);
            ImGui.Spacing();

            var available = ImGui.GetContentRegionAvail();
            var layout = SentinelModernLayout.Resolve(available, options.Scale, options.Layout);
            DrawChild("##Navigation", layout.NavigationSize, true, drawNavigation);

            if (layout.IsCompact)
                ImGui.Spacing();
            else
                ImGui.SameLine();

            DrawChild("##Content", Vector2.Zero, false, drawContent);
        }
        finally
        {
            ImGui.PopID();
        }
    }

    private static void DrawHeader(SentinelModernShellOptions options)
    {
        var headerHeight = MathF.Max(options.HeaderHeight, MinimumHeaderHeight) * options.Scale;
        var visible = ImGui.BeginChild(
            "##Header",
            new Vector2(0f, headerHeight),
            true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        try
        {
            if (!visible)
                return;

            ImGui.TextColored(SentinelModernPalette.AccentStrong, options.Brand);
            ImGui.TextColored(SentinelModernPalette.Text, options.Title);
            if (!string.IsNullOrWhiteSpace(options.ContextLabel))
            {
                ImGui.SameLine();
                ImGui.TextColored(SentinelModernPalette.Muted, options.ContextLabel);
            }

            ImGui.TextColored(SentinelModernPalette.Muted, options.Subtitle);

            if (options.Status is { } status)
            {
                var textSize = ImGui.CalcTextSize(status.Text);
                var chipWidth = textSize.X + (18f * options.Scale);
                ImGui.SetCursorPos(new Vector2(
                    MathF.Max(12f * options.Scale, ImGui.GetWindowWidth() - chipWidth - (16f * options.Scale)),
                    24f * options.Scale));
                SentinelModernUi.StatusChip(status.Text, status.Tone, options.Scale);
            }
        }
        finally
        {
            ImGui.EndChild();
        }
    }

    private static void DrawChild(string id, Vector2 size, bool border, Action draw)
    {
        var visible = ImGui.BeginChild(id, size, border);
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

    private static void Validate(SentinelModernShellOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Title);
        ArgumentNullException.ThrowIfNull(options.Subtitle);
        if (!float.IsFinite(options.Scale) || options.Scale <= 0f)
            throw new ArgumentOutOfRangeException(nameof(options), "Scale must be finite and positive.");
        if (!float.IsFinite(options.HeaderHeight) || options.HeaderHeight <= 0f)
            throw new ArgumentOutOfRangeException(nameof(options), "Header height must be finite and positive.");
        if (!float.IsFinite(options.AmbientIntensity)
            || options.AmbientIntensity < 0f
            || options.AmbientIntensity > 1f)
            throw new ArgumentOutOfRangeException(nameof(options), "Ambient intensity must be between zero and one.");
        if (options.Status is { } status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(status.Text);
            if (!Enum.IsDefined(status.Tone))
                throw new ArgumentOutOfRangeException(nameof(options), "Status tone is invalid.");
        }
    }
}
