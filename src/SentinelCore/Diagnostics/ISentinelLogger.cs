namespace SentinelCore.Diagnostics;

public enum SentinelLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
}

public interface ISentinelLogger
{
    void Log(SentinelLogLevel level, string message, Exception? exception = null);
}

public static class SentinelLoggerExtensions
{
    public static void Debug(this ISentinelLogger logger, string message)
        => logger.Log(SentinelLogLevel.Debug, message);

    public static void Information(this ISentinelLogger logger, string message)
        => logger.Log(SentinelLogLevel.Information, message);

    public static void Warning(this ISentinelLogger logger, string message, Exception? exception = null)
        => logger.Log(SentinelLogLevel.Warning, message, exception);

    public static void Error(this ISentinelLogger logger, string message, Exception? exception = null)
        => logger.Log(SentinelLogLevel.Error, message, exception);
}

