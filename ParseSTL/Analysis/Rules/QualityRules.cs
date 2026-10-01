using ParseStl.Configuration;

namespace ParseStl.Analysis.Rules;

/// <summary>
/// A model-level check. To add a new check, implement this interface and register it in Program.cs;
/// the analyzer does not need to change.
/// </summary>
public interface IQualityRule
{
    RuleResult Evaluate(QualityFindings findings, EdgeTopology topology);
}

public sealed class SliverPercentageRule(QualitySettings settings) : IQualityRule
{
    public RuleResult Evaluate(QualityFindings findings, EdgeTopology topology)
    {
        var passed = findings.SliverPercent <= settings.ModelSliverPercentThreshold;
        return new RuleResult(
            "Sliver triangles",
            passed,
            $"{findings.Slivers.Count:N0} of {findings.TriangleCount:N0} triangles ({findings.SliverPercent:F2}%) are slivers; " +
            $"limit is {settings.ModelSliverPercentThreshold:F2}% (min angle < {settings.SliverMinAngleDegrees}° or aspect ratio > {settings.SliverMaxAspectRatio}).");
    }
}

public sealed class LargeTrianglePercentageRule(QualitySettings settings) : IQualityRule
{
    public RuleResult Evaluate(QualityFindings findings, EdgeTopology topology)
    {
        var passed = findings.LargeTrianglePercent <= settings.ModelLargeTrianglePercentThreshold;
        var edgeLimit = settings.LargeTriangleEdgeToDiagonalRatio * findings.BoundingBoxDiagonal;
        return new RuleResult(
            "Very large triangles",
            passed,
            $"{findings.LargeTriangles.Count:N0} of {findings.TriangleCount:N0} triangles ({findings.LargeTrianglePercent:F2}%) have an edge longer than " +
            $"{settings.LargeTriangleEdgeToDiagonalRatio * 100:0.##}% of the bounding-box diagonal ({edgeLimit:G6}); limit is {settings.ModelLargeTrianglePercentThreshold:F2}%.");
    }
}

public sealed class HighValenceVertexRule(QualitySettings settings) : IQualityRule
{
    public RuleResult Evaluate(QualityFindings findings, EdgeTopology topology)
    {
        var count = findings.HighValenceVertices.Count;
        return new RuleResult(
            "High-valence vertices",
            count == 0,
            count == 0
                ? $"No vertex has more than {settings.MaxVertexValence} incident edges (max {topology.MaxValence})."
                : $"{count:N0} vertices have more than {settings.MaxVertexValence} incident edges (max {topology.MaxValence}); " +
                  $"{findings.HighValenceVerticesTouchingSlivers:N0} of them touch at least one sliver.");
    }
}

public sealed class DegenerateTriangleRule : IQualityRule
{
    public RuleResult Evaluate(QualityFindings findings, EdgeTopology topology)
    {
        var count = findings.DegenerateTriangles.Count;
        return new RuleResult(
            "Degenerate triangles",
            count == 0,
            count == 0 ? "No zero-area triangles." : $"{count:N0} triangles have zero area or collapsed corners.");
    }
}

public sealed class WatertightRule : IQualityRule
{
    public RuleResult Evaluate(QualityFindings findings, EdgeTopology topology) => new(
        "Watertight / manifold",
        topology.IsWatertight,
        topology.IsWatertight
            ? "Every edge is shared by exactly two triangles."
            : $"{topology.BoundaryEdgeCount:N0} open (boundary) edges and {topology.NonManifoldEdgeCount:N0} non-manifold edges.");
}
