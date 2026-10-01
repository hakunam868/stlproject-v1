using System.Numerics;
using ParseStl.Model;

namespace ParseStl.Analysis;

/// <summary>Summary statistics over all non-degenerate triangles.</summary>
public sealed record TriangleStatistics(
    int MeasuredTriangles,
    double SurfaceArea,
    double MinArea,
    double MaxArea,
    double MeanArea,
    double SmallestAngleDegrees,
    double MeanMinAngleDegrees,
    double MaxAspectRatio,
    double MeanAspectRatio,
    double MaxEdgeLength);

/// <summary>A vertex with more incident edges than the configured limit.</summary>
public readonly record struct VertexValenceFinding(int Vertex, Vector3 Position, int Valence, bool TouchesSliver);

/// <summary>Per-triangle and per-vertex results. The lists are complete and sorted worst first.</summary>
public sealed record QualityFindings(
    int TriangleCount,
    double BoundingBoxDiagonal,
    IReadOnlyList<TriangleFinding> Slivers,
    IReadOnlyList<TriangleFinding> LargeTriangles,
    IReadOnlyList<int> DegenerateTriangles,
    IReadOnlyList<VertexValenceFinding> HighValenceVertices,
    int SliversWithoutHighValenceVertex)
{
    public double SliverPercent => Percent(Slivers.Count);

    public double LargeTrianglePercent => Percent(LargeTriangles.Count);

    public int HighValenceVerticesTouchingSlivers => HighValenceVertices.Count(v => v.TouchesSliver);

    private double Percent(int count) => TriangleCount == 0 ? 0 : 100.0 * count / TriangleCount;
}

/// <summary>The result of one model-level check.</summary>
public sealed record RuleResult(string Check, bool Passed, string Detail);

public sealed record AnalysisReport(
    LoadedModel Model,
    BoundingBox BoundingBox,
    TriangleStatistics Statistics,
    EdgeTopology Topology,
    QualityFindings Findings,
    IReadOnlyList<RuleResult> ModelChecks)
{
    public IEnumerable<RuleResult> Warnings => ModelChecks.Where(c => !c.Passed);
}
