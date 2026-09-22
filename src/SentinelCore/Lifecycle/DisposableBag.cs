namespace SentinelCore.Lifecycle;

public sealed class DisposableBag(Action<Exception>? onError = null) : IDisposable
{
    private readonly object gate = new();
    private readonly Stack<IDisposable> resources = new();
    private readonly Action<Exception>? onError = onError;
    private bool disposed;

    public T Add<T>(T resource)
        where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(resource);
        lock (gate)
        {
            if (!disposed)
            {
                resources.Push(resource);
                return resource;
            }
        }

        DisposeOne(resource);
        return resource;
    }

    public IDisposable Add(Action cleanup)
        => Add(new ActionDisposable(cleanup));

    public void Dispose()
    {
        IDisposable[] snapshot;
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
            snapshot = resources.ToArray();
            resources.Clear();
        }

        foreach (var resource in snapshot)
            DisposeOne(resource);
    }

    private void DisposeOne(IDisposable resource)
    {
        try
        {
            resource.Dispose();
        }
        catch (Exception exception)
        {
            onError?.Invoke(exception);
        }
    }

    private sealed class ActionDisposable(Action cleanup) : IDisposable
    {
        private Action? cleanup = cleanup ?? throw new ArgumentNullException(nameof(cleanup));

        public void Dispose() => Interlocked.Exchange(ref cleanup, null)?.Invoke();
    }
}

