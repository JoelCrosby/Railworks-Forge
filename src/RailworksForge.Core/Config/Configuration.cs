using System.Text.Json;
using System.Text.Json.Serialization;

namespace RailworksForge.Core.Config;

public class Configuration
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly ConfigurationOptions DefaultConfigurationOptions = new ()
    {
        GameDirectoryPath = string.Empty,
    };

    public static ConfigurationOptions Get()
    {
        if (GetConfigFromPath("settings", DefaultConfigurationOptions) is not {} options)
        {
            throw new Exception("Failed to read configuration settings.");
        }

        return options;
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
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            throw new Exception($"unable to read config for file {filename}", ex);
        }
    }
}
