using System.Numerics;
using SentinelCore.Diagnostics;

namespace SentinelCore.Navigation;

/// <summary>Framework-thread actor. Begin/Tick/Cancel/Dispose and projection must all run on the same
/// thread. Async completions only supply data; they never stop or replace movement.</summary>
public sealed class NavigationCoordinator : IDisposable
{
    private readonly INavigationAdapter adapter;
    private readonly NavigationOptions options;
    private readonly NavigationDiagnostics diagnostics;
    private readonly TimeProvider clock;
    private readonly int thread = Environment.CurrentManagedThreadId;
    private NavigationOperation? active;
    private bool disposed;
    private bool poisoned;

    public NavigationCoordinator(INavigationAdapter adapter, NavigationDiagnostics diagnostics,
        NavigationOptions? options = null, TimeProvider? clock = null)
    {
        this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        this.diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        this.options = options ?? new();
        this.options.Validate();
        this.clock = clock ?? TimeProvider.System;
    }

    public NavigationOperation Begin(NavigationRequest request)
    {
        CheckThread();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (poisoned) throw new InvalidOperationException("Backend stop is unconfirmed; ownership cannot be transferred.");
        ArgumentNullException.ThrowIfNull(request);
        if (!NavigationMath.Finite(request.Destination) || !float.IsFinite(request.ArrivalRadius) ||
            request.ArrivalRadius <= 0 || !Enum.IsDefined(request.Mode))
            throw new ArgumentOutOfRangeException(nameof(request));
        if (active is not null) Finish(active, NavigationState.Cancelled, NavigationReason.Superseded);
        if (poisoned) throw new InvalidOperationException("Backend stop is unconfirmed; ownership cannot be transferred.");
        var snapshot = adapter.Read();
        // Never seize a route that was not started by this coordinator.
        if (snapshot.Following) throw new InvalidOperationException("External movement is active.");
        var op = new NavigationOperation(this, request, snapshot.Zone, clock.GetTimestamp());
        active = op;
        op.Last = snapshot;
        Change(op, NavigationState.WaitingForMesh, NavigationReason.Started);
        return op;
    }

    public void Tick()
    {
        CheckThread();
        if (disposed || active is not { } op) return;
        try { Tick(op); }
        catch { Finish(op, NavigationState.Failed, NavigationReason.AdapterFault); }
    }

    public Vector3? ProjectLanding(Vector3 candidate, float searchRadius)
    {
        CheckThread();
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!NavigationMath.Finite(candidate) || !float.IsFinite(searchRadius) || searchRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(candidate));
        var s = adapter.Read();
        if (!Ready(s)) return null;
        var result = adapter.ProjectLanding(candidate, searchRadius);
        return result is { } point && NavigationMath.Finite(point) &&
            NavigationMath.Distance(candidate, point, true) <= searchRadius ? point : null;
    }

    private void Tick(NavigationOperation op)
    {
        var s = adapter.Read();
        op.Last = s;
        if (s.Following && !op.OwnsFollower)
        { Finish(op, NavigationState.Failed, NavigationReason.AdapterFault); return; }
        if (s.Zone != op.Zone) { Finish(op, NavigationState.Cancelled, NavigationReason.ZoneChanged); return; }
        if (!NavigationMath.Finite(s.Position)) { Finish(op, NavigationState.Failed, NavigationReason.AdapterFault); return; }
        if (Elapsed(op.Started) >= options.OperationTimeout)
        { Finish(op, NavigationState.Failed, NavigationReason.BudgetExhausted); return; }
        if (!Ready(s))
        {
            op.ReadySince = null;
            if (op.State != NavigationState.WaitingForMesh)
            {
                InvalidatePath(op);
                StopOwned(op);
                Change(op, NavigationState.WaitingForMesh, NavigationReason.DependencyLost);
            }
            if (Elapsed(op.StateSince) >= options.ReadinessTimeout)
                Finish(op, NavigationState.Failed, NavigationReason.BudgetExhausted);
            return;
        }
        op.ReadySince ??= clock.GetTimestamp();
        if (op.State == NavigationState.WaitingForMesh)
        {
            if (Elapsed(op.StateSince) >= options.ReadinessTimeout)
            { Finish(op, NavigationState.Failed, NavigationReason.BudgetExhausted); return; }
            if (Elapsed(op.ReadySince.Value) < options.ReadinessSettle) return;
            Change(op, NavigationState.Recovering, NavigationReason.DependencyWait);
            op.RetryAt = clock.GetTimestamp();
        }
        if (NavigationMath.Distance(s.Position, op.Request.Destination, op.Request.HorizontalArrival) <= op.Request.ArrivalRadius)
        { Finish(op, NavigationState.Arrived, NavigationReason.DestinationReached); return; }
        if (op.State == NavigationState.Recovering && Elapsed(op.RetryAt) < options.RetryDelay) return;

        // Availability unknown never means permission to choose a ground route.
        var wantsFlight = op.Request.Mode != TravelMode.Ground;
        if (wantsFlight && s.Flight == FlightAvailability.Unknown)
        {
            if (op.State == NavigationState.Pathfinding || op.State == NavigationState.Following)
                Retry(op, NavigationReason.FlightChanged);
            else if (Elapsed(op.StateSince) >= options.StartupTimeout)
                Finish(op, NavigationState.Failed, NavigationReason.BudgetExhausted);
            return;
        }
        if (op.Request.Mode == TravelMode.RequireFlight && s.Flight == FlightAvailability.Unavailable)
        { Finish(op, NavigationState.Failed, NavigationReason.FlightChanged); return; }
        var fly = wantsFlight && s.Flight == FlightAvailability.Available;
        if ((op.State == NavigationState.Pathfinding || op.State == NavigationState.Following) &&
            (op.Fly != fly || (op.Fly && !s.InFlight) || (op.Request.RequireMount && !s.Mounted)))
        { Retry(op, NavigationReason.FlightChanged); return; }

        if ((op.Request.RequireMount || fly) && !s.Mounted)
        {
            Startup(op, NavigationState.Mounting, NavigationReason.MountRequested, options.MountInterval, adapter.RequestMount);
            return;
        }
        if (fly && !s.InFlight)
        {
            Startup(op, NavigationState.TakingOff, NavigationReason.TakeoffRequested, options.TakeoffInterval, adapter.RequestTakeoff);
            return;
        }
        if (op.State is NavigationState.Mounting or NavigationState.TakingOff or NavigationState.Recovering)
        {
            op.Fly = fly;
            op.PathOrigin = s.Position;
            op.PathCancellation = new();
            op.Path = adapter.FindPath(s.Position, op.Request.Destination, fly, op.PathCancellation.Token);
            // Observe abandoned failures, without touching any adapter or operation state.
            _ = op.Path.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            Change(op, NavigationState.Pathfinding, NavigationReason.PathRequested);
            return;
        }
        if (op.State == NavigationState.Pathfinding)
        {
            if (Elapsed(op.StateSince) >= options.PathTimeout) { Retry(op, NavigationReason.PathTimeout); return; }
            if (op.Path is not { IsCompleted: true }) return;
            if (op.Path.IsCanceled || op.Path.IsFaulted) { Retry(op, NavigationReason.PathFailed); return; }
            var path = op.Path.GetAwaiter().GetResult().ToArray();
            if (path.Length < 2 || path.Any(p => !NavigationMath.Finite(p)) ||
                Vector3.Distance(s.Position, op.PathOrigin) > options.MaxStartDrift ||
                Vector3.Distance(s.Position, path[0]) > options.MaxStartDrift ||
                Vector3.Distance(path[^1], op.Request.Destination) > options.EndpointTolerance)
            { Retry(op, NavigationReason.InvalidPath); return; }
            InvalidatePath(op);
            // Mark ownership before submission: if Follow partially succeeds then throws, cleanup must stop it.
            op.OwnsFollower = true;
            adapter.Follow(path, op.Fly);
            op.Waypoint = null;
            op.WaypointCount = int.MaxValue;
            op.ProgressAt = clock.GetTimestamp();
            Change(op, NavigationState.Following, NavigationReason.PathAccepted);
            return;
        }
        if (op.State != NavigationState.Following) return;
        if (!s.Following || s.RemainingWaypoints.Count == 0)
        { Retry(op, NavigationReason.FollowerStopped); return; }
        var waypoint = s.RemainingWaypoints[0];
        if (!NavigationMath.Finite(waypoint)) { Retry(op, NavigationReason.InvalidPath); return; }
        var distance = Vector3.Distance(s.Position, waypoint);
        // A new current waypoint is progress only when the route's remaining count decreases.
        // Sideways movement, oscillation and a changing global destination cannot reset this clock.
        if (op.Waypoint is null || s.RemainingWaypoints.Count < op.WaypointCount)
        {
            op.Waypoint = waypoint;
            op.WaypointCount = s.RemainingWaypoints.Count;
            op.BestWaypointDistance = distance;
            op.ProgressAt = clock.GetTimestamp();
        }
        else if (waypoint == op.Waypoint && distance <= op.BestWaypointDistance - options.ProgressDistance)
        {
            op.BestWaypointDistance = distance;
            op.ProgressAt = clock.GetTimestamp();
        }
        if (Elapsed(op.ProgressAt) >= options.StallTimeout) Retry(op, NavigationReason.Stalled);
    }

    private void Startup(NavigationOperation op, NavigationState state, NavigationReason reason, TimeSpan interval, Action action)
    {
        if (op.State != state)
        {
            Change(op, state, reason);
            op.LastAction = null;
        }
        if (Elapsed(op.StateSince) >= options.StartupTimeout) { Retry(op, NavigationReason.BudgetExhausted); return; }
        if (op.LastAction is null || Elapsed(op.LastAction.Value) >= interval)
        {
            op.LastAction = clock.GetTimestamp();
            action();
        }
    }

    private static bool Ready(NavigationSnapshot s) => !s.Loading && s.Zone.Territory != 0 &&
        s.MeshZone == s.Zone && s.MeshReady && float.IsFinite(s.BuildProgress) && s.BuildProgress < 0;

    private void Retry(NavigationOperation op, NavigationReason reason)
    {
        InvalidatePath(op);
        StopOwned(op);
        if (op.Retries >= options.MaxRetries) { Finish(op, NavigationState.Failed, NavigationReason.BudgetExhausted); return; }
        op.Retries++;
        Change(op, NavigationState.Recovering, reason);
        op.RetryAt = clock.GetTimestamp();
    }

    internal void Cancel(NavigationOperation op)
    {
        CheckThread();
        if (ReferenceEquals(active, op)) Finish(op, NavigationState.Cancelled, NavigationReason.Cancelled);
    }

    private static void InvalidatePath(NavigationOperation op)
    {
        op.Path = null;
        var cancellation = op.PathCancellation;
        op.PathCancellation = null;
        if (cancellation is null) return;
        try { cancellation.Cancel(); } catch (AggregateException) { /* A callback cannot retain ownership. */ }
        finally { cancellation.Dispose(); }
    }

    private void StopOwned(NavigationOperation op)
    {
        if (!op.OwnsFollower) return;
        try { adapter.Stop(); op.OwnsFollower = false; }
        catch { poisoned = true; throw; }
    }

    private void Finish(NavigationOperation op, NavigationState state, NavigationReason reason)
    {
        if (!ReferenceEquals(active, op)) return;
        InvalidatePath(op);
        try { StopOwned(op); }
        catch { state = NavigationState.Failed; reason = NavigationReason.AdapterFault; }
        Change(op, state, reason);
        active = null;
    }

    private void Change(NavigationOperation op, NavigationState state, NavigationReason reason)
    {
        var previous = op.State;
        op.State = state;
        op.StateSince = clock.GetTimestamp();
        diagnostics.Record(op.Id, previous, state, reason, op.Last, op.Result, op.Retries);
    }
    private TimeSpan Elapsed(long timestamp) => clock.GetElapsedTime(timestamp);
    private void CheckThread()
    {
        if (Environment.CurrentManagedThreadId != thread)
            throw new InvalidOperationException("Navigation must be driven on its owning framework thread.");
    }
    public void Dispose()
    {
        CheckThread();
        if (disposed) return;
        if (active is { } op) Finish(op, NavigationState.Cancelled, NavigationReason.Cancelled);
        disposed = true;
    }
}

public sealed class NavigationOperation : IDisposable
{
    private readonly NavigationCoordinator owner;
    internal NavigationOperation(NavigationCoordinator owner, NavigationRequest request, ZoneStamp zone, long started)
    { this.owner = owner; Request = request; Zone = zone; Started = started; }
    public Guid Id { get; } = Guid.NewGuid();
    public NavigationState State { get; internal set; } = NavigationState.Created;
    public int Retries { get; internal set; }
    public NavigationResult Result => State switch
    {
        NavigationState.Arrived => NavigationResult.Success,
        NavigationState.Cancelled => NavigationResult.Cancelled,
        NavigationState.Failed => NavigationResult.Failure,
        _ => NavigationResult.Pending
    };
    public void Cancel() => owner.Cancel(this);
    public void Dispose() => Cancel();
    internal NavigationRequest Request { get; }
    internal ZoneStamp Zone { get; }
    internal long Started { get; }
    internal long StateSince, RetryAt, ProgressAt;
    internal long? ReadySince, LastAction;
    internal bool Fly, OwnsFollower;
    internal Vector3 PathOrigin;
    internal Vector3? Waypoint;
    internal int WaypointCount;
    internal float BestWaypointDistance;
    internal NavigationSnapshot? Last;
    internal Task<IReadOnlyList<Vector3>>? Path;
    internal CancellationTokenSource? PathCancellation;
}
