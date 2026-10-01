using ParseStl.Files;
using ParseStl.Parsing;
using ParseStl.Processing;

namespace ParseStl.UI.Commands;

public sealed class OpenFileCommand(
    IStlFileRepository repository,
    IModelLoader loader,
    ApplicationSession session,
    INumberPrompt prompt,
    IConsole console) : IMenuCommand
{
    public string Title => "Open STL file";

    public CommandResult Execute()
    {
        var files = repository.GetFiles();
        console.WriteLine();
        console.WriteLine($"STL folder: {repository.FolderPath}", ConsoleColor.DarkGray);

        if (files.Count == 0)
        {
            console.WriteLine("No .stl files found. Copy files into the folder above, or change \"StlFolder\" in Config.json.", ConsoleColor.Yellow);
            return CommandResult.Continue;
        }

        for (var i = 0; i < files.Count; i++)
            console.WriteLine($"  {i + 1,3}. {files[i].Name,-40} {Formatting.FileSize(files[i].Length),12}");
        console.WriteLine("    0. Cancel");

        var choice = prompt.Ask("Select a file", 0, files.Count);
        if (choice is null or 0)
            return CommandResult.Continue;

        var file = files[choice.Value - 1];
        console.WriteLine($"Loading {file.Name} ...");

        try
        {
            var model = loader.Load(file.FullName);
            session.CurrentModel = model;

            console.WriteLine($"Loaded {model.FileName} ({model.Format} STL, solid \"{model.SolidName}\")", ConsoleColor.Green);
            console.WriteLine($"  Triangles : {model.Mesh.TriangleCount:N0}");
            console.WriteLine($"  Vertices  : {model.Mesh.VertexCount:N0} unique (from {model.RawVertexCount:N0}; {model.DuplicateVerticesRemoved:N0} duplicates removed)");
            console.WriteLine($"  Time      : parse {Formatting.Duration(model.ParseTime)}, weld {Formatting.Duration(model.WeldTime)}");
            console.WriteLine("Choose 'Analyse' to see the full report.", ConsoleColor.DarkGray);
        }
        catch (Exception ex) when (ex is StlParseException or IOException or UnauthorizedAccessException)
        {
            console.WriteLine($"Could not load {file.Name}: {ex.Message}", ConsoleColor.Red);
        }

        return CommandResult.Continue;
    }
}
