using System.Text.RegularExpressions;

namespace SentinelCore.Ipc;

public static partial class SentinelIpcContract
{
    public static string Endpoint(string pluginInternalName, string operation, int contractMajor = 1)
    {
        ValidateSegment(pluginInternalName, nameof(pluginInternalName));
        ValidateSegment(operation, nameof(operation));
        if (contractMajor <= 0)
            throw new ArgumentOutOfRangeException(nameof(contractMajor));
        return $"Sentinel.{pluginInternalName}.{operation}.v{contractMajor}";
    }

    private static void ValidateSegment(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || !SegmentPattern().IsMatch(value))
            throw new ArgumentException("IPC segments must contain only letters, digits, underscores, or hyphens.", parameterName);
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SegmentPattern();
}

public enum IpcStatus
{
    Success,
    Unavailable,
    Unsupported,
    Rejected,
    Failed,
}

public readonly record struct IpcResult(IpcStatus Status, string? Detail = null)
{
    public bool Succeeded => Status == IpcStatus.Success;
    public static IpcResult Success() => new(IpcStatus.Success);
}

public readonly record struct IpcResult<T>(IpcStatus Status, T? Value = default, string? Detail = null)
{
    public bool Succeeded => Status == IpcStatus.Success;
    public static IpcResult<T> Success(T value) => new(IpcStatus.Success, value);
}

