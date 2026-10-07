using System.Numerics;

namespace SentinelCore.Navigation;

public enum NavigationState { Created, WaitingForMesh, Mounting, TakingOff, Pathfinding, Following, Recovering, Arrived, Cancelled, Failed, Landing }
public enum NavigationReason { Started, DependencyWait, MountRequested, TakeoffRequested, PathRequested, PathAccepted, Progress, DestinationReached, Cancelled, Superseded, ZoneChanged, DependencyLost, FlightChanged, InvalidPath, PathFailed, PathTimeout, Stalled, FollowerStopped, BudgetExhausted, AdapterFault, LandingStarted, LandingRequested, LandingRejected, GroundConfirmed, LandingTimeout, LandingDrift }
public enum FlightAvailability { Unknown, Unavailable, Available }
public enum TravelMode { Ground, PreferFlight, RequireFlight }
public enum NavigationResult { Pending, Success, Cancelled, Failure }
public readonly record struct ZoneStamp(uint Territory, long Epoch);

/// <summary>A coherent framework-thread snapshot. MeshZone must be null unless the adapter has
/// observed current-zone readiness; unknown IPC values must fail closed, never imply ready.</summary>
public sealed record NavigationSnapshot(
    ZoneStamp Zone, ZoneStamp? MeshZone, bool Loading, bool MeshReady, float BuildProgress,
    Vector3 Position, bool Mounted, bool InFlight, FlightAvailability Flight,
    bool Following, IReadOnlyList<Vector3> RemainingWaypoints)
{
    /// <summary>Physical ground evidence supplied by a landing-capable adapter; null means unknown.
    /// Clearing InFlight or accepting an action is insufficient on its own.</summary>
    public bool? Grounded { get; init; }
}

/// <summary>All members except the returned task run synchronously on the coordinator's thread.
/// No adapter may submit paths from task continuations. Mount selection stays consumer-owned.
/// Stop must throw on unconfirmed failure. One coordinator must exclusively own this backend.</summary>
public interface INavigationAdapter
{
    NavigationSnapshot Read();
    Task<IReadOnlyList<Vector3>> FindPath(Vector3 from, Vector3 to, bool fly, CancellationToken cancellation);
    void Follow(IReadOnlyList<Vector3> path, bool fly);
    void Stop();
    void RequestMount();
    void RequestTakeoff();
    Vector3? ProjectLanding(Vector3 candidate, float searchRadius);
}

/// <summary>Optional synchronous capability. Actions must be scoped to this operation, must never
/// schedule delayed cleanup, and must not dismount an already-grounded character. The consumer
/// verifies local landing safety before submitting a normal game action.</summary>
public interface ILandingNavigationAdapter : INavigationAdapter
{
    bool RequestLanding(NavigationSnapshot snapshot, Vector3 destination);
}

public sealed record NavigationRequest(Vector3 Destination, TravelMode Mode = TravelMode.PreferFlight,
    bool RequireMount = true, float ArrivalRadius = 3, bool HorizontalArrival = false)
{
    /// <summary>Opt-in ground-confirmed completion. Existing constructor and default semantics remain intact.</summary>
    public bool RequireLanding { get; init; }
}

public sealed record NavigationOptions
{
    public TimeSpan ReadinessTimeout { get; init; } = TimeSpan.FromSeconds(90);
    public TimeSpan ReadinessSettle { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan StartupTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan PathTimeout { get; init; } = TimeSpan.FromSeconds(60);
    public TimeSpan StallTimeout { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan MountInterval { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan TakeoffInterval { get; init; } = TimeSpan.FromSeconds(0.5);
    public TimeSpan LandingTimeout { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan LandingInterval { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan GroundConfirmation { get; init; } = TimeSpan.FromSeconds(0.75);
    public float LandingVerticalTolerance { get; init; } = 1.5f;
    public int MaxRetries { get; init; } = 2;
    public float ProgressDistance { get; init; } = 0.75f;
    public float MaxStartDrift { get; init; } = 8;
    public float EndpointTolerance { get; init; } = 5;

    internal void Validate()
    {
        foreach (var value in new[] { ReadinessTimeout, OperationTimeout, StartupTimeout, PathTimeout,
                     StallTimeout, MountInterval, TakeoffInterval, LandingTimeout, LandingInterval, GroundConfirmation })
            if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(NavigationOptions));
        if (ReadinessSettle < TimeSpan.Zero || RetryDelay < TimeSpan.Zero || MaxRetries < 0 ||
            !float.IsFinite(ProgressDistance) || ProgressDistance <= 0 ||
            !float.IsFinite(MaxStartDrift) || MaxStartDrift <= 0 ||
            !float.IsFinite(EndpointTolerance) || EndpointTolerance <= 0 ||
            !float.IsFinite(LandingVerticalTolerance) || LandingVerticalTolerance <= 0)
            throw new ArgumentOutOfRangeException(nameof(NavigationOptions));
    }
}

internal static class NavigationMath
{
    public static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    public static float Distance(Vector3 a, Vector3 b, bool horizontal)
        => horizontal ? Vector2.Distance(new(a.X, a.Z), new(b.X, b.Z)) : Vector3.Distance(a, b);
}
