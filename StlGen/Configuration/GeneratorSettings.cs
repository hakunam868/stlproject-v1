using System.Text.Json;
using System.Text.Json.Serialization;
using StlGen.Output;

namespace StlGen.Configuration;

public sealed class GeneratorSettings
{
    public string OutputFolder { get; set; } = "StlFiles";

    public StlOutputFormat[] DefaultFormats { get; set; } = [StlOutputFormat.Binary];

    public long MaxTriangles { get; set; } = 20_000_000;

    public SuiteSettings Suite { get; set; } = new();

    public IEnumerable<string> Validate()
    {
        if (string.IsNullOrWhiteSpace(OutputFolder))
            yield return "OutputFolder must not be empty.";
        if (DefaultFormats is not { Length: > 0 })
            yield return "DefaultFormats must list at least one format.";
        if (MaxTriangles <= 0)
            yield return "MaxTriangles must be greater than 0.";
        if (Suite?.Densities is not { Length: > 0 } || Suite.Densities.Any(d => d <= 0 || !double.IsFinite(d)))
            yield return "Suite.Densities must list one or more positive numbers.";
        if (Suite?.Formats is not { Length: > 0 })
            yield return "Suite.Formats must list at least one format.";
    }
}

public sealed class SuiteSettings
{
    public double[] Densities { get; set; } = [1, 4, 16];

    public StlOutputFormat[] Formats { get; set; } = [StlOutputFormat.Binary];
}

public interface IGeneratorSettingsProvider
{
    GeneratorSettings Load();
}

public sealed class ConfigurationException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class JsonGeneratorSettingsProvider(string configPath) : IGeneratorSettingsProvider
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public GeneratorSettings Load()
    {
        if (!File.Exists(configPath))
            throw new ConfigurationException($"Configuration file not found: {configPath}");

        GeneratorSettings? settings;
        try
        {
            using var stream = File.OpenRead(configPath);
            settings = JsonSerializer.Deserialize<GeneratorSettings>(stream, Options);
        }
        catch (JsonException ex)
        {
            throw new ConfigurationException($"Config.json is not valid: {ex.Message}", ex);
        }

        if (settings is null)
            throw new ConfigurationException("Config.json is empty.");

        var errors = settings.Validate().ToList();
        if (errors.Count > 0)
            throw new ConfigurationException("Config.json has invalid values:" + Environment.NewLine + "  - " +
                                             string.Join(Environment.NewLine + "  - ", errors));

        var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath))!;
        settings.OutputFolder = Path.GetFullPath(settings.OutputFolder, configDirectory);
        return settings;
    }
}
