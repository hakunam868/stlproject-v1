using System.Numerics;

namespace ParseStl.Model;

/// <summary>Axis-aligned bounding box.</summary>
public readonly record struct BoundingBox(Vector3 Min, Vector3 Max)
{
    public Vector3 Size => Max - Min;

    public Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>Length of the box diagonal, computed in double precision.</summary>
    public double Diagonal
    {
        get
        {
            var s = Size;
            return Math.Sqrt((double)s.X * s.X + (double)s.Y * s.Y + (double)s.Z * s.Z);
        }
    }

    public static BoundingBox FromPoints(ReadOnlySpan<Vector3> points)
    {
        if (points.IsEmpty)
            throw new ArgumentException("Cannot compute a bounding box of zero points.", nameof(points));

        var min = points[0];
        var max = points[0];
        for (var i = 1; i < points.Length; i++)
        {
            min = Vector3.Min(min, points[i]);
            max = Vector3.Max(max, points[i]);
        }

        return new BoundingBox(min, max);
    }
}
