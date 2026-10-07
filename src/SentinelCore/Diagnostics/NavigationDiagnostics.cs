using System.Text.Json;
using System.Text.Json.Serialization;
using SentinelCore.Identity;
using SentinelCore.Navigation;

namespace SentinelCore.Diagnostics;

/// <summary>Allowlisted telemetry only: no arbitrary message, exception, chat, account or config fields.
/// Physical context intentionally excludes coordinates, character/world names and actor IDs.</summary>
public sealed record NavigationDiagnostic(
    DateTimeOffset Timestamp, string Plugin, string Version, Guid OperationId,
    NavigationState PreviousState, NavigationState NewState, NavigationReason Reason,
    NavigationDependency Dependency, NavigationPhysicalContext PhysicalContext,
    NavigationResult Result, int Retry);
public sealed record NavigationDependency(bool MeshReady, bool Building, bool CurrentZone, bool Following);
public sealed record NavigationPhysicalContext(uint Territory, long ZoneEpoch, bool Loading, bool Mounted,
    bool InFlight, FlightAvailability Flight)
{
    public bool? Grounded { get; init; }
}

public sealed class NavigationDiagnostics
{
    private readonly object gate = new();
    private readonly Queue<NavigationDiagnostic> entries = new();
    private readonly SentinelIdentity identity;
    private readonly TimeProvider clock;
    public int Capacity { get; }

    public NavigationDiagnostics(SentinelIdentity identity, int capacity = 512, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.identity = identity;
        this.clock = clock ?? TimeProvider.System;
        Capacity = capacity;
    }

    internal void Record(Guid id, NavigationState previous, NavigationState next, NavigationReason reason,
        NavigationSnapshot? s, NavigationResult result, int retry)
    {
        var entry = new NavigationDiagnostic(clock.GetUtcNow(), identity.InternalName, identity.Version.ToString(),
            id, previous, next, reason,
            new(s?.MeshReady == true, s is not null && float.IsFinite(s.BuildProgress) && s.BuildProgress >= 0,
                s is not null && s.MeshZone == s.Zone, s?.Following == true),
            new(s?.Zone.Territory ?? 0, s?.Zone.Epoch ?? 0, s?.Loading ?? true, s?.Mounted ?? false,
                s?.InFlight ?? false, s?.Flight ?? FlightAvailability.Unknown) { Grounded = s?.Grounded }, result, retry);
        lock (gate)
        {
            while (entries.Count >= Capacity) entries.Dequeue();
            entries.Enqueue(entry);
        }
    }

    public IReadOnlyList<NavigationDiagnostic> Snapshot() { lock (gate) return entries.ToArray(); }
    public string ExportJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Serialize(new { Schema = "sentinel.navigation.v1", Entries = Snapshot() }, options);
    }
}
