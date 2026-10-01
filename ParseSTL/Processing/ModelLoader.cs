using System.Diagnostics;
using ParseStl.Model;
using ParseStl.Parsing;

namespace ParseStl.Processing;

public interface IModelLoader
{
    LoadedModel Load(string path);
}

/// <summary>Runs the loading pipeline: parse the raw facets first, then weld duplicate vertices.</summary>
public sealed class ModelLoader(IStlReader reader, IVertexWelder welder) : IModelLoader
{
    public LoadedModel Load(string path)
    {
        var stopwatch = Stopwatch.StartNew();
        var document = reader.Read(path);
        var parseTime = stopwatch.Elapsed;

        stopwatch.Restart();
        var mesh = welder.Weld(document.Facets);
        var weldTime = stopwatch.Elapsed;

        return new LoadedModel(
            Path.GetFullPath(path),
            new FileInfo(path).Length,
            document.Name,
            document.Format,
            mesh,
            RawVertexCount: document.Facets.Count * 3,
            parseTime,
            weldTime);
    }
}
