using System.Numerics;

namespace ParseStl.Model;

/// <summary>Indexed triangle mesh: unique vertex positions plus three vertex indices per triangle.</summary>
public sealed class Mesh
{
    private readonly Vector3[] _vertices;
    private readonly int[] _indices;

    public Mesh(Vector3[] vertices, int[] indices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        if (indices.Length % 3 != 0)
            throw new ArgumentException("Index count must be a multiple of 3.", nameof(indices));

        _vertices = vertices;
        _indices = indices;
    }

    public ReadOnlySpan<Vector3> Vertices => _vertices;

    /// <summary>Flat index buffer: triangle <c>t</c> uses entries <c>3t</c>, <c>3t+1</c> and <c>3t+2</c>.</summary>
    public ReadOnlySpan<int> Indices => _indices;

    public int VertexCount => _vertices.Length;

    public int TriangleCount => _indices.Length / 3;

    public (int A, int B, int C) GetTriangle(int triangle)
    {
        var i = triangle * 3;
        return (_indices[i], _indices[i + 1], _indices[i + 2]);
    }
}
