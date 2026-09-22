using Dalamud.Plugin.Services;
using SentinelCore.Diagnostics;

namespace SentinelCore.Dalamud.Diagnostics;

public sealed class DalamudLoggerAdapter(IPluginLog pluginLog) : ISentinelLogger
{
    private readonly IPluginLog pluginLog = pluginLog ?? throw new ArgumentNullException(nameof(pluginLog));

    public void Log(SentinelLogLevel level, string message, Exception? exception = null)
    {
        var rendered = exception is null
            ? message
            : $"{message}{Environment.NewLine}{exception}";

        switch (level)
        {
            case SentinelLogLevel.Trace:
                pluginLog.Verbose(rendered);
                break;
            case SentinelLogLevel.Debug:
                pluginLog.Debug(rendered);
                break;
            case SentinelLogLevel.Information:
                pluginLog.Information(rendered);
                break;
            case SentinelLogLevel.Warning:
                pluginLog.Warning(rendered);
                break;
            case SentinelLogLevel.Error:
            case SentinelLogLevel.Critical:
                pluginLog.Error(rendered);
                break;
            default:
                pluginLog.Debug(rendered);
                break;
        }
    }
}

