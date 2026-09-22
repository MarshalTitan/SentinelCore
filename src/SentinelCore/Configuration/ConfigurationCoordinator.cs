namespace SentinelCore.Configuration;

public sealed class ConfigurationCoordinator<TConfiguration> : IDisposable
    where TConfiguration : class
{
    private readonly object gate = new();
    private readonly IConfigurationStore<TConfiguration> store;
    private readonly Func<TConfiguration, TConfiguration> normalize;
    private readonly TimeProvider timeProvider;
    private TConfiguration current;
    private DateTimeOffset saveAfter;
    private bool dirty;
    private bool disposed;

    public ConfigurationCoordinator(
        IConfigurationStore<TConfiguration> store,
        Func<TConfiguration> createDefault,
        Func<TConfiguration, TConfiguration> normalize,
        TimeProvider? timeProvider = null,
        bool saveAfterLoad = true)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        ArgumentNullException.ThrowIfNull(createDefault);
        this.normalize = normalize ?? throw new ArgumentNullException(nameof(normalize));
        this.timeProvider = timeProvider ?? TimeProvider.System;

        current = this.normalize(store.Load() ?? createDefault())
            ?? throw new InvalidOperationException("Configuration normalization returned null.");
        if (saveAfterLoad)
            store.Save(current);
    }

    public TConfiguration Current
    {
        get
        {
            lock (gate)
            {
                ThrowIfDisposed();
                return current;
            }
        }
    }

    public TResult Read<TResult>(Func<TConfiguration, TResult> reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        lock (gate)
        {
            ThrowIfDisposed();
            return reader(current);
        }
    }

    public void Update(Action<TConfiguration> mutation, TimeSpan? saveDelay = null)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        lock (gate)
        {
            ThrowIfDisposed();
            mutation(current);
            current = normalize(current)
                ?? throw new InvalidOperationException("Configuration normalization returned null.");

            if (saveDelay is null || saveDelay <= TimeSpan.Zero)
            {
                SaveLocked();
                return;
            }

            dirty = true;
            saveAfter = timeProvider.GetUtcNow() + saveDelay.Value;
        }
    }

    public void MarkDirty(TimeSpan saveDelay)
    {
        lock (gate)
        {
            ThrowIfDisposed();
            dirty = true;
            saveAfter = timeProvider.GetUtcNow() + (saveDelay < TimeSpan.Zero ? TimeSpan.Zero : saveDelay);
        }
    }

    public bool FlushIfDue()
    {
        lock (gate)
        {
            ThrowIfDisposed();
            if (!dirty || timeProvider.GetUtcNow() < saveAfter)
                return false;
            SaveLocked();
            return true;
        }
    }

    public void SaveNow()
    {
        lock (gate)
        {
            ThrowIfDisposed();
            SaveLocked();
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            if (dirty)
                SaveLocked();
            disposed = true;
        }
    }

    private void SaveLocked()
    {
        store.Save(current);
        dirty = false;
        saveAfter = default;
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
}

