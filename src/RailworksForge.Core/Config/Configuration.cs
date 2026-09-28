using System.Text.Json;
using System.Text.Json.Serialization;

namespace RailworksForge.Core.Config;

public class Configuration
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = SourceGenerationContext.Default,
    };

    private static readonly Lock SettingsLock = new();
    private static ConfigurationOptions? _settings;

    // Kept in memory: settings are read on hot paths such as every Serz conversion, and only this app writes them.
    public static ConfigurationOptions Get()
    {
        lock (SettingsLock)
        {
            if (_settings is not null)
            {
                return _settings;
            }

            if (GetConfigFromPath("settings", new ConfigurationOptions()) is not {} options)
            {
                throw new Exception("Failed to read configuration settings.");
            }

            _settings = options;

            return options;
        }
    }

    public static void Set(ConfigurationOptions options)
    {
        lock (SettingsLock)
        {
            SaveConfig("settings", options);
            _settings = options;
        }
    }

    public static TConfig GetConfigFromPath<TConfig>(string filename, TConfig defaultValue)
    {
        try
        {
            var path = Path.Join(Paths.GetConfigurationFolder(), $"{filename}.json");

            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                var deserialized = JsonSerializer.Deserialize<TConfig>(content, JsonSerializerOptions);

                return deserialized ?? defaultValue;
            }

            var json = JsonSerializer.Serialize(defaultValue, JsonSerializerOptions);

            if (Path.GetDirectoryName(path) is not {} directory)
            {
                throw new Exception($"failed to create config file {filename}");
            }

            Directory.CreateDirectory(directory);
            File.WriteAllText(path, json);

            return defaultValue;
        }
        catch (Exception ex)
        {
            throw new Exception($"unable to read config for file {filename}", ex);
        }
    }

    public static void SaveConfig<TValue>(string filename, TValue value)
    {
        try
        {
            var path = Path.Join(Paths.GetConfigurationFolder(), $"{filename}.json");
            var json = JsonSerializer.Serialize(value, JsonSerializerOptions);

            if (Path.GetDirectoryName(path) is not {} directory)
            {
                throw new Exception($"failed to create config file {filename}");
            }

            Directory.CreateDirectory(directory);
            WriteAtomically(path, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"unable to save config for file {filename}", ex);
        }
    }

    private static void WriteAtomically(string path, string content)
    {
        var stagingPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllText(stagingPath, content);
            File.Move(stagingPath, path, overwrite: true);
        }
        finally
        {
            File.Delete(stagingPath);
        }
    }
}
