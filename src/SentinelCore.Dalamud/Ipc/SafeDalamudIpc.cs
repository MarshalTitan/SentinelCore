using SentinelCore.Diagnostics;
using SentinelCore.Ipc;

namespace SentinelCore.Dalamud.Ipc;

public static class SafeDalamudIpc
{
    public static IpcResult<T> Invoke<T>(
        string endpoint,
        Func<T> invocation,
        ISentinelLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(invocation);

        try
        {
            return IpcResult<T>.Success(invocation());
        }
        catch (Exception exception)
        {
            logger?.Warning($"Optional IPC endpoint '{endpoint}' was unavailable or failed.", exception);
            return new IpcResult<T>(IpcStatus.Unavailable, default, exception.GetType().Name);
        }
    }

    public static IpcResult Invoke(
        string endpoint,
        Action invocation,
        ISentinelLogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(invocation);

        try
        {
            invocation();
            return IpcResult.Success();
        }
        catch (Exception exception)
        {
            logger?.Warning($"Optional IPC endpoint '{endpoint}' was unavailable or failed.", exception);
            return new IpcResult(IpcStatus.Unavailable, exception.GetType().Name);
        }
    }
}

