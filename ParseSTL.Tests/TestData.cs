using System.Globalization;
using System.Numerics;
using System.Text;
using ParseStl.Model;

namespace ParseStl.Tests;

internal static class TestData
{
    /// <summary>Unit cube as 12 facets (36 corners, 8 unique positions).</summary>
    public static List<StlFacet> Cube(float s = 1)
    {
        var p = new Vector3[8];
        for (var i = 0; i < 8; i++)
            p[i] = new Vector3((i & 1) * s, ((i >> 1) & 1) * s, ((i >> 2) & 1) * s);

        int[][] quads = [[0, 2, 3, 1], [4, 5, 7, 6], [0, 1, 5, 4], [2, 6, 7, 3], [0, 4, 6, 2], [1, 3, 7, 5]];
        var facets = new List<StlFacet>();
        foreach (var q in quads)
        {
            facets.Add(new StlFacet(Vector3.Zero, p[q[0]], p[q[1]], p[q[2]]));
            facets.Add(new StlFacet(Vector3.Zero, p[q[0]], p[q[2]], p[q[3]]));
        }

        return facets;
    }

    public static MemoryStream Ascii(IEnumerable<StlFacet> facets, string name = "test")
    {
        static string V(Vector3 v) => string.Create(CultureInfo.InvariantCulture, $"{v.X:R} {v.Y:R} {v.Z:R}");

        var sb = new StringBuilder($"solid {name}\n");
        foreach (var f in facets)
            sb.Append($"facet normal {V(f.Normal)}\n outer loop\n  vertex {V(f.V1)}\n  vertex {V(f.V2)}\n  vertex {V(f.V3)}\n endloop\nendfacet\n");
        sb.Append($"endsolid {name}\n");
        return new MemoryStream(Encoding.ASCII.GetBytes(sb.ToString()));
    }

    public static MemoryStream Binary(IReadOnlyCollection<StlFacet> facets, string header = "binary test", uint? declaredCount = null)
    {
        var stream = new MemoryStream();
        using (var w = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            var h = new byte[80];
            Encoding.ASCII.GetBytes(header, 0, Math.Min(80, header.Length), h, 0);
            w.Write(h);
            w.Write(declaredCount ?? (uint)facets.Count);
            foreach (var f in facets)
            {
                foreach (var v in new[] { f.Normal, f.V1, f.V2, f.V3 })
                {
                    w.Write(v.X);
                    w.Write(v.Y);
                    w.Write(v.Z);
                }

                w.Write((ushort)0);
            }
        }

        stream.Position = 0;
        return stream;
    }
}
