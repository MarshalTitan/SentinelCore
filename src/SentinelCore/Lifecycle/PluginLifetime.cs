namespace SentinelCore.Lifecycle;

public sealed class PluginLifetime : IDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly DisposableBag resources;
    private int disposed;

    public PluginLifetime(Action<Exception>? onDisposalError = null)
        => resources = new DisposableBag(onDisposalError);

    public CancellationToken Token => cancellation.Token;

    public T Add<T>(T resource)
        where T : IDisposable
        => resources.Add(resource);

    public IDisposable Add(Action cleanup) => resources.Add(cleanup);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        try
        {
            cancellation.Cancel();
        }
        finally
        {
            resources.Dispose();
            cancellation.Dispose();
        }
    }
}

