using StlGen.Configuration;
using StlGen.Geometry;
using StlGen.Output;
using StlGen.Services;
using StlGen.Shapes;

namespace StlGen.UI;

/// <summary>Numbered menu: pick a shape, adjust its parameters (Enter keeps the default), set the density and format.</summary>
public sealed class InteractiveRunner(
    IReadOnlyList<IShapeGenerator> shapes,
    IGenerationService service,
    ITestSuiteGenerator suite,
    IResultPrinter printer,
    GeneratorSettings settings,
    IPrompt prompt,
    IConsole console)
{
    public int Run()
    {
        console.WriteLine("StlGen: STL test model generator", ConsoleColor.White);

        while (true)
        {
            console.WriteLine();
            console.WriteLine("----------------------------------------", ConsoleColor.DarkGray);
            console.WriteLine($"Output folder: {settings.OutputFolder}", ConsoleColor.DarkGray);
            for (var i = 0; i < shapes.Count; i++)
                console.WriteLine($"  {i + 1,2}. {shapes[i].Name,-10} {shapes[i].Description}");
            var suiteChoice = shapes.Count + 1;
            var exitChoice = shapes.Count + 2;
            console.WriteLine($"  {suiteChoice,2}. {"suite",-10} Generate every shape at densities {string.Join(", ", settings.Suite.Densities)}");
            console.WriteLine($"  {exitChoice,2}. {"exit",-10} Close the application");

            var choice = prompt.Choice("Choose an option", 1, exitChoice);
            if (choice is null || choice == exitChoice)
                return 0;

            if (choice == suiteChoice)
                RunSuite();
            else
                RunShape(shapes[choice.Value - 1]);
        }
    }

    private void RunShape(IShapeGenerator shape)
    {
        console.WriteLine();
        console.WriteLine($"{shape.Name}: {shape.Description}", ConsoleColor.White);
        console.WriteLine("Press Enter to keep a value. Resolution parameters are then multiplied by the density.", ConsoleColor.DarkGray);

        var overrides = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in shape.Parameters)
        {
            var value = prompt.Number($"  {parameter.Name} ({parameter.Description})", parameter.Default, parameter.Min, parameter.Max, parameter.IsInteger);
            if (value is null)
                return;
            overrides[parameter.Name] = value.Value;
        }

        var density = prompt.Number("  density multiplier", 1, 0.01, 1000, integer: false);
        if (density is null)
            return;

        var formats = AskFormats();
        if (formats is null)
            return;

        try
        {
            printer.Print(service.Generate(new GenerationRequest(shape, overrides, density.Value, formats, settings.OutputFolder)));
        }
        catch (Exception ex) when (ex is GenerationException or IOException or UnauthorizedAccessException or OutOfMemoryException)
        {
            console.WriteLine($"Not generated: {ex.Message}", ConsoleColor.Red);
        }
    }

    private void RunSuite()
    {
        var formats = AskFormats(settings.Suite.Formats);
        if (formats is null)
            return;

        console.WriteLine($"Generating suite into {settings.OutputFolder} ...");
        var entries = suite.Generate(settings.Suite.Densities, formats, settings.OutputFolder, printer.PrintSuiteRow);
        printer.PrintSuite(entries);
    }

    private IReadOnlyList<StlOutputFormat>? AskFormats(IReadOnlyList<StlOutputFormat>? defaults = null)
    {
        defaults ??= settings.DefaultFormats;
        var defaultChoice = defaults.Distinct().Count() > 1 ? 3 : defaults[0] == StlOutputFormat.Ascii ? 2 : 1;
        console.WriteLine("  Format: 1. Binary   2. ASCII   3. Both");
        return prompt.Choice("  format", 1, 3, defaultChoice) switch
        {
            1 => [StlOutputFormat.Binary],
            2 => [StlOutputFormat.Ascii],
            3 => [StlOutputFormat.Binary, StlOutputFormat.Ascii],
            _ => null,
        };
    }
}
