using System.Diagnostics;
using System.Globalization;
using StlGen.Configuration;
using StlGen.Geometry;
using StlGen.Output;
using StlGen.Shapes;

namespace StlGen.Services;

/// <param name="Shape">Shape to build.</param>
/// <param name="Overrides">Parameter values that replace the defaults; missing parameters use their default.</param>
/// <param name="Density">Multiplier applied to resolution parameters after the overrides (1 = unchanged).</param>
/// <param name="Formats">Output encodings; one file is written per format.</param>
/// <param name="OutputFolder">Folder the files are written to (created if needed).</param>
/// <param name="BaseName">File name without extension; derived from the shape and its resolution when null.</param>
public sealed record GenerationRequest(
    IShapeGenerator Shape,
    IReadOnlyDictionary<string, double> Overrides,
    double Density,
    IReadOnlyList<StlOutputFormat> Formats,
    string OutputFolder,
    string? BaseName = null);

public sealed record WrittenFile(string Path, StlOutputFormat Format, long SizeBytes);

public sealed record GenerationResult(
    IShapeGenerator Shape,
    ParameterSet Parameters,
    int TriangleCount,
    int VertexCount,
    IReadOnlyList<WrittenFile> Files,
    TimeSpan BuildTime,
    TimeSpan WriteTime);

public interface IGenerationService
{
    /// <summary>Applies the defaults, overrides and density, then validates.</summary>
    /// <exception cref="GenerationException">A value is unknown or invalid.</exception>
    ParameterSet Resolve(IShapeGenerator shape, IReadOnlyDictionary<string, double> overrides, double density);

    MeshData Build(IShapeGenerator shape, ParameterSet parameters);

    GenerationResult Generate(GenerationRequest request);
}

public sealed class GenerationService(IEnumerable<IStlWriter> writers, GeneratorSettings settings) : IGenerationService
{
    private readonly IReadOnlyDictionary<StlOutputFormat, IStlWriter> _writers = writers.ToDictionary(w => w.Format);

    public ParameterSet Resolve(IShapeGenerator shape, IReadOnlyDictionary<string, double> overrides, double density)
    {
        if (!(density > 0) || !double.IsFinite(density))
            throw new GenerationException("Density must be a positive number.");

        var errors = new List<string>();
        var known = shape.Parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in overrides.Keys.Where(k => !known.Contains(k)))
            errors.Add($"'{name}' is not a parameter of {shape.Name}. Valid parameters: {string.Join(", ", known)}.");

        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var lookup = new Dictionary<string, double>(overrides, StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in shape.Parameters)
        {
            var value = lookup.TryGetValue(parameter.Name, out var v) ? v : parameter.Default;
            if (parameter.Check(value) is { } error)
            {
                errors.Add(error);
                continue;
            }

            values[parameter.Name] = parameter.ApplyDensity(value, density);
        }

        if (errors.Count == 0)
            errors.AddRange(shape.Validate(new ParameterSet(values)));

        if (errors.Count > 0)
            throw new GenerationException(string.Join(Environment.NewLine, errors));

        return new ParameterSet(values);
    }

    public MeshData Build(IShapeGenerator shape, ParameterSet parameters)
    {
        var builder = new MeshBuilder(settings.MaxTriangles);
        shape.Build(parameters, builder);
        return builder.Build();
    }

    public GenerationResult Generate(GenerationRequest request)
    {
        if (request.Formats.Count == 0)
            throw new GenerationException("At least one output format is required.");

        var parameters = Resolve(request.Shape, request.Overrides, request.Density);

        var stopwatch = Stopwatch.StartNew();
        var mesh = Build(request.Shape, parameters);
        var buildTime = stopwatch.Elapsed;

        stopwatch.Restart();
        Directory.CreateDirectory(request.OutputFolder);
        var baseName = request.BaseName ?? DefaultBaseName(request.Shape, parameters);
        var solidName = $"{request.Shape.Name} {Describe(parameters)}";

        var files = new List<WrittenFile>();
        foreach (var format in request.Formats.Distinct())
        {
            var path = Path.Combine(request.OutputFolder, $"{baseName}_{format.ToString().ToLowerInvariant()}.stl");
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
                _writers[format].Write(mesh, solidName, stream);
            files.Add(new WrittenFile(path, format, new FileInfo(path).Length));
        }

        return new GenerationResult(request.Shape, parameters, mesh.TriangleCount, mesh.VertexCount, files, buildTime, stopwatch.Elapsed);
    }

    /// <summary>For example "cylinder_segments64_stacks4_capRings1": the shape plus its resolution parameters.</summary>
    public static string DefaultBaseName(IShapeGenerator shape, ParameterSet parameters)
    {
        var resolution = shape.Parameters
            .Where(p => p.IsInteger)
            .Select(p => string.Create(CultureInfo.InvariantCulture, $"{p.Name}{parameters[p.Name]}"));
        return string.Join("_", resolution.Prepend(shape.Name));
    }

    public static string Describe(ParameterSet parameters) =>
        string.Join(" ", parameters.Values.Select(kv => string.Create(CultureInfo.InvariantCulture, $"{kv.Key}={kv.Value:G6}")));
}
