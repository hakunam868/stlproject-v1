using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;
using StlGen.Geometry;

namespace StlGen.Output;

public enum StlOutputFormat
{
    Binary,
    Ascii,
}

public interface IStlWriter
{
    StlOutputFormat Format { get; }

    void Write(MeshData mesh, string solidName, Stream stream);
}

internal static class FacetNormal
{
    /// <summary>Unit normal from the winding (right-hand rule); zero for a degenerate triangle.</summary>
    public static Vector3 Of(Vector3 a, Vector3 b, Vector3 c)
    {
        var n = Vector3.Cross(b - a, c - a);
        var length = n.Length();
        return length > 0 ? n / length : Vector3.Zero;
    }
}

public sealed class BinaryStlWriter : IStlWriter
{
    private const int FacetSize = 50;
    private const int FacetsPerChunk = 8192;

    public StlOutputFormat Format => StlOutputFormat.Binary;

    public void Write(MeshData mesh, string solidName, Stream stream)
    {
        // The header must not start with "solid", or some readers mistake the file for ASCII.
        Span<byte> preamble = stackalloc byte[84];
        preamble.Clear();
        var header = $"StlGen binary: {solidName}";
        Encoding.ASCII.GetBytes(header.AsSpan(0, Math.Min(header.Length, 80)), preamble);
        BinaryPrimitives.WriteUInt32LittleEndian(preamble[80..], (uint)mesh.TriangleCount);
        stream.Write(preamble);

        var buffer = ArrayPool<byte>.Shared.Rent(FacetsPerChunk * FacetSize);
        try
        {
            var v = mesh.Vertices;
            var idx = mesh.Indices;
            var written = 0;
            while (written < mesh.TriangleCount)
            {
                var batch = Math.Min(FacetsPerChunk, mesh.TriangleCount - written);
                var span = buffer.AsSpan(0, batch * FacetSize);
                for (var i = 0; i < batch; i++)
                {
                    var t = (written + i) * 3;
                    Vector3 a = v[idx[t]], b = v[idx[t + 1]], c = v[idx[t + 2]];
                    var facet = span.Slice(i * FacetSize, FacetSize);
                    WriteVector(facet, FacetNormal.Of(a, b, c));
                    WriteVector(facet[12..], a);
                    WriteVector(facet[24..], b);
                    WriteVector(facet[36..], c);
                    BinaryPrimitives.WriteUInt16LittleEndian(facet[48..], 0);
                }

                stream.Write(span);
                written += batch;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void WriteVector(Span<byte> s, Vector3 v)
    {
        BinaryPrimitives.WriteSingleLittleEndian(s, v.X);
        BinaryPrimitives.WriteSingleLittleEndian(s[4..], v.Y);
        BinaryPrimitives.WriteSingleLittleEndian(s[8..], v.Z);
    }
}

public sealed class AsciiStlWriter : IStlWriter
{
    public StlOutputFormat Format => StlOutputFormat.Ascii;

    public void Write(MeshData mesh, string solidName, Stream stream)
    {
        var name = new string(solidName.Select(ch => char.IsControl(ch) ? ' ' : ch).ToArray());
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 1 << 16, leaveOpen: true);
        writer.NewLine = "\n";

        writer.WriteLine($"solid {name}");
        var v = mesh.Vertices;
        var idx = mesh.Indices;
        for (var t = 0; t < idx.Length; t += 3)
        {
            Vector3 a = v[idx[t]], b = v[idx[t + 1]], c = v[idx[t + 2]];
            writer.WriteLine($"  facet normal {Format3(FacetNormal.Of(a, b, c))}");
            writer.WriteLine("    outer loop");
            writer.WriteLine($"      vertex {Format3(a)}");
            writer.WriteLine($"      vertex {Format3(b)}");
            writer.WriteLine($"      vertex {Format3(c)}");
            writer.WriteLine("    endloop");
            writer.WriteLine("  endfacet");
        }

        writer.WriteLine($"endsolid {name}");
    }

    // G9 round-trips every float exactly, so vertices shared between facets still weld exactly after parsing.
    private static string Format3(Vector3 v) => string.Create(CultureInfo.InvariantCulture, $"{v.X:G9} {v.Y:G9} {v.Z:G9}");
}
