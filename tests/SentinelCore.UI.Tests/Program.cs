using System.Numerics;
using SentinelCore.UI;

var tests = new (string Name, Action Run)[]
{
    ("opt-in theme state", TestThemeState),
    ("persisted theme normalization", TestThemeNormalization),
    ("responsive modern layout", TestResponsiveLayout),
    ("modern status tokens", TestStatusTokens),
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL {test.Name}: {exception.Message}");
    }
}

foreach (var failure in failures)
    Console.Error.WriteLine(failure);

Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} UI test groups passed.");
return failures.Count == 0 ? 0 : 1;

static void TestThemeState()
{
    var state = new SentinelThemeState<TestPage>(TestPage.General);
    Equal(SentinelThemeKind.Classic, state.Theme);
    False(state.IsModern);
    False(state.SelectTheme(SentinelThemeKind.Classic));
    True(state.SelectTheme(SentinelThemeKind.Modern));
    True(state.IsModern);
    True(state.SelectPage(TestPage.Appearance));
    Equal(TestPage.Appearance, state.SelectedPage);
    False(state.SelectPage(TestPage.Appearance));
    Throws<ArgumentOutOfRangeException>(() => state.SelectTheme((SentinelThemeKind)99));
    Throws<ArgumentOutOfRangeException>(() => state.SelectPage((TestPage)99));
}

static void TestThemeNormalization()
{
    Equal(SentinelThemeKind.Classic, SentinelThemeState<TestPage>.NormalizeTheme(0));
    Equal(SentinelThemeKind.Modern, SentinelThemeState<TestPage>.NormalizeTheme(1));
    Equal(SentinelThemeKind.Classic, SentinelThemeState<TestPage>.NormalizeTheme(-1));
    Equal(SentinelThemeKind.Classic, SentinelThemeState<TestPage>.NormalizeTheme(99));
}

static void TestResponsiveLayout()
{
    var wide = SentinelModernLayout.Resolve(new Vector2(1000f, 700f));
    False(wide.IsCompact);
    Equal(218f, wide.NavigationSize.X);
    Equal(700f, wide.NavigationSize.Y);

    var narrow = SentinelModernLayout.Resolve(new Vector2(600f, 700f));
    True(narrow.IsCompact);
    Equal(600f, narrow.NavigationSize.X);
    Equal(210f, narrow.NavigationSize.Y);
    Equal(490f, narrow.ContentSize.Y);

    var scaled = SentinelModernLayout.Resolve(new Vector2(1200f, 800f), 1.5f);
    False(scaled.IsCompact);
    Equal(273f, scaled.NavigationSize.X);

    Throws<ArgumentOutOfRangeException>(() => SentinelModernLayout.Resolve(new Vector2(-1f, 10f)));
    Throws<ArgumentOutOfRangeException>(() => SentinelModernLayout.Resolve(new Vector2(10f, 10f), 0f));
}

static void TestStatusTokens()
{
    Equal(SentinelModernPalette.Muted, SentinelModernPalette.ForStatus(SentinelModernStatusTone.Neutral));
    Equal(SentinelModernPalette.Accent, SentinelModernPalette.ForStatus(SentinelModernStatusTone.Accent));
    Equal(SentinelModernPalette.Teal, SentinelModernPalette.ForStatus(SentinelModernStatusTone.Success));
    Equal(SentinelModernPalette.Rose, SentinelModernPalette.ForStatus(SentinelModernStatusTone.Warning));
    Equal(SentinelModernPalette.Violet, SentinelModernPalette.ForStatus(SentinelModernStatusTone.Violet));
}

static void True(bool value)
{
    if (!value)
        throw new InvalidOperationException("Expected true.");
}

static void False(bool value) => True(!value);

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
}

static void Throws<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

enum TestPage
{
    General,
    Appearance,
}
