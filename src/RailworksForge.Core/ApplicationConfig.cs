using System.Text.Json;
using System.Text.Json.Serialization;

namespace RailworksForge.Core;

public class ApplicationConfig
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        AllowTrailingCommas = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly ConfigOptions DefaultConfigOptions = new ()
    {
        GameDirectoryPath = string.Empty,
    };

    public static ConfigOptions Get()
    {
        if (GetConfigFromPath("settings", DefaultConfigOptions) is not {} options)
        {
            throw new Exception("Failed to read configuration settings.");
        }

        return options;
    }

    private static TConfig? GetConfigFromPath<TConfig>(string filename, TConfig defaultValue)
    {
        try
        {
            var path = Path.Join(Paths.GetConfigurationFolder(), $"{filename}.json");

            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                return JsonSerializer.Deserialize<TConfig>(content, JsonSerializerOptions);
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
}
