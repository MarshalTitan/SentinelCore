namespace SentinelCore.Navigation;

/// <summary>Observed readiness after an adapter-assigned zone epoch. This is not proof of a mesh's
/// territory identity: backends without that API require supervised validation.</summary>
public sealed class ZoneReadinessGate
{
    private readonly TimeProvider clock;
    private readonly TimeSpan settle;
    private ZoneStamp? observed;
    private long? readySince;
    public ZoneReadinessGate(TimeSpan? settle = null, TimeProvider? clock = null)
    {
        this.settle = settle ?? TimeSpan.FromSeconds(2);
        if (this.settle < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(settle));
        this.clock = clock ?? TimeProvider.System;
    }
    public ZoneStamp? Observe(ZoneStamp zone, bool loading, bool meshReady, float buildProgress)
    {
        if (observed != zone) { observed = zone; readySince = null; }
        if (loading || zone.Territory == 0 || !meshReady || !float.IsFinite(buildProgress) || buildProgress >= 0)
        { readySince = null; return null; }
        readySince ??= clock.GetTimestamp();
        return clock.GetElapsedTime(readySince.Value) >= settle ? zone : null;
    }
}
