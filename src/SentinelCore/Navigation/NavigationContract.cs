using System.Numerics;

namespace SentinelCore.Navigation;

public enum NavigationState { Created, WaitingForMesh, Mounting, TakingOff, Pathfinding, Following, Recovering, Arrived, Cancelled, Failed }
public enum NavigationReason { Started, DependencyWait, MountRequested, TakeoffRequested, PathRequested, PathAccepted, Progress, DestinationReached, Cancelled, Superseded, ZoneChanged, DependencyLost, FlightChanged, InvalidPath, PathFailed, PathTimeout, Stalled, FollowerStopped, BudgetExhausted, AdapterFault }
public enum FlightAvailability { Unknown, Unavailable, Available }
public enum TravelMode { Ground, PreferFlight, RequireFlight }
public enum NavigationResult { Pending, Success, Cancelled, Failure }
public readonly record struct ZoneStamp(uint Territory, long Epoch);

/// <summary>A coherent framework-thread snapshot. MeshZone must be null unless the adapter has
/// observed current-zone readiness; unknown IPC values must fail closed, never imply ready.</summary>
public sealed record NavigationSnapshot(
    ZoneStamp Zone, ZoneStamp? MeshZone, bool Loading, bool MeshReady, float BuildProgress,
    Vector3 Position, bool Mounted, bool InFlight, FlightAvailability Flight,
    bool Following, IReadOnlyList<Vector3> RemainingWaypoints);

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

public sealed record NavigationRequest(Vector3 Destination, TravelMode Mode = TravelMode.PreferFlight,
    bool RequireMount = true, float ArrivalRadius = 3, bool HorizontalArrival = false);

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
    public int MaxRetries { get; init; } = 2;
    public float ProgressDistance { get; init; } = 0.75f;
    public float MaxStartDrift { get; init; } = 8;
    public float EndpointTolerance { get; init; } = 5;

    internal void Validate()
    {
        foreach (var value in new[] { ReadinessTimeout, OperationTimeout, StartupTimeout, PathTimeout,
                     StallTimeout, MountInterval, TakeoffInterval })
            if (value <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(NavigationOptions));
        if (ReadinessSettle < TimeSpan.Zero || RetryDelay < TimeSpan.Zero || MaxRetries < 0 ||
            !float.IsFinite(ProgressDistance) || ProgressDistance <= 0 ||
            !float.IsFinite(MaxStartDrift) || MaxStartDrift <= 0 ||
            !float.IsFinite(EndpointTolerance) || EndpointTolerance <= 0)
            throw new ArgumentOutOfRangeException(nameof(NavigationOptions));
    }
}

internal static class NavigationMath
{
    public static bool Finite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);
    public static float Distance(Vector3 a, Vector3 b, bool horizontal)
        => horizontal ? Vector2.Distance(new(a.X, a.Z), new(b.X, b.Z)) : Vector3.Distance(a, b);
}
