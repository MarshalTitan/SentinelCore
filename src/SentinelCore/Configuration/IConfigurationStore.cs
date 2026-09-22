namespace SentinelCore.Configuration;

public interface IConfigurationStore<TConfiguration>
    where TConfiguration : class
{
    TConfiguration? Load();
    void Save(TConfiguration configuration);
}

