namespace SentinelCore.UI;

/// <summary>
/// Selects the presentation used by a Sentinel configuration surface.
/// Existing consumers remain on <see cref="Classic"/> until they explicitly opt in.
/// </summary>
public enum SentinelThemeKind
{
    Classic = 0,
    Modern = 1,
}

/// <summary>
/// Small, renderer-independent state holder for a themed, page-based configuration window.
/// Create one instance per window and persist <see cref="Theme"/> in the consumer's own
/// configuration when desired.
/// </summary>
/// <typeparam name="TPage">A consumer-owned enum identifying configuration pages.</typeparam>
public sealed class SentinelThemeState<TPage>
    where TPage : struct, Enum
{
    public SentinelThemeState(
        TPage initialPage,
        SentinelThemeKind initialTheme = SentinelThemeKind.Classic)
    {
        ValidatePage(initialPage);
        ValidateTheme(initialTheme);
        SelectedPage = initialPage;
        Theme = initialTheme;
    }

    public SentinelThemeKind Theme { get; private set; }

    public TPage SelectedPage { get; private set; }

    public bool IsModern => Theme == SentinelThemeKind.Modern;

    public bool SelectTheme(SentinelThemeKind theme)
    {
        ValidateTheme(theme);
        if (Theme == theme)
            return false;

        Theme = theme;
        return true;
    }

    public bool SelectPage(TPage page)
    {
        ValidatePage(page);
        if (EqualityComparer<TPage>.Default.Equals(SelectedPage, page))
            return false;

        SelectedPage = page;
        return true;
    }

    public static SentinelThemeKind NormalizeTheme(int persistedValue)
        => Enum.IsDefined((SentinelThemeKind)persistedValue)
            ? (SentinelThemeKind)persistedValue
            : SentinelThemeKind.Classic;

    private static void ValidateTheme(SentinelThemeKind theme)
    {
        if (!Enum.IsDefined(theme))
            throw new ArgumentOutOfRangeException(nameof(theme));
    }

    private static void ValidatePage(TPage page)
    {
        if (!Enum.IsDefined(page))
            throw new ArgumentOutOfRangeException(nameof(page));
    }
}
