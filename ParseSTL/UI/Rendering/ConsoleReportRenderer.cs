using ParseStl.Analysis;
using ParseStl.Configuration;
using static ParseStl.UI.Formatting;

namespace ParseStl.UI.Rendering;

public interface IReportRenderer
{
    void Render(AnalysisReport report);
}

public sealed class ConsoleReportRenderer(IConsole console, QualitySettings settings) : IReportRenderer
{
    private const int LabelWidth = 26;

    public void Render(AnalysisReport report)
    {
        RenderSummary(report);
        RenderBoundingBox(report);
        RenderStatistics(report);
        RenderTopology(report);
        RenderModelChecks(report);
        RenderSlivers(report.Findings);
        RenderLargeTriangles(report.Findings);
        RenderHighValence(report.Findings);
        RenderDegenerate(report.Findings);
        console.WriteLine();
    }

    private void RenderSummary(AnalysisReport report)
    {
        var m = report.Model;
        Header($"ANALYSIS: {m.FileName}");
        Row("Format", $"{m.Format} STL");
        Row("Solid name / header", m.SolidName);
        Row("File size", FileSize(m.FileSizeBytes));
        Row("Triangles (faces)", $"{m.Mesh.TriangleCount:N0}");
        Row("Vertices (unique)", $"{m.Mesh.VertexCount:N0}");
        Row("Vertices (raw, 3 x faces)", $"{m.RawVertexCount:N0}");
        Row("Duplicates removed", $"{m.DuplicateVerticesRemoved:N0} ({Percent(m.DuplicateVerticesRemoved, m.RawVertexCount)})");
        Row("Parse / weld time", $"{Duration(m.ParseTime)} / {Duration(m.WeldTime)}");
    }

    private void RenderBoundingBox(AnalysisReport report)
    {
        var box = report.BoundingBox;
        Header("BOUNDING BOX");
        Row("Min", Vector(box.Min));
        Row("Max", Vector(box.Max));
        Row("Size (X, Y, Z)", Vector(box.Size));
        Row("Center", Vector(box.Center));
        Row("Diagonal", Number(box.Diagonal));
    }

    private void RenderStatistics(AnalysisReport report)
    {
        var s = report.Statistics;
        Header("TRIANGLE QUALITY");
        Row("Surface area", Number(s.SurfaceArea));
        if (s.MeasuredTriangles == 0)
        {
            Row("Statistics", "n/a (every triangle is degenerate)");
            return;
        }

        Row("Area min / mean / max", $"{Number(s.MinArea)} / {Number(s.MeanArea)} / {Number(s.MaxArea)}");
        Row("Smallest angle", $"{s.SmallestAngleDegrees:F3}°   (mean of per-triangle min angle: {s.MeanMinAngleDegrees:F2}°)");
        Row("Aspect ratio max / mean", $"{Number(s.MaxAspectRatio)} / {Number(s.MeanAspectRatio)}   (equilateral = 1.155)");
        Row("Longest edge", $"{Number(s.MaxEdgeLength)}   ({Percent(s.MaxEdgeLength, report.BoundingBox.Diagonal)} of diagonal)");
    }

    private void RenderTopology(AnalysisReport report)
    {
        var t = report.Topology;
        Header("TOPOLOGY");
        Row("Unique edges", $"{t.UniqueEdgeCount:N0}");
        Row("Boundary (open) edges", $"{t.BoundaryEdgeCount:N0}");
        Row("Non-manifold edges", $"{t.NonManifoldEdgeCount:N0}");
        Row("Vertex valence max / mean", $"{t.MaxValence} / {t.MeanValence:F2}");
    }

    private void RenderModelChecks(AnalysisReport report)
    {
        Header("MODEL CHECKS");
        foreach (var check in report.ModelChecks)
        {
            console.Write(check.Passed ? "  [ OK ] " : "  [WARN] ", check.Passed ? ConsoleColor.Green : ConsoleColor.Yellow);
            console.WriteLine($"{check.Check}: {check.Detail}");
        }

        var warnings = report.Warnings.Count();
        console.WriteLine();
        console.WriteLine(warnings == 0 ? "  No model-level warnings." : $"  {warnings} model-level warning(s).",
                          warnings == 0 ? ConsoleColor.Green : ConsoleColor.Yellow);
    }

    private void RenderSlivers(QualityFindings f)
    {
        if (f.Slivers.Count == 0)
            return;

        WarningHeader($"SLIVER TRIANGLES: {f.Slivers.Count:N0} ({f.SliverPercent:F2}% of faces), worst first",
                      $"min angle < {settings.SliverMinAngleDegrees}° or aspect ratio > {settings.SliverMaxAspectRatio}");
        console.WriteLine($"  {"Face",10} {"Vertices (A, B, C)",-26} {"Min angle",10} {"Aspect",10} {"Area",12}", ConsoleColor.DarkGray);
        foreach (var s in f.Slivers.Take(settings.MaxListedWarnings))
            console.WriteLine($"  {s.Triangle,10} {$"{s.A}, {s.B}, {s.C}",-26} {s.Metrics.MinAngleDegrees,9:F3}° {Number(s.Metrics.AspectRatio),10} {Number(s.Metrics.Area),12}");
        More(f.Slivers.Count);

        console.WriteLine($"  {f.SliversWithoutHighValenceVertex:N0} of these slivers have no high-valence corner (valence > {settings.MaxVertexValence}), " +
                          "so the valence check alone does not find every sliver.", ConsoleColor.DarkGray);
    }

    private void RenderLargeTriangles(QualityFindings f)
    {
        if (f.LargeTriangles.Count == 0)
            return;

        WarningHeader($"VERY LARGE TRIANGLES: {f.LargeTriangles.Count:N0} ({f.LargeTrianglePercent:F2}% of faces), largest first",
                      $"longest edge > {settings.LargeTriangleEdgeToDiagonalRatio * 100:0.##}% of bounding-box diagonal ({Number(settings.LargeTriangleEdgeToDiagonalRatio * f.BoundingBoxDiagonal)})");
        console.WriteLine($"  {"Face",10} {"Vertices (A, B, C)",-26} {"Longest edge",13} {"% diagonal",11} {"Area",12}", ConsoleColor.DarkGray);
        foreach (var l in f.LargeTriangles.Take(settings.MaxListedWarnings))
            console.WriteLine($"  {l.Triangle,10} {$"{l.A}, {l.B}, {l.C}",-26} {Number(l.Metrics.LongestEdge),13} {Percent(l.Metrics.LongestEdge, f.BoundingBoxDiagonal),11} {Number(l.Metrics.Area),12}");
        More(f.LargeTriangles.Count);
    }

    private void RenderHighValence(QualityFindings f)
    {
        if (f.HighValenceVertices.Count == 0)
            return;

        WarningHeader($"HIGH-VALENCE VERTICES: {f.HighValenceVertices.Count:N0}, highest first",
                      $"more than {settings.MaxVertexValence} incident edges; {f.HighValenceVerticesTouchingSlivers:N0} touch a sliver");
        console.WriteLine($"  {"Vertex",10} {"Valence",8} {"Touches sliver",15}  Position", ConsoleColor.DarkGray);
        foreach (var v in f.HighValenceVertices.Take(settings.MaxListedWarnings))
            console.WriteLine($"  {v.Vertex,10} {v.Valence,8} {(v.TouchesSliver ? "yes" : "no"),15}  {Vector(v.Position)}");
        More(f.HighValenceVertices.Count);
    }

    private void RenderDegenerate(QualityFindings f)
    {
        if (f.DegenerateTriangles.Count == 0)
            return;

        WarningHeader($"DEGENERATE TRIANGLES: {f.DegenerateTriangles.Count:N0}", "zero area or two corners welded together");
        console.WriteLine("  Faces: " + string.Join(", ", f.DegenerateTriangles.Take(settings.MaxListedWarnings)) +
                          (f.DegenerateTriangles.Count > settings.MaxListedWarnings ? ", ..." : ""));
    }

    private void Header(string title)
    {
        console.WriteLine();
        console.WriteLine($"=== {title} ===", ConsoleColor.White);
    }

    private void WarningHeader(string title, string rule)
    {
        console.WriteLine();
        console.WriteLine($"!!! {title}", ConsoleColor.Yellow);
        console.WriteLine($"    rule: {rule}", ConsoleColor.DarkGray);
    }

    private void Row(string label, string value) => console.WriteLine($"  {label.PadRight(LabelWidth)}: {value}");

    private void More(int total)
    {
        if (total > settings.MaxListedWarnings)
            console.WriteLine($"  ... and {total - settings.MaxListedWarnings:N0} more (raise MaxListedWarnings in Config.json to list more)", ConsoleColor.DarkGray);
    }

    private static string Percent(double part, double whole) => whole == 0 ? "n/a" : $"{100.0 * part / whole:F2}%";
}
