using ParseStl.Analysis;
using ParseStl.Analysis.Rules;
using ParseStl.Configuration;
using ParseStl.Files;
using ParseStl.Parsing;
using ParseStl.Processing;
using ParseStl.UI;
using ParseStl.UI.Commands;
using ParseStl.UI.Rendering;

// Composition root: concrete types are created only here. Everything else depends on interfaces.
IConsole console = new SystemConsole();

AppSettings settings;
try
{
    settings = new JsonAppSettingsProvider(Path.Combine(AppContext.BaseDirectory, "Config.json")).Load();
}
catch (ConfigurationException ex)
{
    console.WriteLine(ex.Message, ConsoleColor.Red);
    return 1;
}

var session = new ApplicationSession();
var prompt = new NumberPrompt(console);

var loader = new ModelLoader(
    new StlReader(new StlFormatDetector(), [new AsciiStlParser(), new BinaryStlParser()]),
    new VertexWelder(settings.Welding));

var analyzer = new MeshAnalyzer(
    new TriangleMetricsCalculator(),
    new TriangleClassifier(settings.Quality),
    new EdgeTopologyAnalyzer(),
    [
        new SliverPercentageRule(settings.Quality),
        new LargeTrianglePercentageRule(settings.Quality),
        new HighValenceVertexRule(settings.Quality),
        new DegenerateTriangleRule(),
        new WatertightRule(),
    ],
    settings.Quality);

IMenuCommand[] commands =
[
    new OpenFileCommand(new StlFileRepository(settings.StlFolder), loader, session, prompt, console),
    new AnalyseCommand(session, analyzer, new ConsoleReportRenderer(console, settings.Quality), console),
    new ExitCommand(console),
];

new MenuApplication(commands, session, prompt, console).Run();
return 0;
