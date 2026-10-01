using System.Numerics;
using StlGen.Geometry;

namespace StlGen.Shapes;

/// <summary>
/// Sphere made by repeatedly subdividing an icosahedron. The triangles are nearly equilateral and valence is 5 or 6,
/// which makes it a good "clean" reference to compare against the UV sphere.
/// </summary>
public sealed class IcosphereGenerator : IShapeGenerator
{
    private static readonly int[] IcosahedronFaces =
    [
        0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
        1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
        3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
        4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
    ];

    public string Name => "icosphere";

    public string Description => "Geodesic sphere (subdivided icosahedron) with near-equilateral triangles; 20 x 4^subdivisions faces";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        ShapeParameter.Length("radius", "radius", 10),
        new("subdivisions", "subdivision levels (each level = 4x triangles; density adds log2(density))", 2, 0, 10,
            IsInteger: true, Scaling: DensityScaling.Log2),
    ];

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        var t = (1 + Math.Sqrt(5)) / 2;
        var points = new List<Vector3d>
        {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
            new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
            new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
        };
        for (var i = 0; i < points.Count; i++)
            points[i] = points[i].Normalized();

        var faces = IcosahedronFaces.ToArray();
        for (var level = 0; level < p.Int("subdivisions"); level++)
            faces = Subdivide(faces, points);

        var radius = p["radius"];
        var indices = points.Select(v => builder.AddVertex(v.X * radius, v.Y * radius, v.Z * radius)).ToArray();

        for (var f = 0; f < faces.Length; f += 3)
        {
            int a = indices[faces[f]], b = indices[faces[f + 1]], c = indices[faces[f + 2]];
            // Safety net: on a convex shape centred at the origin, the outward normal points the same way as the centroid.
            var pa = builder.GetVertex(a);
            var normal = Vector3.Cross(builder.GetVertex(b) - pa, builder.GetVertex(c) - pa);
            if (Vector3.Dot(normal, pa + builder.GetVertex(b) + builder.GetVertex(c)) < 0)
                (b, c) = (c, b);
            builder.AddTriangle(a, b, c);
        }
    }

    private static int[] Subdivide(int[] faces, List<Vector3d> points)
    {
        var midpoints = new Dictionary<(int, int), int>();
        var result = new int[faces.Length * 4];
        var k = 0;

        int Midpoint(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!midpoints.TryGetValue(key, out var index))
            {
                index = points.Count;
                points.Add(((points[a] + points[b]) * 0.5).Normalized());
                midpoints[key] = index;
            }

            return index;
        }

        for (var f = 0; f < faces.Length; f += 3)
        {
            int a = faces[f], b = faces[f + 1], c = faces[f + 2];
            int ab = Midpoint(a, b), bc = Midpoint(b, c), ca = Midpoint(c, a);
            ReadOnlySpan<int> four = [a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca];
            four.CopyTo(result.AsSpan(k));
            k += four.Length;
        }

        return result;
    }

    /// <summary>Double-precision point so repeated midpoint normalisation does not accumulate float error.</summary>
    private readonly record struct Vector3d(double X, double Y, double Z)
    {
        public static Vector3d operator +(Vector3d a, Vector3d b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3d operator *(Vector3d a, double s) => new(a.X * s, a.Y * s, a.Z * s);

        public Vector3d Normalized()
        {
            var length = Math.Sqrt(X * X + Y * Y + Z * Z);
            return new Vector3d(X / length, Y / length, Z / length);
        }
    }
}
