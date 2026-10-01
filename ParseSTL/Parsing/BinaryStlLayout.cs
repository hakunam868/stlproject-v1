namespace ParseStl.Parsing;

/// <summary>
/// Binary STL layout: 80-byte header, uint32 facet count, then 50 bytes per facet
/// (normal + 3 vertices as 12 little-endian float32 values, followed by a uint16 attribute byte count).
/// </summary>
internal static class BinaryStlLayout
{
    public const int HeaderSize = 80;
    public const int PreambleSize = HeaderSize + sizeof(uint);
    public const int FacetSize = 50;

    public static long ExpectedLength(uint facetCount) => PreambleSize + (long)FacetSize * facetCount;
}
