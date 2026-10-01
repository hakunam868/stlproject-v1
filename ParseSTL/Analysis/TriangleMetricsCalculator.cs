using System.Numerics;

namespace ParseStl.Analysis;

public interface ITriangleMetricsCalculator
{
    TriangleMetrics Calculate(Vector3 a, Vector3 b, Vector3 c);
}

public sealed class TriangleMetricsCalculator : ITriangleMetricsCalculator
{
    private const double RadiansToDegrees = 180.0 / Math.PI;

    public TriangleMetrics Calculate(Vector3 a, Vector3 b, Vector3 c)
    {
        // Edge vectors in double precision.
        double abX = b.X - a.X, abY = b.Y - a.Y, abZ = b.Z - a.Z;
        double bcX = c.X - b.X, bcY = c.Y - b.Y, bcZ = c.Z - b.Z;
        double caX = a.X - c.X, caY = a.Y - c.Y, caZ = a.Z - c.Z;

        var ab2 = abX * abX + abY * abY + abZ * abZ;
        var bc2 = bcX * bcX + bcY * bcY + bcZ * bcZ;
        var ca2 = caX * caX + caY * caY + caZ * caZ;
        var longest = Math.Sqrt(Math.Max(ab2, Math.Max(bc2, ca2)));

        // |AB x AC| is twice the area. AC = -CA.
        var crossX = abY * -caZ - abZ * -caY;
        var crossY = abZ * -caX - abX * -caZ;
        var crossZ = abX * -caY - abY * -caX;
        var twiceArea = Math.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);

        if (twiceArea == 0)
            return new TriangleMetrics(0, 0, double.PositiveInfinity, longest);

        // The angle at each corner is atan2(|u x v|, u . v), and |u x v| = 2·Area at every corner.
        // atan2 stays accurate for very small angles, where acos would lose precision.
        var dotA = -(abX * caX + abY * caY + abZ * caZ); // AB . AC
        var dotB = -(abX * bcX + abY * bcY + abZ * bcZ); // BA . BC
        var dotC = -(bcX * caX + bcY * caY + bcZ * caZ); // CB . CA
        var minAngle = Math.Min(Math.Atan2(twiceArea, dotA), Math.Min(Math.Atan2(twiceArea, dotB), Math.Atan2(twiceArea, dotC)));

        var area = twiceArea * 0.5;
        var aspectRatio = longest * longest / twiceArea; // L / (2A / L)

        return new TriangleMetrics(area, minAngle * RadiansToDegrees, aspectRatio, longest);
    }
}
