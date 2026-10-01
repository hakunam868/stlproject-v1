namespace ParseStl.Analysis;

/// <summary>Shape measurements of one triangle (double precision).</summary>
/// <param name="Area">Triangle area.</param>
/// <param name="MinAngleDegrees">Smallest interior angle; 60 for an equilateral triangle, 0 when degenerate.</param>
/// <param name="AspectRatio">Longest edge / shortest altitude = L² / (2·Area). 1.155 for equilateral; +∞ when degenerate.</param>
/// <param name="LongestEdge">Length of the longest edge.</param>
public readonly record struct TriangleMetrics(double Area, double MinAngleDegrees, double AspectRatio, double LongestEdge);

/// <summary>A triangle reported in a warning list.</summary>
public readonly record struct TriangleFinding(int Triangle, int A, int B, int C, TriangleMetrics Metrics);

[Flags]
public enum TriangleClassification
{
    Normal = 0,
    Degenerate = 1,
    Sliver = 2,
    VeryLarge = 4,
}
