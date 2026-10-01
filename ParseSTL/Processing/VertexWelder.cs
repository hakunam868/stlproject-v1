using System.Numerics;
using System.Runtime.InteropServices;
using ParseStl.Configuration;
using ParseStl.Model;

namespace ParseStl.Processing;

/// <summary>
/// Hash-based welding in a single O(n) pass over all 3·F corners. Each position becomes an integer key and
/// one dictionary lookup either returns the existing index or appends a new vertex.
/// <list type="bullet">
/// <item>Tolerance 0: the key is the float bit pattern (with -0 folded onto +0), so only identical positions merge.</item>
/// <item>Tolerance &gt; 0: the key is the position snapped to a grid of that cell size. Two points closer than the
/// tolerance can still land in neighbouring cells, so this is a fast approximation, not an exact epsilon weld.</item>
/// </list>
/// </summary>
public sealed class VertexWelder(WeldingSettings settings) : IVertexWelder
{
    private readonly double _tolerance = settings.Tolerance;

    public Mesh Weld(IReadOnlyList<StlFacet> facets)
    {
        ArgumentNullException.ThrowIfNull(facets);

        // A closed triangle mesh has about F/2 unique vertices; sizing up front avoids rehashing.
        var expectedVertices = facets.Count / 2 + 3;
        var lookup = new Dictionary<VertexKey, int>(expectedVertices);
        var vertices = new List<Vector3>(expectedVertices);
        var indices = new int[checked(facets.Count * 3)];

        var k = 0;
        for (var i = 0; i < facets.Count; i++)
        {
            var f = facets[i];
            indices[k++] = GetOrAdd(f.V1, lookup, vertices);
            indices[k++] = GetOrAdd(f.V2, lookup, vertices);
            indices[k++] = GetOrAdd(f.V3, lookup, vertices);
        }

        return new Mesh(vertices.ToArray(), indices);
    }

    private int GetOrAdd(Vector3 position, Dictionary<VertexKey, int> lookup, List<Vector3> vertices)
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(lookup, MakeKey(position), out var exists);
        if (!exists)
        {
            slot = vertices.Count;
            vertices.Add(position);
        }

        return slot;
    }

    private VertexKey MakeKey(Vector3 p) => _tolerance > 0
        ? new VertexKey(Snap(p.X), Snap(p.Y), Snap(p.Z))
        : new VertexKey(Bits(p.X), Bits(p.Y), Bits(p.Z));

    private long Snap(float value) => (long)Math.Floor(value / _tolerance);

    private static long Bits(float value) => value == 0f ? 0 : BitConverter.SingleToInt32Bits(value);

    private readonly record struct VertexKey(long X, long Y, long Z);
}
