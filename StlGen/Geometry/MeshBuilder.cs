using System.Numerics;
using System.Runtime.InteropServices;

namespace StlGen.Geometry;

/// <summary>Indexed triangle mesh produced by a shape generator. Triangles wind counter-clockwise seen from outside.</summary>
public sealed record MeshData(Vector3[] Vertices, int[] Indices)
{
    public int VertexCount => Vertices.Length;

    public int TriangleCount => Indices.Length / 3;
}

public sealed class GenerationException(string message) : Exception(message);

/// <summary>Collects vertices and triangles, and enforces the configured triangle limit.</summary>
public sealed class MeshBuilder(long maxTriangles)
{
    private readonly List<Vector3> _vertices = [];
    private readonly List<int> _indices = [];
    private Dictionary<Vector3, int>? _shared;

    public int VertexCount => _vertices.Count;

    public long TriangleCount => _indices.Count / 3;

    public int AddVertex(double x, double y, double z) => AddVertex(new Vector3((float)x, (float)y, (float)z));

    public int AddVertex(Vector3 position)
    {
        _vertices.Add(position);
        return _vertices.Count - 1;
    }

    /// <summary>Returns the existing index when the exact same position was already added through this method.</summary>
    public int AddSharedVertex(Vector3 position)
    {
        _shared ??= [];
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(_shared, position, out var exists);
        if (!exists)
            slot = AddVertex(position);
        return slot;
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (TriangleCount >= maxTriangles)
            throw new GenerationException(
                $"The mesh would have more than {maxTriangles:N0} triangles (MaxTriangles in Config.json). Lower the density or the resolution parameters.");

        _indices.Add(a);
        _indices.Add(b);
        _indices.Add(c);
    }

    public Vector3 GetVertex(int index) => _vertices[index];

    public MeshData Build() => new([.. _vertices], [.. _indices]);
}
