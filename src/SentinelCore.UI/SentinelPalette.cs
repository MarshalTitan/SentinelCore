using System.Numerics;
using SentinelCore.Jobs;

namespace SentinelCore.UI;

public sealed record SentinelRolePalette(
    Vector4 Idle,
    Vector4 Hover,
    Vector4 Active,
    Vector4 Border,
    Vector4 Header);

public static class SentinelPalette
{
    public static readonly Vector4 WindowBackground = new(0.026f, 0.031f, 0.040f, 0.96f);
    public static readonly Vector4 PanelBackground = new(0.055f, 0.064f, 0.082f, 0.98f);
    public static readonly Vector4 Border = new(0.58f, 0.47f, 0.27f, 0.92f);
    public static readonly Vector4 HeaderGold = new(0.92f, 0.78f, 0.44f, 1f);
    public static readonly Vector4 AccentBlue = new(0.20f, 0.78f, 0.96f, 1f);
    public static readonly Vector4 Text = new(0.94f, 0.95f, 0.97f, 1f);
    public static readonly Vector4 MutedText = new(0.62f, 0.66f, 0.72f, 1f);

    public static SentinelRolePalette ForRole(RoleHue role) => role switch
    {
        RoleHue.Tank => Build(new Vector4(0.16f, 0.37f, 0.66f, 1f)),
        RoleHue.Healer => Build(new Vector4(0.18f, 0.54f, 0.30f, 1f)),
        RoleHue.Melee => Build(new Vector4(0.64f, 0.20f, 0.22f, 1f)),
        RoleHue.PhysicalRanged => Build(new Vector4(0.72f, 0.38f, 0.13f, 1f)),
        RoleHue.MagicalRanged => Build(new Vector4(0.45f, 0.25f, 0.64f, 1f)),
        _ => Build(new Vector4(0.27f, 0.31f, 0.38f, 1f)),
    };

    private static SentinelRolePalette Build(Vector4 baseColour)
        => new(
            Scale(baseColour, 0.72f),
            Scale(baseColour, 1.10f),
            Scale(baseColour, 1.26f),
            Scale(baseColour, 1.34f),
            Scale(baseColour, 1.45f));

    private static Vector4 Scale(Vector4 colour, float scale)
        => new(
            Math.Clamp(colour.X * scale, 0f, 1f),
            Math.Clamp(colour.Y * scale, 0f, 1f),
            Math.Clamp(colour.Z * scale, 0f, 1f),
            colour.W);
}

