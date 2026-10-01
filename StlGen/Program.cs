using StlGen.Configuration;
using StlGen.Output;
using StlGen.Services;
using StlGen.Shapes;
using StlGen.UI;

// Composition root: concrete types are created only here.
IConsole console = new SystemConsole();

GeneratorSettings settings;
try
{
    settings = new JsonGeneratorSettingsProvider(Path.Combine(AppContext.BaseDirectory, "Config.json")).Load();
}
catch (ConfigurationException ex)
{
    console.WriteLine(ex.Message, ConsoleColor.Red);
    return 2;
}

IShapeGenerator[] shapes =
[
    new BoxGenerator(),
    new CylinderGenerator(),
    new ConeGenerator(),
    new PrismGenerator(),
    new SphereGenerator(),
    new IcosphereGenerator(),
    new TorusGenerator(),
    new TubeGenerator(),
];

var service = new GenerationService([new BinaryStlWriter(), new AsciiStlWriter()], settings);
var suite = new TestSuiteGenerator(shapes, service);
var printer = new ResultPrinter(console);

return args.Length > 0
    ? new CommandLineRunner(shapes, service, suite, printer, settings, console).Run(args)
    : new InteractiveRunner(shapes, service, suite, printer, settings, new Prompt(console), console).Run();
