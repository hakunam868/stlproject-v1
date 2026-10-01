namespace StlGen.Shapes;

/// <summary>How the global density multiplier affects a parameter.</summary>
public enum DensityScaling
{
    /// <summary>Not affected (dimensions, flags, prism side count).</summary>
    None,

    /// <summary>Multiplied by the density (segment, stack and division counts).</summary>
    Linear,

    /// <summary>Increased by log2(density) (recursive subdivision levels, where +1 means 4× the triangles).</summary>
    Log2,
}

public sealed record ShapeParameter(
    string Name,
    string Description,
    double Default,
    double Min,
    double Max,
    bool IsInteger = false,
    DensityScaling Scaling = DensityScaling.None)
{
    public static ShapeParameter Length(string name, string description, double @default) =>
        new(name, description, @default, 1e-6, 1e9);

    public static ShapeParameter Resolution(string name, string description, int @default, int min, int max = 1_000_000) =>
        new(name, description, @default, min, max, IsInteger: true, Scaling: DensityScaling.Linear);

    public double ApplyDensity(double value, double density) => Scaling switch
    {
        DensityScaling.Linear => Math.Clamp(Math.Round(value * density), Min, Max),
        DensityScaling.Log2 => Math.Clamp(value + Math.Round(Math.Log2(density)), Min, Max),
        _ => value,
    };

    public string? Check(double value)
    {
        if (!double.IsFinite(value))
            return $"{Name} must be a finite number.";
        if (IsInteger && value != Math.Floor(value))
            return $"{Name} must be a whole number.";
        if (value < Min || value > Max)
            return $"{Name} must be between {Min:G} and {Max:G} (was {value:G}).";
        return null;
    }
}

/// <summary>Resolved parameter values for one generation, looked up by name (case-insensitive).</summary>
public sealed class ParameterSet(IEnumerable<KeyValuePair<string, double>> values)
{
    private readonly Dictionary<string, double> _values = new(values, StringComparer.OrdinalIgnoreCase);

    public double this[string name] => _values.TryGetValue(name, out var v)
        ? v
        : throw new KeyNotFoundException($"Unknown parameter '{name}'.");

    public int Int(string name) => (int)this[name];

    public IReadOnlyDictionary<string, double> Values => _values;
}
