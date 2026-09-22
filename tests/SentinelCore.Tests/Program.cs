using SentinelCore.Configuration;
using SentinelCore.Diagnostics;
using SentinelCore.Identity;
using SentinelCore.Ipc;
using SentinelCore.Jobs;
using SentinelCore.Lifecycle;

var tests = new (string Name, Action Run)[]
{
    ("four-part versions", TestVersions),
    ("identity validation", TestIdentity),
    ("configuration coordination", TestConfiguration),
    ("diagnostic buffer and gates", TestDiagnostics),
    ("safe LIFO disposal", TestDisposal),
    ("plugin lifetime cancellation", TestLifetime),
    ("dynamic job classification", TestJobClassification),
    ("versioned IPC conventions", TestIpcConventions),
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

Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} test groups passed.");
return failures.Count == 0 ? 0 : 1;

static void TestVersions()
{
    var parsed = SentinelVersion.Parse("0.7.38.0");
    Equal(new SentinelVersion(0, 7, 38, 0), parsed);
    Equal("0.7.38.0", parsed.ToString());
    True(parsed > new SentinelVersion(0, 7, 37, 9));
    False(SentinelVersion.TryParse("0.7.38", out _));
    False(SentinelVersion.TryParse("0.7.-1.0", out _));
}

static void TestIdentity()
{
    var identity = new SentinelIdentity(
        "SentinelHUD",
        "Sentinel HUD",
        "MTitan",
        new SentinelVersion(0, 1, 0, 0));
    Equal("[Sentinel HUD 0.1.0.0]", identity.DiagnosticPrefix);
    Throws<ArgumentException>(() => new SentinelIdentity(
        "Sentinel HUD",
        "Sentinel HUD",
        "MTitan",
        new SentinelVersion(0, 1, 0, 0)));
}

static void TestConfiguration()
{
    var clock = new TestTimeProvider(DateTimeOffset.Parse("2026-09-22T00:00:00Z"));
    var store = new MemoryStore<TestConfiguration>();
    using var coordinator = new ConfigurationCoordinator<TestConfiguration>(
        store,
        static () => new TestConfiguration(),
        static configuration =>
        {
            configuration.SchemaVersion = 2;
            configuration.Value = Math.Clamp(configuration.Value, 0, 100);
            return configuration;
        },
        clock);

    Equal(1, store.SaveCount);
    coordinator.Update(configuration => configuration.Value = 42, TimeSpan.FromSeconds(1));
    False(coordinator.FlushIfDue());
    clock.Advance(TimeSpan.FromSeconds(1));
    True(coordinator.FlushIfDue());
    Equal(2, store.SaveCount);
    Equal(42, store.LastSaved?.Value);

    coordinator.Update(configuration => configuration.Value = 500, TimeSpan.Zero);
    Equal(100, coordinator.Current.Value);
    Equal(3, store.SaveCount);
}

static void TestDiagnostics()
{
    var clock = new TestTimeProvider(DateTimeOffset.Parse("2026-09-22T00:00:00Z"));
    var buffer = new DiagnosticBuffer(2, clock);
    buffer.Information("one");
    buffer.Warning("two");
    buffer.Error("three");
    Equal(2, buffer.Snapshot().Count);
    Equal("two", buffer.Snapshot()[0].Message);

    var tracker = new DiagnosticTracker(clock);
    True(tracker.Changed("state", "idle"));
    False(tracker.Changed("state", "idle"));
    True(tracker.Changed("state", "active"));
    True(tracker.Throttled("poll", TimeSpan.FromSeconds(5)));
    False(tracker.Throttled("poll", TimeSpan.FromSeconds(5)));
    clock.Advance(TimeSpan.FromSeconds(5));
    True(tracker.Throttled("poll", TimeSpan.FromSeconds(5)));
}

static void TestDisposal()
{
    var order = new List<int>();
    var errors = new List<Exception>();
    var bag = new DisposableBag(errors.Add);
    bag.Add(() => order.Add(1));
    bag.Add(() => throw new InvalidOperationException("expected"));
    bag.Add(() => order.Add(3));
    bag.Dispose();
    bag.Dispose();
    SequenceEqual(new[] { 3, 1 }, order);
    Equal(1, errors.Count);

    var disposedImmediately = false;
    bag.Add(() => disposedImmediately = true);
    True(disposedImmediately);
}

static void TestLifetime()
{
    var cleanupCalled = false;
    var lifetime = new PluginLifetime();
    var token = lifetime.Token;
    lifetime.Add(() => cleanupCalled = true);
    lifetime.Dispose();
    True(token.IsCancellationRequested);
    True(cleanupCalled);
}

static void TestJobClassification()
{
    Equal(JobCategory.Tank, JobClassifier.Classify(new JobClassificationInput(1, 0, false, false, false)));
    Equal(JobCategory.Healer, JobClassifier.Classify(new JobClassificationInput(4, 0, false, false, false)));
    Equal(JobCategory.MeleeDps, JobClassifier.Classify(new JobClassificationInput(2, 0, false, false, false)));
    Equal(JobCategory.PhysicalRangedDps, JobClassifier.Classify(new JobClassificationInput(3, 2, false, false, false)));
    Equal(JobCategory.MagicalRangedDps, JobClassifier.Classify(new JobClassificationInput(3, 4, false, false, false)));
    Equal(JobCategory.LimitedJob, JobClassifier.Classify(new JobClassificationInput(3, 4, true, false, false)));
    Equal(JobCategory.Crafter, JobClassifier.Classify(new JobClassificationInput(0, 0, false, true, false)));
    Equal(RoleHue.MagicalRanged, JobClassifier.ClassifyRoleHue(new JobClassificationInput(3, 4, true, false, false)));
}

static void TestIpcConventions()
{
    Equal("Sentinel.SentinelHUD.State.v1", SentinelIpcContract.Endpoint("SentinelHUD", "State"));
    Equal("Sentinel.SentinelHUD.State.v2", SentinelIpcContract.Endpoint("SentinelHUD", "State", 2));
    Throws<ArgumentException>(() => SentinelIpcContract.Endpoint("Sentinel HUD", "State"));
    Throws<ArgumentOutOfRangeException>(() => SentinelIpcContract.Endpoint("SentinelHUD", "State", 0));
}

static void True(bool value)
{
    if (!value) throw new InvalidOperationException("Expected true.");
}

static void False(bool value) => True(!value);

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
}

static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
{
    if (!expected.SequenceEqual(actual))
        throw new InvalidOperationException("Sequences were not equal.");
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

sealed class TestConfiguration
{
    public int SchemaVersion { get; set; }
    public int Value { get; set; }
}

sealed class MemoryStore<T> : IConfigurationStore<T>
    where T : class
{
    public T? Value { get; set; }
    public T? LastSaved { get; private set; }
    public int SaveCount { get; private set; }

    public T? Load() => Value;

    public void Save(T configuration)
    {
        LastSaved = configuration;
        SaveCount++;
    }
}

sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset now = now;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan value) => now += value;
}
