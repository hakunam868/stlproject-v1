using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using ParseStl.Model;

namespace ParseStl.Parsing;

public sealed class BinaryStlParser : IStlParser
{
    private const int FacetsPerChunk = 8192;

    public StlFormat Format => StlFormat.Binary;

    public StlDocument Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Span<byte> preamble = stackalloc byte[BinaryStlLayout.PreambleSize];
        if (stream.ReadAtLeast(preamble, preamble.Length, throwOnEndOfStream: false) < preamble.Length)
            throw new StlParseException($"Binary STL is too short: it needs at least {BinaryStlLayout.PreambleSize} bytes for the header and facet count.");

        var name = DecodeHeader(preamble[..BinaryStlLayout.HeaderSize]);
        var facetCount = BinaryPrimitives.ReadUInt32LittleEndian(preamble[BinaryStlLayout.HeaderSize..]);

        if (facetCount == 0)
            throw new StlParseException("Binary STL declares zero facets.");
        if (facetCount > Array.MaxLength)
            throw new StlParseException($"Binary STL declares {facetCount:N0} facets, which is more than this application supports.");

        if (stream.CanSeek)
        {
            var available = stream.Length - stream.Position;
            var required = (long)facetCount * BinaryStlLayout.FacetSize;
            if (available < required)
                throw new StlParseException(
                    $"Binary STL is truncated: the header declares {facetCount:N0} facets ({required:N0} bytes) but only {available:N0} bytes follow.");
        }

        var facets = new StlFacet[facetCount];
        var buffer = ArrayPool<byte>.Shared.Rent(FacetsPerChunk * BinaryStlLayout.FacetSize);
        try
        {
            var done = 0;
            while (done < facets.Length)
            {
                var batch = Math.Min(FacetsPerChunk, facets.Length - done);
                var chunk = buffer.AsSpan(0, batch * BinaryStlLayout.FacetSize);
                if (stream.ReadAtLeast(chunk, chunk.Length, throwOnEndOfStream: false) < chunk.Length)
                    throw new StlParseException($"Binary STL ended unexpectedly after {done:N0} of {facetCount:N0} facets.");

                for (var i = 0; i < batch; i++)
                    facets[done + i] = ReadFacet(chunk.Slice(i * BinaryStlLayout.FacetSize, BinaryStlLayout.FacetSize), done + i);

                done += batch;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return new StlDocument(name, StlFormat.Binary, facets);
    }

    private static StlFacet ReadFacet(ReadOnlySpan<byte> s, int index)
    {
        // Bytes 48-49 (attribute byte count) are ignored.
        var facet = new StlFacet(ReadVector(s), ReadVector(s[12..]), ReadVector(s[24..]), ReadVector(s[36..]));
        if (!IsFinite(facet.V1) || !IsFinite(facet.V2) || !IsFinite(facet.V3))
            throw new StlParseException($"Facet {index:N0} has a NaN or infinite vertex coordinate.");
        return facet;
    }

    private static Vector3 ReadVector(ReadOnlySpan<byte> s) => new(
        BinaryPrimitives.ReadSingleLittleEndian(s),
        BinaryPrimitives.ReadSingleLittleEndian(s[4..]),
        BinaryPrimitives.ReadSingleLittleEndian(s[8..]));

    private static bool IsFinite(Vector3 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    private static string DecodeHeader(ReadOnlySpan<byte> header)
    {
        var text = Encoding.ASCII.GetString(header);
        var cleaned = new string(text.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        return cleaned.Length == 0 ? "(no header text)" : cleaned;
    }
}
