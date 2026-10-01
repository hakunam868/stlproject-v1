using System.Numerics;

namespace ParseStl.Model;

public enum StlFormat
{
    Ascii,
    Binary,
}

/// <summary>One facet exactly as stored in the file: a normal and three corner positions (STL "triangle soup").</summary>
public readonly record struct StlFacet(Vector3 Normal, Vector3 V1, Vector3 V2, Vector3 V3);

/// <summary>Raw result of parsing an STL file, before duplicate vertices are merged.</summary>
public sealed record StlDocument(string Name, StlFormat Format, IReadOnlyList<StlFacet> Facets);
