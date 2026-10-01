using ParseStl.Analysis.Rules;
using ParseStl.Configuration;
using ParseStl.Model;

namespace ParseStl.Analysis;

public interface IMeshAnalyzer
{
    AnalysisReport Analyze(LoadedModel model);
}

/// <summary>
/// Combines the analysis steps: bounding box, per-triangle metrics and classification, edge topology and valence,
/// then the model-level rules. Each step is an injected abstraction.
/// </summary>
public sealed class MeshAnalyzer(
    ITriangleMetricsCalculator metricsCalculator,
    ITriangleClassifier classifier,
    IEdgeTopologyAnalyzer topologyAnalyzer,
    IEnumerable<IQualityRule> rules,
    QualitySettings settings) : IMeshAnalyzer
{
    private readonly IReadOnlyList<IQualityRule> _rules = rules.ToList();

    public AnalysisReport Analyze(LoadedModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var mesh = model.Mesh;
        var boundingBox = BoundingBox.FromPoints(mesh.Vertices);
        var diagonal = boundingBox.Diagonal;

        var (statistics, slivers, large, degenerate) = MeasureTriangles(mesh, diagonal);
        var topology = topologyAnalyzer.Analyze(mesh);
        var (highValence, sliversWithoutHub) = FindHighValenceVertices(mesh, topology, slivers);

        var findings = new QualityFindings(
            mesh.TriangleCount,
            diagonal,
            slivers.OrderBy(s => s.Metrics.MinAngleDegrees).ThenByDescending(s => s.Metrics.AspectRatio).ToList(),
            large.OrderByDescending(l => l.Metrics.LongestEdge).ToList(),
            degenerate,
            highValence.OrderByDescending(v => v.Valence).ToList(),
            sliversWithoutHub);

        var checks = _rules.Select(r => r.Evaluate(findings, topology)).ToList();
        return new AnalysisReport(model, boundingBox, statistics, topology, findings, checks);
    }

    private (TriangleStatistics, List<TriangleFinding> Slivers, List<TriangleFinding> Large, List<int> Degenerate)
        MeasureTriangles(Mesh mesh, double diagonal)
    {
        var vertices = mesh.Vertices;
        var indices = mesh.Indices;

        var slivers = new List<TriangleFinding>();
        var large = new List<TriangleFinding>();
        var degenerate = new List<int>();

        var measured = 0;
        double surfaceArea = 0, minArea = double.MaxValue, maxArea = 0;
        double smallestAngle = double.MaxValue, sumMinAngle = 0;
        double maxAspect = 0, sumAspect = 0, maxEdge = 0;

        for (var t = 0; t < mesh.TriangleCount; t++)
        {
            int a = indices[3 * t], b = indices[3 * t + 1], c = indices[3 * t + 2];
            var metrics = metricsCalculator.Calculate(vertices[a], vertices[b], vertices[c]);
            surfaceArea += metrics.Area;

            var classification = classifier.Classify(metrics, diagonal, a == b || b == c || a == c);
            if (classification.HasFlag(TriangleClassification.Degenerate))
            {
                degenerate.Add(t);
                continue;
            }

            measured++;
            minArea = Math.Min(minArea, metrics.Area);
            maxArea = Math.Max(maxArea, metrics.Area);
            smallestAngle = Math.Min(smallestAngle, metrics.MinAngleDegrees);
            sumMinAngle += metrics.MinAngleDegrees;
            maxAspect = Math.Max(maxAspect, metrics.AspectRatio);
            sumAspect += metrics.AspectRatio;
            maxEdge = Math.Max(maxEdge, metrics.LongestEdge);

            var finding = new TriangleFinding(t, a, b, c, metrics);
            if (classification.HasFlag(TriangleClassification.Sliver))
                slivers.Add(finding);
            if (classification.HasFlag(TriangleClassification.VeryLarge))
                large.Add(finding);
        }

        var statistics = measured == 0
            ? new TriangleStatistics(0, surfaceArea, 0, 0, 0, 0, 0, 0, 0, 0)
            : new TriangleStatistics(measured, surfaceArea, minArea, maxArea, surfaceArea / measured,
                                     smallestAngle, sumMinAngle / measured, maxAspect, sumAspect / measured, maxEdge);

        return (statistics, slivers, large, degenerate);
    }

    /// <summary>
    /// Finds high-valence vertices and checks how they relate to slivers. A high-valence vertex usually means there
    /// are slivers around it, but many slivers (long thin strips, for example) have no high-valence corner.
    /// Both directions are counted.
    /// </summary>
    private (List<VertexValenceFinding> HighValence, int SliversWithoutHub) FindHighValenceVertices(
        Mesh mesh, EdgeTopology topology, List<TriangleFinding> slivers)
    {
        var touchesSliver = new bool[mesh.VertexCount];
        foreach (var s in slivers)
            touchesSliver[s.A] = touchesSliver[s.B] = touchesSliver[s.C] = true;

        var isHub = new bool[mesh.VertexCount];
        var highValence = new List<VertexValenceFinding>();
        var vertices = mesh.Vertices;
        for (var v = 0; v < topology.Valence.Length; v++)
        {
            if (topology.Valence[v] <= settings.MaxVertexValence)
                continue;
            isHub[v] = true;
            highValence.Add(new VertexValenceFinding(v, vertices[v], topology.Valence[v], touchesSliver[v]));
        }

        var sliversWithoutHub = slivers.Count(s => !isHub[s.A] && !isHub[s.B] && !isHub[s.C]);
        return (highValence, sliversWithoutHub);
    }
}
