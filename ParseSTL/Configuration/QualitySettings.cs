namespace ParseStl.Configuration;

/// <summary>Thresholds used to classify triangles and vertices and to raise model-level warnings.</summary>
public sealed class QualitySettings
{
    public double SliverMinAngleDegrees { get; set; } = 5.0;

    public double SliverMaxAspectRatio { get; set; } = 20.0;

    public double LargeTriangleEdgeToDiagonalRatio { get; set; } = 0.25;

    public double ModelSliverPercentThreshold { get; set; } = 5.0;

    public double ModelLargeTrianglePercentThreshold { get; set; } = 1.0;

    public int MaxVertexValence { get; set; } = 10;

    public int MaxListedWarnings { get; set; } = 15;

    public IEnumerable<string> Validate()
    {
        if (SliverMinAngleDegrees is <= 0 or >= 60)
            yield return "Quality.SliverMinAngleDegrees must be between 0 and 60 (exclusive).";
        if (SliverMaxAspectRatio <= 2 / Math.Sqrt(3))
            yield return "Quality.SliverMaxAspectRatio must be greater than 1.155 (the equilateral aspect ratio).";
        if (LargeTriangleEdgeToDiagonalRatio is <= 0 or > 1)
            yield return "Quality.LargeTriangleEdgeToDiagonalRatio must be in the range (0, 1].";
        if (ModelSliverPercentThreshold is < 0 or > 100)
            yield return "Quality.ModelSliverPercentThreshold must be between 0 and 100.";
        if (ModelLargeTrianglePercentThreshold is < 0 or > 100)
            yield return "Quality.ModelLargeTrianglePercentThreshold must be between 0 and 100.";
        if (MaxVertexValence < 3)
            yield return "Quality.MaxVertexValence must be at least 3.";
        if (MaxListedWarnings < 0)
            yield return "Quality.MaxListedWarnings must be >= 0.";
    }
}
