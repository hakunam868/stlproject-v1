namespace StlGen.Geometry;

/// <summary>A point of a 2D profile in the (radius, z) half-plane. A radius of 0 lies on the Z axis.</summary>
public readonly record struct ProfilePoint(double Radius, double Z);

public static class Profile
{
    /// <summary>
    /// Points from <paramref name="from"/> to <paramref name="to"/> split into <paramref name="steps"/> equal parts.
    /// Both end points are included unless <paramref name="includeEnd"/> is false. The end points are returned exactly,
    /// without interpolation rounding.
    /// </summary>
    public static IEnumerable<ProfilePoint> Line(ProfilePoint from, ProfilePoint to, int steps, bool includeEnd = true)
    {
        yield return from;
        for (var i = 1; i < steps; i++)
        {
            var t = (double)i / steps;
            yield return new ProfilePoint(from.Radius + (to.Radius - from.Radius) * t, from.Z + (to.Z - from.Z) * t);
        }

        if (includeEnd)
            yield return to;
    }
}

/// <summary>
/// Builds a surface of revolution by sweeping a (radius, z) profile around the Z axis. Cylinders, cones, prisms,
/// spheres, tori and tubes are all built this way.
/// <list type="bullet">
/// <item>A profile point with radius 0 becomes one vertex on the axis (a pole or apex), not a ring of duplicates.</item>
/// <item>An open profile ending at radius &gt; 0 can be closed with flat caps. 1 cap ring is a fan around one centre vertex;
/// more rings add concentric bands.</item>
/// <item>The profile must run counter-clockwise in the (radius, z) plane (upwards on the outside) so normals point outwards.</item>
/// </list>
/// </summary>
public static class Lathe
{
    public static void Build(MeshBuilder builder, IReadOnlyList<ProfilePoint> profile, bool closed, int segments,
                             int capRings = 0, double angleOffset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(segments, 3);
        ArgumentOutOfRangeException.ThrowIfLessThan(profile.Count, 2);

        var rows = profile.Select(p => Ring(builder, p.Radius, p.Z, segments, angleOffset)).ToArray();

        for (var j = 0; j < rows.Length - 1; j++)
            Band(builder, rows[j], rows[j + 1], segments);

        if (closed)
        {
            Band(builder, rows[^1], rows[0], segments);
            return;
        }

        if (capRings <= 0)
            return;
        if (rows[0].Length > 1)
            Cap(builder, rows[0], profile[0], segments, capRings, angleOffset, facesUp: false);
        if (rows[^1].Length > 1)
            Cap(builder, rows[^1], profile[^1], segments, capRings, angleOffset, facesUp: true);
    }

    private static int[] Ring(MeshBuilder builder, double radius, double z, int segments, double angleOffset)
    {
        if (radius <= 0)
            return [builder.AddVertex(0, 0, z)];

        var ring = new int[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = angleOffset + 2 * Math.PI * i / segments;
            ring[i] = builder.AddVertex(radius * Math.Cos(angle), radius * Math.Sin(angle), z);
        }

        return ring;
    }

    /// <summary>Joins two consecutive profile rows; <paramref name="lower"/> comes first along the profile.</summary>
    private static void Band(MeshBuilder builder, int[] lower, int[] upper, int segments)
    {
        if (lower.Length == 1 && upper.Length == 1)
            return;

        for (var i = 0; i < segments; i++)
        {
            var next = (i + 1) % segments;
            if (lower.Length == 1)
            {
                builder.AddTriangle(lower[0], upper[next], upper[i]);
            }
            else if (upper.Length == 1)
            {
                builder.AddTriangle(lower[i], lower[next], upper[0]);
            }
            else
            {
                builder.AddTriangle(lower[i], lower[next], upper[next]);
                builder.AddTriangle(lower[i], upper[next], upper[i]);
            }
        }
    }

    private static void Cap(MeshBuilder builder, int[] rim, ProfilePoint end, int segments, int rings, double angleOffset, bool facesUp)
    {
        var outer = rim;
        for (var k = rings - 1; k >= 0; k--)
        {
            var inner = Ring(builder, end.Radius * k / rings, end.Z, segments, angleOffset); // k = 0 is the centre vertex
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                if (inner.Length == 1)
                {
                    Triangle(builder, facesUp, inner[0], outer[i], outer[next]);
                }
                else
                {
                    Triangle(builder, facesUp, inner[i], outer[i], outer[next]);
                    Triangle(builder, facesUp, inner[i], outer[next], inner[next]);
                }
            }

            outer = inner;
        }
    }

    private static void Triangle(MeshBuilder builder, bool facesUp, int a, int b, int c)
    {
        if (facesUp)
            builder.AddTriangle(a, b, c);
        else
            builder.AddTriangle(a, c, b);
    }
}
