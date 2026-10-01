using ParseStl.Model;

namespace ParseStl.Processing;

/// <summary>Merges duplicate vertices of a triangle soup into an indexed mesh.</summary>
public interface IVertexWelder
{
    Mesh Weld(IReadOnlyList<StlFacet> facets);
}
