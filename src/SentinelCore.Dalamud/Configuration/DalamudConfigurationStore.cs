using Dalamud.Configuration;
using Dalamud.Plugin;
using SentinelCore.Configuration;

namespace SentinelCore.Dalamud.Configuration;

public sealed class DalamudConfigurationStore<TConfiguration>(IDalamudPluginInterface pluginInterface)
    : IConfigurationStore<TConfiguration>
    where TConfiguration : class, IPluginConfiguration
{
    private readonly IDalamudPluginInterface pluginInterface = pluginInterface
        ?? throw new ArgumentNullException(nameof(pluginInterface));

    public TConfiguration? Load() => pluginInterface.GetPluginConfig() as TConfiguration;

    public void Save(TConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        pluginInterface.SavePluginConfig(configuration);
    }
}

