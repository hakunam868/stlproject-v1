namespace ParseStl.Model;

/// <summary>A parsed and welded STL file together with metadata about how it was loaded.</summary>
public sealed record LoadedModel(
    string FilePath,
    long FileSizeBytes,
    string SolidName,
    StlFormat Format,
    Mesh Mesh,
    int RawVertexCount,
    TimeSpan ParseTime,
    TimeSpan WeldTime)
{
    public string FileName => Path.GetFileName(FilePath);

    public int DuplicateVerticesRemoved => RawVertexCount - Mesh.VertexCount;
}
