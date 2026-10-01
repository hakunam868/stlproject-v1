using System.Text.Json;

namespace ParseStl.Configuration;

/// <summary>Reads <see cref="AppSettings"/> from a JSON file (comments and trailing commas allowed).</summary>
public sealed class JsonAppSettingsProvider(string configPath) : IAppSettingsProvider
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public string ConfigPath { get; } = configPath;

    public AppSettings Load()
    {
        if (!File.Exists(ConfigPath))
            throw new ConfigurationException($"Configuration file not found: {ConfigPath}");

        AppSettings? settings;
        try
        {
            using var stream = File.OpenRead(ConfigPath);
            settings = JsonSerializer.Deserialize<AppSettings>(stream, Options);
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Config.json is not valid JSON: {ex.Message}", ex);
        }

        if (settings is null)
            throw new ConfigurationException("Config.json is empty.");

        var errors = settings.Validate();
        if (errors.Count > 0)
            throw new ConfigurationException("Config.json has invalid values:" + Environment.NewLine + "  - " +
                                             string.Join(Environment.NewLine + "  - ", errors));

        // Relative folders are relative to the exe, not the current working directory.
        var configDirectory = Path.GetDirectoryName(Path.GetFullPath(ConfigPath))!;
        settings.StlFolder = Path.GetFullPath(settings.StlFolder, configDirectory);
        return settings;
    }
}
