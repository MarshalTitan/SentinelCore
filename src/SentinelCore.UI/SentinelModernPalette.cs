using System.Numerics;

namespace SentinelCore.UI;

/// <summary>
/// Sentinel Modern colour tokens derived from the live-tested Sentinel HUD 0.8.3.0 theme.
/// Consumers should use these semantic tokens instead of duplicating colour literals.
/// </summary>
public static class SentinelModernPalette
{
    public static readonly Vector4 Canvas = new(0.027f, 0.035f, 0.075f, 0.985f);
    public static readonly Vector4 Surface = new(0.047f, 0.063f, 0.118f, 0.94f);
    public static readonly Vector4 SurfaceRaised = new(0.071f, 0.092f, 0.165f, 0.98f);
    public static readonly Vector4 SurfaceHover = new(0.095f, 0.130f, 0.235f, 1f);
    public static readonly Vector4 SurfaceActive = new(0.105f, 0.150f, 0.275f, 1f);
    public static readonly Vector4 SurfaceGlassTop = new(0.072f, 0.102f, 0.190f, 0.92f);
    public static readonly Vector4 SurfaceGlassBottom = new(0.038f, 0.052f, 0.105f, 0.94f);
    public static readonly Vector4 SurfaceRail = new(0.030f, 0.044f, 0.092f, 0.88f);
    public static readonly Vector4 SurfaceDock = new(0.045f, 0.065f, 0.128f, 0.97f);
    public static readonly Vector4 NavigationSelected = new(0.115f, 0.245f, 0.465f, 0.95f);
    public static readonly Vector4 NavigationSelectedHover = new(0.135f, 0.285f, 0.530f, 1f);

    public static readonly Vector4 Border = new(0.180f, 0.239f, 0.388f, 0.92f);
    public static readonly Vector4 BorderBright = new(0.275f, 0.505f, 0.925f, 0.95f);
    public static readonly Vector4 Accent = new(0.275f, 0.565f, 1f, 1f);
    public static readonly Vector4 AccentStrong = new(0.360f, 0.650f, 1f, 1f);
    public static readonly Vector4 Violet = new(0.635f, 0.355f, 0.940f, 1f);
    public static readonly Vector4 Teal = new(0.180f, 0.720f, 0.700f, 1f);
    public static readonly Vector4 Rose = new(0.960f, 0.330f, 0.515f, 1f);
    public static readonly Vector4 Error = new(0.985f, 0.255f, 0.325f, 1f);
    public static readonly Vector4 Shadow = new(0.005f, 0.008f, 0.025f, 0.50f);
    public static readonly Vector4 Highlight = new(0.720f, 0.830f, 1f, 0.10f);

    public static readonly Vector4 Text = new(0.925f, 0.941f, 0.985f, 1f);
    public static readonly Vector4 Muted = new(0.590f, 0.635f, 0.740f, 1f);
    public static readonly Vector4 Subtle = new(0.390f, 0.435f, 0.550f, 1f);
    public static readonly Vector4 ToggleOff = new(0.215f, 0.250f, 0.335f, 1f);
    public static readonly Vector4 ToggleKnobOn = new(0.960f, 0.980f, 1f, 1f);
    public static readonly Vector4 ToggleKnobOff = new(0.720f, 0.750f, 0.820f, 1f);

    public static Vector4 ForStatus(SentinelModernStatusTone tone) => tone switch
    {
        SentinelModernStatusTone.Accent => Accent,
        SentinelModernStatusTone.Success => Teal,
        SentinelModernStatusTone.Warning => Rose,
        SentinelModernStatusTone.Violet => Violet,
        _ => Muted,
    };
}

public enum SentinelModernStatusTone
{
    Neutral = 0,
    Accent = 1,
    Success = 2,
    Warning = 3,
    Violet = 4,
}
