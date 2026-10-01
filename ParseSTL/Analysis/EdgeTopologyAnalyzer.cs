using System.Runtime.InteropServices;
using ParseStl.Model;

namespace ParseStl.Analysis;

/// <summary>Edge connectivity of a mesh.</summary>
/// <param name="Valence">Number of unique edges incident on each vertex.</param>
/// <param name="UniqueEdgeCount">Number of distinct undirected edges.</param>
/// <param name="BoundaryEdgeCount">Edges used by exactly one triangle (holes or open borders).</param>
/// <param name="NonManifoldEdgeCount">Edges shared by more than two triangles.</param>
public sealed record EdgeTopology(int[] Valence, int UniqueEdgeCount, int BoundaryEdgeCount, int NonManifoldEdgeCount)
{
    public int MaxValence => Valence.Length == 0 ? 0 : Valence.Max();

    public double MeanValence => Valence.Length == 0 ? 0 : Valence.Average();

    public bool IsWatertight => BoundaryEdgeCount == 0 && NonManifoldEdgeCount == 0;
}

public interface IEdgeTopologyAnalyzer
{
    EdgeTopology Analyze(Mesh mesh);
}

/// <summary>
/// Counts how many triangles use each undirected edge, in one pass with a hash map keyed by
/// (min index, max index). A vertex's valence goes up by one the first time each of its edges is seen.
/// </summary>
public sealed class EdgeTopologyAnalyzer : IEdgeTopologyAnalyzer
{
    public EdgeTopology Analyze(Mesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var valence = new int[mesh.VertexCount];
        // By Euler's formula a closed mesh has about 1.5·F edges.
        var edgeUse = new Dictionary<EdgeKey, int>(mesh.TriangleCount * 3 / 2 + 1);
        var indices = mesh.Indices;

        for (var i = 0; i < indices.Length; i += 3)
        {
            AddEdge(indices[i], indices[i + 1], edgeUse, valence);
            AddEdge(indices[i + 1], indices[i + 2], edgeUse, valence);
            AddEdge(indices[i + 2], indices[i], edgeUse, valence);
        }

        int boundary = 0, nonManifold = 0;
        foreach (var uses in edgeUse.Values)
        {
            if (uses == 1)
                boundary++;
            else if (uses > 2)
                nonManifold++;
        }

        return new EdgeTopology(valence, edgeUse.Count, boundary, nonManifold);
    }

    private static void AddEdge(int a, int b, Dictionary<EdgeKey, int> edgeUse, int[] valence)
    {
        if (a == b)
            return; // collapsed edge of a degenerate triangle

        var key = a < b ? new EdgeKey(a, b) : new EdgeKey(b, a);
        ref var uses = ref CollectionsMarshal.GetValueRefOrAddDefault(edgeUse, key, out var exists);
        if (!exists)
        {
            valence[a]++;
            valence[b]++;
        }

        uses++;
    }

    /// <summary>
    /// Undirected edge key. Do not pack the two indices into a long: long.GetHashCode() is (high ^ low),
    /// so neighbouring edges collide heavily and the dictionary degrades towards O(n²).
    /// </summary>
    private readonly record struct EdgeKey(int Lo, int Hi)
    {
        public override int GetHashCode() => HashCode.Combine(Lo, Hi);
    }
}
