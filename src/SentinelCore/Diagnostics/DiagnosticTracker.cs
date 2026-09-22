namespace SentinelCore.Diagnostics;

public sealed class DiagnosticTracker(TimeProvider? timeProvider = null)
{
    private readonly object gate = new();
    private readonly Dictionary<string, State> states = new(StringComparer.Ordinal);
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    public bool Changed(string key, string signature)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(signature);
        var now = timeProvider.GetUtcNow();
        lock (gate)
        {
            if (states.TryGetValue(key, out var previous)
                && string.Equals(previous.Signature, signature, StringComparison.Ordinal))
            {
                return false;
            }
            states[key] = new State(signature, now);
            return true;
        }
    }

    public bool Throttled(string key, TimeSpan minimumInterval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (minimumInterval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(minimumInterval));

        var now = timeProvider.GetUtcNow();
        lock (gate)
        {
            if (states.TryGetValue(key, out var previous)
                && now - previous.LoggedAt < minimumInterval)
            {
                return false;
            }
            states[key] = new State(previous?.Signature ?? string.Empty, now);
            return true;
        }
    }

    public void Reset()
    {
        lock (gate)
            states.Clear();
    }

    private sealed record State(string Signature, DateTimeOffset LoggedAt);
}

