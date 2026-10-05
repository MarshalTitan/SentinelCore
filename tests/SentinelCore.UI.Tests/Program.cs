using System.Numerics;
using SentinelCore.UI;

var tests = new (string Name, Action Run)[]
{
    ("opt-in theme state", TestThemeState),
    ("persisted theme normalization", TestThemeNormalization),
    ("responsive modern layout", TestResponsiveLayout),
    ("modern status tokens", TestStatusTokens),
    ("modern 2 application layout", TestModern2Layout),
    ("modern 2 minimum sizes", TestModern2MinimumSizes),
    ("modern 2 reduced motion", TestModern2ReducedMotion),
    ("modern 2 motion channel lifecycle", TestModern2MotionLifecycle),
    ("modern 2 status pills", TestModern2StatusPills),
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
    False(narrow.IsCompact);
    Equal(182f, narrow.NavigationSize.X);
    Equal(700f, narrow.NavigationSize.Y);
    Equal(418f, narrow.ContentSize.X);

    var stacked = SentinelModernLayout.Resolve(
        new Vector2(600f, 700f),
        1f,
        SentinelModernLayoutOptions.Default with { AllowStackedNavigation = true });
    True(stacked.IsCompact);
    Equal(600f, stacked.NavigationSize.X);
    Equal(210f, stacked.NavigationSize.Y);
    Equal(490f, stacked.ContentSize.Y);

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

static void TestModern2Layout()
{
    var simple = SentinelModernAppLayout.Resolve(new Vector2(900f, 640f));
    Equal(56f, simple.HeaderHeight);
    Equal(584f, simple.BodyHeight);
    Equal(64f, simple.RailWidth);
    Equal(42f, simple.RailButtonSize);
    Equal(0f, simple.SecondarySidebarWidth);
    Equal(836f, simple.ContentWidth);
    Equal(584f, simple.ContentHeight);
    True(simple.IsUsable);
    False(simple.NavigationIsStacked);
    False(simple.HeaderAllowsScrolling);

    var complex = SentinelModernAppLayout.Resolve(
        new Vector2(960f, 720f),
        hasSecondarySidebar: true,
        hasActionDock: true);
    Equal(196f, complex.SecondarySidebarWidth);
    Equal(72f, complex.ActionDockHeight);
    Equal(600f, complex.ContentWidth);
    Equal(592f, complex.ContentHeight);
    False(complex.NavigationIsStacked);

    var narrow = SentinelModernAppLayout.Resolve(
        new Vector2(500f, 500f),
        hasSecondarySidebar: true);
    Equal(64f, narrow.RailWidth);
    Equal(196f, narrow.SecondarySidebarWidth);
    Equal(240f, narrow.ContentWidth);
    False(narrow.IsUsable);
    False(narrow.NavigationIsStacked);

    var scaled = SentinelModernAppLayout.Resolve(
        new Vector2(1440f, 1080f),
        1.5f,
        hasSecondarySidebar: true,
        hasActionDock: true);
    Equal(84f, scaled.HeaderHeight);
    Equal(96f, scaled.RailWidth);
    Equal(63f, scaled.RailButtonSize);
    Equal(294f, scaled.SecondarySidebarWidth);
    Equal(108f, scaled.ActionDockHeight);
    Equal(30f, scaled.ContentPadding);

    Throws<ArgumentOutOfRangeException>(() => SentinelModernAppLayout.Resolve(new Vector2(-1f, 100f)));
    Throws<ArgumentOutOfRangeException>(() => SentinelModernAppLayout.Resolve(new Vector2(100f), 0f));
}

static void TestModern2MinimumSizes()
{
    Equal(new Vector2(384f, 336f), SentinelModernAppLayout.MinimumWindowSize());
    Equal(
        new Vector2(580f, 408f),
        SentinelModernAppLayout.MinimumWindowSize(
            hasSecondarySidebar: true,
            hasActionDock: true));
    Equal(
        new Vector2(870f, 612f),
        SentinelModernAppLayout.MinimumWindowSize(
            1.5f,
            hasSecondarySidebar: true,
            hasActionDock: true));
}

static void TestModern2ReducedMotion()
{
    Equal(1f, SentinelModernMotion.Smooth(0f, 1f, 1f / 60f, 18f, true));
    var animated = SentinelModernMotion.Smooth(0f, 1f, 1f / 60f, 18f, false);
    True(animated > 0f && animated < 1f);
    Equal(1f, SentinelModernMotion.AdvanceProgress(0f, 0f, 0.24f, true));
    Equal(0.5f, SentinelModernMotion.AdvanceProgress(0f, 0.12f, 0.24f, false));
    Equal(1f, SentinelModernMotion.AdvanceProgress(0.9f, 0.12f, 0.24f, false));
}

static void TestModern2MotionLifecycle()
{
    using var motion = new SentinelModernMotion();
    motion.BeginFrame(1f / 60f, false);
    Equal(0f, motion.Approach("new", 0f));
    Equal(1, motion.ActiveChannelCount);
    var moving = motion.Approach("new", 1f);
    True(moving > 0f && moving < 1f);

    motion.BeginFrame(1f / 60f, true);
    Equal(1f, motion.Approach("new", 1f));
    Equal(0.5f, motion.Pulse());
    motion.Clear();
    Equal(0, motion.ActiveChannelCount);
}

static void TestModern2StatusPills()
{
    Equal(
        SentinelModernPalette.Teal,
        SentinelModernStatusPill.ResolveColour(
            new SentinelModernStatusPillOptions("READY", SentinelModernPillTone.Ready)));
    Equal(
        SentinelModernPalette.AccentStrong,
        SentinelModernStatusPill.ResolveColour(
            new SentinelModernStatusPillOptions("RUNNING", SentinelModernPillTone.Running)));
    Equal(
        SentinelModernPalette.Error,
        SentinelModernStatusPill.ResolveColour(
            new SentinelModernStatusPillOptions("ERROR", SentinelModernPillTone.Error)));
    var custom = new Vector4(0.2f, 0.3f, 0.4f, 1f);
    Equal(
        custom,
        SentinelModernStatusPill.ResolveColour(
            new SentinelModernStatusPillOptions("CUSTOM", SentinelModernPillTone.Custom)
            {
                CustomColour = custom,
            }));
    Throws<ArgumentException>(() => SentinelModernStatusPill.ResolveColour(
        new SentinelModernStatusPillOptions("CUSTOM", SentinelModernPillTone.Custom)));
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
