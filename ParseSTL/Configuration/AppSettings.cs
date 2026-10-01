namespace ParseStl.Configuration;

/// <summary>Root of Config.json.</summary>
public sealed class AppSettings
{
    public string StlFolder { get; set; } = "StlFiles";

    public WeldingSettings Welding { get; set; } = new();

    public QualitySettings Quality { get; set; } = new();

    /// <summary>Returns a human-readable message for every invalid value; empty when the settings are usable.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(StlFolder))
            errors.Add("StlFolder must not be empty.");

        if (Welding is null)
            errors.Add("Welding section is missing.");
        else if (Welding.Tolerance < 0 || !double.IsFinite(Welding.Tolerance))
            errors.Add("Welding.Tolerance must be a finite number >= 0.");

        if (Quality is null)
            errors.Add("Quality section is missing.");
        else
            errors.AddRange(Quality.Validate());

        return errors;
    }
}
