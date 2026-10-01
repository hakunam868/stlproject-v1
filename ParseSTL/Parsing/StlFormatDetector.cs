using System.Buffers.Binary;
using ParseStl.Model;

namespace ParseStl.Parsing;

/// <summary>
/// Detects the STL encoding. Many binary exporters write "solid" at the start of the 80-byte header,
/// so the keyword alone is not trusted:
///  1. If the file length equals 84 + 50 * (facet count in the header), the file is binary.
///  2. Otherwise, if it starts with "solid" and the first bytes are printable text, it is ASCII.
///  3. Otherwise it is treated as binary (the binary parser reports truncation if needed).
/// </summary>
public sealed class StlFormatDetector : IStlFormatDetector
{
    private const int SniffLength = 512;

    public StlFormat Detect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
            throw new ArgumentException("Format detection requires a seekable stream.", nameof(stream));

        var start = stream.Position;
        try
        {
            var length = stream.Length - start;
            Span<byte> buffer = stackalloc byte[SniffLength];
            var read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            var head = buffer[..read];

            if (read == 0)
                throw new StlParseException("The file is empty.");

            if (read >= BinaryStlLayout.HeaderSize + sizeof(uint))
            {
                var facetCount = BinaryPrimitives.ReadUInt32LittleEndian(head[BinaryStlLayout.HeaderSize..]);
                if (BinaryStlLayout.ExpectedLength(facetCount) == length)
                    return StlFormat.Binary;
            }

            return StartsWithSolid(head) && IsPrintableText(head) ? StlFormat.Ascii : StlFormat.Binary;
        }
        finally
        {
            stream.Position = start;
        }
    }

    private static bool StartsWithSolid(ReadOnlySpan<byte> head)
    {
        var i = 0;
        while (i < head.Length && IsWhiteSpace(head[i]))
            i++;

        return head[i..].StartsWith("solid"u8);
    }

    private static bool IsPrintableText(ReadOnlySpan<byte> head)
    {
        foreach (var b in head)
        {
            if (b is < 0x20 or > 0x7E && !IsWhiteSpace(b))
                return false;
        }

        return true;
    }

    private static bool IsWhiteSpace(byte b) => b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';
}
