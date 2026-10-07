using System.Numerics;
using System.Text.Json;
using SentinelCore.Diagnostics;
using SentinelCore.Identity;
using SentinelCore.Navigation;

internal static class NavigationTests
{
    public static void Run()
    {
        OwnershipAndLateResults();
        CancellationAndDisposal();
        ZoneAndReadiness();
        StaleOriginAndEndpoint();
        RetryAndTimeoutBudgets();
        WaypointProgress();
        FlightLifecycle();
        LandingAndDiagnostics();
        LandingSuccess();
        LandingFailures();
        LandingCancellation();
        LandingReplacementAndLateResults();
        FaultsAndThreadAffinity();
        Console.WriteLine("PASS navigation: 13 scenario groups (ownership, cancellation, readiness, stale paths, budgets, progress, flight, export, faults)");
    }
    private static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
    private static readonly Vector3 Target = new(100, 0, 0);
    private static void OwnershipAndLateResults()
    {
        using var h = new Harness();
        var a = h.Start(); h.Core.Tick();
        var old = h.Backend.Tasks[0];
        var b = h.Start(); h.Core.Tick();
        Check(a.Result == NavigationResult.Cancelled && a.Id != b.Id, "supersession identity");
        old.SetResult([Vector3.Zero, Target]); h.Core.Tick();
        Check(h.Backend.Follows == 0, "stale result submitted");
        h.Complete(); h.Core.Tick();
        Check(h.Backend.Follows == 1, "current path missing");
        a.Cancel(); a.Dispose();
        Check(h.Backend.Stops == 0, "old cancellation stopped successor");
        var c = h.Start();
        Check(h.Backend.Stops == 1 && b.Result == NavigationResult.Cancelled, "owned follower not stopped");
        h.Core.Tick(); h.Complete(); h.Core.Tick(); b.Dispose();
        Check(h.Backend.Stops == 1 && c.Result == NavigationResult.Pending, "old disposal affected new route");
    }
    private static void CancellationAndDisposal()
    {
        using var h = new Harness();
        var op = h.Start(); h.Core.Tick();
        var token = h.Backend.Tokens[0];
        op.Cancel();
        Check(token.IsCancellationRequested && op.Result == NavigationResult.Cancelled, "cancel missing");
        h.Backend.Tasks[0].SetException(new InvalidOperationException("SECRET-EXCEPTION"));
        h.Core.Tick();
        Check(h.Backend.Follows == 0 && h.Backend.Stops == 0, "abandoned task mutated backend");
        h.Start(); h.Core.Tick(); h.Complete(); h.Core.Tick();
        h.Core.Dispose(); h.Core.Dispose();
        Check(h.Backend.Stops == 1, "dispose not idempotent");
        Throws<ObjectDisposedException>(() => h.Start());
    }
    private static void ZoneAndReadiness()
    {
        var clock = new Clock();
        var gate = new ZoneReadinessGate(TimeSpan.FromSeconds(2), clock);
        var zone = new ZoneStamp(1, 1);
        Check(gate.Observe(zone, false, true, -1) is null, "new epoch trusted immediately");
        clock.Advance(2);
        Check(gate.Observe(zone, false, true, -1) == zone, "stable ready never confirmed");
        Check(gate.Observe(new(1, 2), false, true, -1) is null, "same-territory epoch not reset");
        clock.Advance(2);
        Check(gate.Observe(new(1, 2), false, true, 0) is null, "building accepted by gate");
        Check(gate.Observe(new(1, 2), false, true, -1) is null, "build did not reset settle");
        using var h = new Harness();
        h.Backend.State = h.Backend.State with { MeshZone = null };
        var op = h.Start(); h.Core.Tick();
        Check(h.Backend.Tasks.Count == 0, "unknown zone ready");
        h.Backend.State = h.Backend.State with { MeshZone = h.Backend.State.Zone, BuildProgress = 0.5f };
        h.Core.Tick(); Check(h.Backend.Tasks.Count == 0, "building old mesh accepted");
        h.Backend.State = h.Backend.State with { BuildProgress = float.NaN };
        h.Core.Tick(); Check(h.Backend.Tasks.Count == 0, "NaN accepted");
        h.Backend.State = h.Backend.State with { BuildProgress = -1 };
        h.Core.Tick(); Check(h.Backend.Tasks.Count == 1, "ready path missing");
        h.Backend.State = h.Backend.State with { Zone = new(2, 2) };
        h.Complete(); h.Core.Tick();
        Check(op.Result == NavigationResult.Cancelled && h.Backend.Follows == 0, "cross-zone path followed");
        using var slow = new Harness(new() { ReadinessSettle = TimeSpan.FromSeconds(2), RetryDelay = TimeSpan.Zero });
        slow.Start(); slow.Core.Tick(); slow.Clock.Advance(1); slow.Core.Tick();
        Check(slow.Backend.Tasks.Count == 0, "settle skipped");
        slow.Clock.Advance(1); slow.Core.Tick(); Check(slow.Backend.Tasks.Count == 1, "settle never released");
        slow.Backend.State = slow.Backend.State with { Loading = true };
        slow.Core.Tick(); slow.Complete(); slow.Core.Tick();
        Check(slow.Backend.Follows == 0, "loading result submitted");
    }
    private static void StaleOriginAndEndpoint()
    {
        using var h = new Harness();
        var op = h.Start(); h.Core.Tick();
        h.Backend.State = h.Backend.State with { Position = new(30, 0, 0) };
        h.Complete(); h.Core.Tick();
        Check(op.Retries == 1 && h.Backend.Follows == 0, "displaced origin accepted");
        h.Core.Tick();
        h.Backend.Tasks[^1].SetResult([new(30, 0, 0), new(31, 0, 0)]); h.Core.Tick();
        Check(op.Retries == 2 && h.Backend.Follows == 0, "partial endpoint accepted");
        h.Core.Tick();
        h.Backend.Tasks[^1].SetResult([new(float.NaN, 0, 0), Target]); h.Core.Tick();
        Check(op.Result == NavigationResult.Failure, "nonfinite path accepted");
    }
    private static void RetryAndTimeoutBudgets()
    {
        using var h = new Harness();
        var op = h.Start();
        for (var i = 0; i < 3; ++i)
        { h.Core.Tick(); h.Backend.Tasks[^1].SetResult([]); h.Core.Tick(); }
        Check(op.Result == NavigationResult.Failure && h.Backend.Tasks.Count == 3, "retry unbounded");
        h.Core.Tick(); Check(h.Backend.Tasks.Count == 3, "terminal operation retried");
        using var pending = new Harness();
        var q = pending.Start(); pending.Core.Tick(); pending.Clock.Advance(61); pending.Core.Tick();
        Check(q.Retries == 1 && pending.Backend.Tokens[0].IsCancellationRequested, "query timeout missing");
        pending.Backend.Tasks[0].SetResult([Vector3.Zero, Target]);
        pending.Core.Tick(); Check(pending.Backend.Follows == 0, "timed-out result submitted");
        pending.Clock.Advance(301); pending.Core.Tick();
        Check(q.Result == NavigationResult.Failure, "total deadline missing");
        using var unready = new Harness();
        unready.Backend.State = unready.Backend.State with { MeshReady = false };
        var r = unready.Start(); unready.Core.Tick(); unready.Clock.Advance(91); unready.Core.Tick();
        Check(r.Result == NavigationResult.Failure && unready.Backend.Tasks.Count == 0, "readiness unbounded");
    }
    private static void WaypointProgress()
    {
        using var h = new Harness();
        var op = h.Follow(); h.Core.Tick();
        h.Clock.Advance(10);
        h.Backend.State = h.Backend.State with { Position = new(0, 0, 5) };
        h.Core.Tick(); h.Clock.Advance(6); h.Core.Tick();
        Check(op.Retries == 1 && h.Backend.Stops == 1, "sideways movement hid stall");
        using var forward = new Harness();
        var a = forward.Follow(); forward.Core.Tick(); forward.Clock.Advance(10);
        forward.Backend.State = forward.Backend.State with { Position = new(5, 0, 0) };
        forward.Core.Tick(); forward.Clock.Advance(10); forward.Core.Tick();
        Check(a.Retries == 0, "waypoint progress ignored");
        forward.Backend.State = forward.Backend.State with { Position = Target };
        forward.Core.Tick();
        Check(a.Result == NavigationResult.Success && forward.Backend.Stops == 1, "arrival not released");
    }
    private static void FlightLifecycle()
    {
        using var h = new Harness();
        h.Backend.State = h.Backend.State with { Mounted = false, InFlight = false };
        var op = h.Core.Begin(new(Target));
        h.Core.Tick(); h.Core.Tick(); Check(h.Backend.Mounts == 1 && h.Backend.Tasks.Count == 0, "mount spam or premature path");
        h.Backend.State = h.Backend.State with { Mounted = true };
        h.Core.Tick(); Check(h.Backend.Takeoffs == 1 && h.Backend.Tasks.Count == 0, "takeoff confirmation bypassed");
        h.Backend.State = h.Backend.State with { InFlight = true };
        h.Core.Tick(); h.Complete(); h.Core.Tick();
        Check(h.Backend.LastFly, "flight path not requested");
        h.Backend.State = h.Backend.State with { InFlight = false };
        h.Core.Tick(); Check(op.Retries == 1 && h.Backend.Stops == 1, "lost flight did not stop");
        h.Core.Tick(); Check(h.Backend.Tasks.Count == 1, "failed takeoff silently fell back to ground");
        h.Backend.State = h.Backend.State with { Flight = FlightAvailability.Unavailable };
        h.Core.Tick(); h.Complete(); h.Core.Tick();
        Check(!h.Backend.LastFly, "explicit unavailable did not replan ground");
        using var unknown = new Harness();
        unknown.Backend.State = unknown.Backend.State with { Flight = FlightAvailability.Unknown };
        var a = unknown.Start(); unknown.Core.Tick(); unknown.Clock.Advance(31); unknown.Core.Tick();
        Check(a.Result == NavigationResult.Failure && unknown.Backend.Tasks.Count == 0, "unknown flight accepted");
    }
    private static void LandingAndDiagnostics()
    {
        using var h = new Harness();
        h.Backend.Projected = new(1, 0, 0);
        Check(h.Core.ProjectLanding(Vector3.Zero, 3) == h.Backend.Projected, "projection missing");
        h.Backend.Projected = new(99, 0, 0);
        Check(h.Core.ProjectLanding(Vector3.Zero, 3) is null, "unbounded projection accepted");
        var op = h.Follow(); op.Cancel();
        using var json = JsonDocument.Parse(h.Diagnostics.ExportJson());
        var entries = json.RootElement.GetProperty("Entries");
        Check(entries.GetArrayLength() > 1, "transitions missing");
        var last = entries[entries.GetArrayLength() - 1];
        Check(last.GetProperty("OperationId").GetGuid() == op.Id &&
            last.GetProperty("Result").GetString() == "Cancelled", "correlation/result missing");
        var export = h.Diagnostics.ExportJson();
        Check(!export.Contains("SECRET") && !export.Contains("Position") &&
            !export.Contains("Exception") && !export.Contains("DisplayName"), "export leaked private fields");
        for (var i = 0; i < 20; ++i) h.Start().Cancel();
        Check(h.Diagnostics.Snapshot().Count == 16, "diagnostics unbounded");
    }
    private static NavigationOperation StartLanding(Harness h)
    {
        var op = h.Core.Begin(new(Target) { RequireLanding = true });
        h.Core.Tick(); h.Complete(); h.Core.Tick();
        h.Backend.State = h.Backend.State with { Position = Target };
        h.Core.Tick();
        Check(op.State == NavigationState.Landing && op.Result == NavigationResult.Pending &&
            h.Backend.Stops == 1, "landing must retain pending operation after follower stop");
        return op;
    }
    private static void LandingSuccess()
    {
        using var h = new Harness();
        var op = StartLanding(h);
        h.Core.Tick(); h.Core.Tick();
        Check(h.Backend.Landings == 1 && op.Result == NavigationResult.Pending,
            "landing request spammed or action acceptance became success");
        h.Backend.State = h.Backend.State with { InFlight = false, Grounded = null };
        h.Core.Tick(); h.Clock.Advance(1); h.Core.Tick();
        Check(op.Result == NavigationResult.Pending, "InFlight alone proved ground");
        h.Backend.State = h.Backend.State with { Grounded = true };
        h.Core.Tick(); h.Clock.Advance(0.5); h.Core.Tick();
        Check(op.Result == NavigationResult.Pending, "ground settle skipped");
        h.Backend.State = h.Backend.State with { Grounded = false };
        h.Core.Tick();
        h.Backend.State = h.Backend.State with { Grounded = true };
        h.Core.Tick(); h.Clock.Advance(0.8); h.Core.Tick();
        Check(op.Result == NavigationResult.Success && h.Backend.Landings == 1,
            "stable ground never confirmed or ground dismount requested");
        var last = h.Diagnostics.Snapshot()[^1];
        Check(last.Reason == NavigationReason.GroundConfirmed && last.PhysicalContext.Grounded == true &&
            !last.PhysicalContext.InFlight, "grounded success evidence missing");
        using var legacy = new Harness();
        var normal = legacy.Follow();
        legacy.Backend.State = legacy.Backend.State with { Position = Target };
        legacy.Core.Tick();
        Check(normal.Result == NavigationResult.Success && legacy.Backend.Landings == 0,
            "legacy destination arrival changed");
        using var unsupported = new NavigationCoordinator(new LegacyBackend(), h.Diagnostics);
        Throws<NotSupportedException>(() => unsupported.Begin(new(Target) { RequireLanding = true }));
    }
    private static void LandingFailures()
    {
        using var h = new Harness();
        h.Backend.AcceptLanding = false;
        var op = StartLanding(h);
        h.Core.Tick();
        Check(h.Diagnostics.Snapshot()[^1].Reason == NavigationReason.LandingRejected,
            "rejected landing hidden");
        h.Clock.Advance(19); h.Core.Tick();
        Check(op.Result == NavigationResult.Pending, "landing deadline premature");
        h.Clock.Advance(1); h.Core.Tick();
        Check(op.Result == NavigationResult.Failure &&
            h.Diagnostics.Snapshot()[^1].Reason == NavigationReason.LandingTimeout,
            "landing retries reset timeout");
        var requests = h.Backend.Landings;
        h.Clock.Advance(100); h.Core.Tick();
        Check(h.Backend.Landings == requests, "terminal landing action continued");
        using var drift = new Harness();
        var d = StartLanding(drift);
        drift.Backend.State = drift.Backend.State with { Position = Target + new Vector3(4, 0, 0) };
        drift.Core.Tick();
        Check(d.Result == NavigationResult.Failure && drift.Backend.Landings == 0 &&
            drift.Diagnostics.Snapshot()[^1].Reason == NavigationReason.LandingDrift, "unsafe landing drift accepted");
        using var mesh = new Harness();
        var m = StartLanding(mesh);
        mesh.Backend.State = mesh.Backend.State with { MeshReady = false };
        mesh.Core.Tick();
        Check(m.Result == NavigationResult.Failure && mesh.Backend.Takeoffs == 0,
            "landing dependency loss restarted flight");
        using var high = new Harness();
        var p = StartLanding(high);
        high.Backend.State = high.Backend.State with { Position = Target + new Vector3(0, 2, 0),
            InFlight = false, Grounded = true };
        high.Core.Tick(); high.Clock.Advance(21); high.Core.Tick();
        Check(p.Result == NavigationResult.Failure, "ground evidence far above intended floor accepted");
    }
    private static void LandingCancellation()
    {
        using var h = new Harness();
        var op = StartLanding(h);
        h.Core.Tick();
        op.Cancel(); op.Dispose();
        var actions = h.Backend.Landings;
        h.Clock.Advance(30); h.Core.Tick();
        Check(op.Result == NavigationResult.Cancelled && h.Backend.Landings == actions,
            "cancelled landing kept acting");
        using var zone = new Harness();
        var z = StartLanding(zone);
        zone.Backend.State = zone.Backend.State with { Zone = new(2, 2) };
        zone.Core.Tick();
        Check(z.Result == NavigationResult.Cancelled && zone.Backend.Landings == 0,
            "zoning landing retained authority");
    }
    private static void LandingReplacementAndLateResults()
    {
        using var h = new Harness();
        var landing = StartLanding(h);
        h.Core.Tick();
        h.Backend.State = h.Backend.State with { Position = Vector3.Zero };
        var replacement = h.Start();
        h.Core.Tick(); h.Complete(); h.Core.Tick();
        var stops = h.Backend.Stops;
        var actions = h.Backend.Landings;
        landing.Cancel(); landing.Dispose(); h.Clock.Advance(2); h.Core.Tick();
        Check(landing.Result == NavigationResult.Cancelled && replacement.Result == NavigationResult.Pending &&
            h.Backend.Stops == stops && h.Backend.Landings == actions,
            "retired landing stopped or acted on replacement");
        using var late = new Harness();
        var old = late.Start(); late.Core.Tick();
        var task = late.Backend.Tasks[0];
        var newer = StartLanding(late);
        task.SetResult([Vector3.Zero, Target]);
        var follows = late.Backend.Follows;
        late.Core.Tick(); old.Dispose();
        Check(newer.State == NavigationState.Landing && late.Backend.Follows == follows &&
            late.Backend.Stops == 1, "late path acquired landing ownership");
        late.Core.Dispose();
        actions = late.Backend.Landings;
        late.Clock.Advance(20); late.Core.Tick();
        Check(late.Backend.Landings == actions && newer.Result == NavigationResult.Cancelled,
            "disposed landing issued delayed actions");
    }
    private sealed class LegacyBackend : INavigationAdapter
    {
        private readonly Backend backend = new();
        public NavigationSnapshot Read() => backend.Read();
        public Task<IReadOnlyList<Vector3>> FindPath(Vector3 from, Vector3 to, bool fly, CancellationToken cancellation)
            => backend.FindPath(from, to, fly, cancellation);
        public void Follow(IReadOnlyList<Vector3> path, bool fly) => backend.Follow(path, fly);
        public void Stop() => backend.Stop();
        public void RequestMount() => backend.RequestMount();
        public void RequestTakeoff() => backend.RequestTakeoff();
        public Vector3? ProjectLanding(Vector3 candidate, float searchRadius) => backend.ProjectLanding(candidate, searchRadius);
    }

    private static void FaultsAndThreadAffinity()
    {
        using var h = new Harness();
        var op = h.Follow(); h.Backend.ThrowStop = true; op.Cancel();
        Check(op.Result == NavigationResult.Failure, "stop failure hidden");
        Throws<InvalidOperationException>(() => h.Start());
        using var external = new Harness();
        external.Backend.State = external.Backend.State with { Following = true };
        Throws<InvalidOperationException>(() => external.Start());
        Check(external.Backend.Stops == 0, "external route seized");
        using var threaded = new Harness();
        Exception? error = null;
        var thread = new Thread(() => { try { threaded.Core.Tick(); } catch (Exception e) { error = e; } });
        thread.Start(); thread.Join();
        Check(error is InvalidOperationException, "thread affinity missing");
    }
    private sealed class Clock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(ticks);
        public void Advance(double seconds) => ticks += TimeSpan.FromSeconds(seconds).Ticks;
    }
    private sealed class Harness : IDisposable
    {
        public readonly Clock Clock = new();
        public readonly Backend Backend = new();
        public readonly NavigationDiagnostics Diagnostics;
        public readonly NavigationCoordinator Core;
        public Harness(NavigationOptions? options = null)
        {
            Diagnostics = new(new("SRankSentinel", "SECRET-DISPLAY", "SECRET-AUTHOR", new(0, 7, 54, 0)), 16, Clock);
            Core = new(Backend, Diagnostics, options ?? new() { ReadinessSettle = TimeSpan.Zero, RetryDelay = TimeSpan.Zero }, Clock);
        }
        public NavigationOperation Start() => Core.Begin(new(Target));
        public void Complete() => Backend.Tasks[^1].SetResult([Backend.State.Position, Target]);
        public NavigationOperation Follow() { var op = Start(); Core.Tick(); Complete(); Core.Tick(); return op; }
        public void Dispose() => Core.Dispose();
    }
    private sealed class Backend : ILandingNavigationAdapter
    {
        public NavigationSnapshot State = new(new(1, 1), new ZoneStamp(1, 1), false, true, -1,
            Vector3.Zero, true, true, FlightAvailability.Available, false, []);
        public readonly List<TaskCompletionSource<IReadOnlyList<Vector3>>> Tasks = new();
        public readonly List<CancellationToken> Tokens = new();
        public int Follows, Stops, Mounts, Takeoffs, Landings;
        public bool AcceptLanding = true;
        public bool LastFly, ThrowStop;
        public Vector3? Projected;
        public NavigationSnapshot Read() => State;
        public Task<IReadOnlyList<Vector3>> FindPath(Vector3 from, Vector3 to, bool fly, CancellationToken cancellation)
        {
            var task = new TaskCompletionSource<IReadOnlyList<Vector3>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Tasks.Add(task); Tokens.Add(cancellation); LastFly = fly; return task.Task;
        }
        public void Follow(IReadOnlyList<Vector3> path, bool fly)
        {
            Follows++;
            State = State with { Following = true, RemainingWaypoints = [Target] };
        }
        public void Stop()
        {
            Stops++;
            if (ThrowStop) throw new InvalidOperationException("SECRET-STOP");
            State = State with { Following = false, RemainingWaypoints = [] };
        }
        public void RequestMount() => Mounts++;
        public void RequestTakeoff() => Takeoffs++;
        public bool RequestLanding(NavigationSnapshot snapshot, Vector3 destination)
        { Landings++; return AcceptLanding; }
        public Vector3? ProjectLanding(Vector3 candidate, float searchRadius) => Projected;
    }
}
