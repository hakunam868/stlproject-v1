using StlGen.Services;

namespace StlGen.UI;

public interface IResultPrinter
{
    void Print(GenerationResult result);

    void PrintSuite(IReadOnlyList<SuiteEntry> entries);

    void PrintSuiteRow(SuiteEntry entry);
}

public sealed class ResultPrinter(IConsole console) : IResultPrinter
{
    public void Print(GenerationResult result)
    {
        console.WriteLine($"Generated {result.Shape.Name}: {result.TriangleCount:N0} triangles, {result.VertexCount:N0} vertices " +
                          $"(build {Duration(result.BuildTime)}, write {Duration(result.WriteTime)})", ConsoleColor.Green);
        console.WriteLine($"  {GenerationService.Describe(result.Parameters)}", ConsoleColor.DarkGray);
        foreach (var file in result.Files)
            console.WriteLine($"  {file.Format,-6} {Size(file.SizeBytes),10}  {file.Path}");
    }

    public void PrintSuiteRow(SuiteEntry entry)
    {
        if (entry.Result is { } r)
            console.WriteLine($"  {entry.Shape.Name,-10} x{entry.Density,-6:G4} {r.TriangleCount,12:N0} tris {r.VertexCount,12:N0} verts  " +
                              string.Join(", ", r.Files.Select(f => Path.GetFileName(f.Path))));
        else
            console.WriteLine($"  {entry.Shape.Name,-10} x{entry.Density,-6:G4} skipped: {entry.Error}", ConsoleColor.Yellow);
    }

    public void PrintSuite(IReadOnlyList<SuiteEntry> entries)
    {
        var ok = entries.Where(e => e.Result is not null).ToList();
        var bytes = ok.SelectMany(e => e.Result!.Files).Sum(f => f.SizeBytes);
        console.WriteLine($"Suite finished: {ok.Count} of {entries.Count} models written, {Size(bytes)} in total.",
                          ok.Count == entries.Count ? ConsoleColor.Green : ConsoleColor.Yellow);
    }

    private static string Duration(TimeSpan t) => t.TotalSeconds >= 1 ? $"{t.TotalSeconds:F2} s" : $"{t.TotalMilliseconds:F0} ms";

    private static string Size(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };
}
