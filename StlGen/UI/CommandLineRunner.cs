using System.Globalization;
using StlGen.Configuration;
using StlGen.Geometry;
using StlGen.Output;
using StlGen.Services;
using StlGen.Shapes;

namespace StlGen.UI;

/// <summary>Command-line mode for scripted or batch generation. Exit codes: 0 success, 1 generation failed, 2 usage error.</summary>
public sealed class CommandLineRunner(
    IReadOnlyList<IShapeGenerator> shapes,
    IGenerationService service,
    ITestSuiteGenerator suite,
    IResultPrinter printer,
    GeneratorSettings settings,
    IConsole console)
{
    private const int Success = 0, Failed = 1, UsageError = 2;

    public int Run(string[] args)
    {
        var command = args[0].ToLowerInvariant();
        if (command is "help" or "-h" or "--help" or "/?")
        {
            PrintUsage();
            return Success;
        }

        if (command == "list")
        {
            PrintShapes();
            return Success;
        }

        if (!TryParseOptions(args.AsSpan(1), out var options, out var error))
            return Usage(error);

        try
        {
            var formats = options.Remove("format", out var f) ? ParseFormats(f) : null;
            var folder = options.Remove("out", out var o) ? Path.GetFullPath(o) : settings.OutputFolder;

            if (command == "suite")
                return RunSuite(options, formats ?? settings.Suite.Formats, folder);

            var shape = shapes.FirstOrDefault(s => s.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
            if (shape is null)
                return Usage($"Unknown command or shape '{args[0]}'.");

            var density = options.Remove("density", out var d) ? ParseNumber("density", d) : 1;
            options.Remove("name", out var baseName);
            var overrides = options.ToDictionary(kv => kv.Key, kv => ParseNumber(kv.Key, kv.Value), StringComparer.OrdinalIgnoreCase);

            printer.Print(service.Generate(new GenerationRequest(shape, overrides, density, formats ?? settings.DefaultFormats, folder, baseName)));
            return Success;
        }
        catch (FormatException ex)
        {
            return Usage(ex.Message);
        }
        catch (Exception ex) when (ex is GenerationException or IOException or UnauthorizedAccessException or OutOfMemoryException)
        {
            console.WriteLine($"Not generated: {ex.Message}", ConsoleColor.Red);
            return Failed;
        }
    }

    private int RunSuite(Dictionary<string, string> options, IReadOnlyList<StlOutputFormat> formats, string folder)
    {
        var densities = options.Remove("densities", out var list)
            ? list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => ParseNumber("densities", x)).ToArray()
            : settings.Suite.Densities;
        if (options.Count > 0)
            return Usage($"Unknown suite option(s): {string.Join(", ", options.Keys.Select(k => "--" + k))}.");

        console.WriteLine($"Generating suite into {folder} ...");
        var entries = suite.Generate(densities, formats, folder, printer.PrintSuiteRow);
        printer.PrintSuite(entries);
        return entries.All(e => e.Result is not null) ? Success : Failed;
    }

    private static bool TryParseOptions(ReadOnlySpan<string> args, out Dictionary<string, string> options, out string error)
    {
        options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        error = "";
        for (var i = 0; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || args[i].Length < 3)
            {
                error = $"Expected an option such as --radius, but found '{args[i]}'.";
                return false;
            }

            if (i + 1 >= args.Length)
            {
                error = $"Option '{args[i]}' needs a value.";
                return false;
            }

            options[args[i][2..]] = args[i + 1];
        }

        return true;
    }

    private static double ParseNumber(string name, string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new FormatException($"--{name} expects a number but got '{text}'.");

    private static StlOutputFormat[] ParseFormats(string text) => text.ToLowerInvariant() switch
    {
        "binary" or "bin" => [StlOutputFormat.Binary],
        "ascii" or "text" => [StlOutputFormat.Ascii],
        "both" => [StlOutputFormat.Binary, StlOutputFormat.Ascii],
        _ => throw new FormatException($"--format must be binary, ascii or both (got '{text}')."),
    };

    private int Usage(string error)
    {
        console.WriteLine(error, ConsoleColor.Red);
        console.WriteLine();
        PrintUsage();
        return UsageError;
    }

    private void PrintUsage()
    {
        console.WriteLine("""
            Usage:
              StlGen                                   interactive menu
              StlGen list                              list shapes and their parameters
              StlGen <shape> [--<parameter> <value>]... [--density <x>] [--format binary|ascii|both] [--out <folder>] [--name <file base name>]
              StlGen suite [--densities 1,4,16] [--format binary|ascii|both] [--out <folder>]

            --density multiplies every resolution parameter (segments, stacks, divisions...) after your values are applied.

            Examples:
              StlGen cylinder --radius 5 --height 80 --segments 256 --format both
              StlGen sphere --density 8
              StlGen cone --topRadius 0 --segments 4                (square pyramid)
              StlGen box --divisionsX 50 --divisionsY 50 --divisionsZ 5
              StlGen suite --densities 1,10,100 --out C:\temp\stl
            """);
    }

    private void PrintShapes()
    {
        foreach (var shape in shapes)
        {
            console.WriteLine();
            console.WriteLine($"{shape.Name}: {shape.Description}", ConsoleColor.White);
            foreach (var p in shape.Parameters)
            {
                var scaling = p.Scaling == DensityScaling.None ? "" : $"  [scaled by density: {p.Scaling}]";
                console.WriteLine($"  --{p.Name,-14} default {p.Default,-6:G} range {p.Min:G}..{p.Max:G}  {p.Description}{scaling}");
            }
        }
    }
}
