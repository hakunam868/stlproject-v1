using ParseStl.Configuration;

namespace ParseStl.Analysis;

public interface ITriangleClassifier
{
    /// <param name="metrics">Shape of the triangle.</param>
    /// <param name="boundingBoxDiagonal">Diagonal of the model bounding box (the reference size).</param>
    /// <param name="hasRepeatedVertex">True when two corners were welded into the same vertex.</param>
    TriangleClassification Classify(in TriangleMetrics metrics, double boundingBoxDiagonal, bool hasRepeatedVertex);
}

/// <summary>
/// <list type="bullet">
/// <item>Degenerate: corners collapsed together, or area too small to measure at float precision for this model size.</item>
/// <item>Sliver: smallest angle &lt; SliverMinAngleDegrees OR aspect ratio &gt; SliverMaxAspectRatio.</item>
/// <item>Very large: longest edge &gt; LargeTriangleEdgeToDiagonalRatio × bounding-box diagonal.</item>
/// </list>
/// A triangle can be both a sliver and very large (for example, a long strip down the side of a cylinder).
/// </summary>
public sealed class TriangleClassifier(QualitySettings settings) : ITriangleClassifier
{
    /// <summary>Area below this fraction of diagonal² is treated as zero (float32 has about 7 significant digits).</summary>
    private const double DegenerateRelativeArea = 1e-12;

    public TriangleClassification Classify(in TriangleMetrics metrics, double boundingBoxDiagonal, bool hasRepeatedVertex)
    {
        if (hasRepeatedVertex || metrics.Area <= DegenerateRelativeArea * boundingBoxDiagonal * boundingBoxDiagonal)
            return TriangleClassification.Degenerate;

        var result = TriangleClassification.Normal;

        if (metrics.MinAngleDegrees < settings.SliverMinAngleDegrees || metrics.AspectRatio > settings.SliverMaxAspectRatio)
            result |= TriangleClassification.Sliver;

        if (metrics.LongestEdge > settings.LargeTriangleEdgeToDiagonalRatio * boundingBoxDiagonal)
            result |= TriangleClassification.VeryLarge;

        return result;
    }
}
