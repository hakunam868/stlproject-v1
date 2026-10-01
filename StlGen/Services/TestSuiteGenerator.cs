using System.Globalization;
using StlGen.Configuration;
using StlGen.Geometry;
using StlGen.Output;
using StlGen.Shapes;

namespace StlGen.Services;

public sealed record SuiteEntry(IShapeGenerator Shape, double Density, GenerationResult? Result, string? Error);

public interface ITestSuiteGenerator
{
    /// <summary>Generates every shape with default parameters at every density. A failure skips that entry and the rest still run.</summary>
    IReadOnlyList<SuiteEntry> Generate(IReadOnlyList<double> densities, IReadOnlyList<StlOutputFormat> formats, string outputFolder,
                                       Action<SuiteEntry>? onProgress = null);
}

public sealed class TestSuiteGenerator(IReadOnlyList<IShapeGenerator> shapes, IGenerationService service) : ITestSuiteGenerator
{
    private static readonly IReadOnlyDictionary<string, double> NoOverrides = new Dictionary<string, double>();

    public IReadOnlyList<SuiteEntry> Generate(IReadOnlyList<double> densities, IReadOnlyList<StlOutputFormat> formats,
                                              string outputFolder, Action<SuiteEntry>? onProgress = null)
    {
        var entries = new List<SuiteEntry>();
        foreach (var shape in shapes)
        foreach (var density in densities)
        {
            var baseName = string.Create(CultureInfo.InvariantCulture, $"suite_{shape.Name}_d{density:G4}");
            SuiteEntry entry;
            try
            {
                var result = service.Generate(new GenerationRequest(shape, NoOverrides, density, formats, outputFolder, baseName));
                entry = new SuiteEntry(shape, density, result, null);
            }
            catch (Exception ex) when (ex is GenerationException or IOException or UnauthorizedAccessException or OutOfMemoryException)
            {
                entry = new SuiteEntry(shape, density, null, ex.Message);
            }

            entries.Add(entry);
            onProgress?.Invoke(entry);
        }

        return entries;
    }
}
