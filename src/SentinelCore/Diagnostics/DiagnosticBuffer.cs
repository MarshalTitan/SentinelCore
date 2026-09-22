namespace SentinelCore.Diagnostics;

public sealed record DiagnosticEntry(
    DateTimeOffset Timestamp,
    SentinelLogLevel Level,
    string Message,
    string? ExceptionType = null,
    string? ExceptionMessage = null);

public sealed class DiagnosticBuffer : ISentinelLogger
{
    private readonly object gate = new();
    private readonly Queue<DiagnosticEntry> entries;
    private readonly TimeProvider timeProvider;

    public DiagnosticBuffer(int capacity = 200, TimeProvider? timeProvider = null)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        entries = new Queue<DiagnosticEntry>(capacity);
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public int Capacity { get; }

    public void Log(SentinelLogLevel level, string message, Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        lock (gate)
        {
            while (entries.Count >= Capacity)
                entries.Dequeue();
            entries.Enqueue(new DiagnosticEntry(
                timeProvider.GetUtcNow(),
                level,
                message,
                exception?.GetType().FullName,
                exception?.Message));
        }
    }

    public IReadOnlyList<DiagnosticEntry> Snapshot()
    {
        lock (gate)
            return entries.ToArray();
    }

    public void Clear()
    {
        lock (gate)
            entries.Clear();
    }
}

