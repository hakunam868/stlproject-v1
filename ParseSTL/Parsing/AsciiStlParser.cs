using System.Globalization;
using System.Numerics;
using System.Text;
using ParseStl.Model;

namespace ParseStl.Parsing;

/// <summary>
/// Parses ASCII STL:
/// <code>
/// solid name
///   facet normal ni nj nk
///     outer loop
///       vertex x y z   (x3)
///     endloop
///   endfacet
/// endsolid name
/// </code>
/// Keywords are matched case-insensitively and several solids in one file are accepted.
/// </summary>
public sealed class AsciiStlParser : IStlParser
{
    public StlFormat Format => StlFormat.Ascii;

    public StlDocument Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true, bufferSize: 1 << 16, leaveOpen: true);

        var facets = new List<StlFacet>();
        string? name = null;
        var lineNumber = 0;

        var inFacet = false;
        var inLoop = false;
        var normal = Vector3.Zero;
        Span<Vector3> corners = stackalloc Vector3[3];
        var cornerCount = 0;

        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            var tokens = new Tokenizer(line);
            if (!tokens.TryNext(out var keyword))
                continue;

            if (Is(keyword, "vertex"))
            {
                if (!inLoop)
                    throw Error(lineNumber, "'vertex' outside of an 'outer loop'.");
                if (cornerCount == 3)
                    throw Error(lineNumber, "facet has more than 3 vertices.");
                corners[cornerCount++] = ReadVector(ref tokens, lineNumber, requireFinite: true);
            }
            else if (Is(keyword, "facet"))
            {
                if (inFacet)
                    throw Error(lineNumber, "'facet' found before the previous 'endfacet'.");
                if (!tokens.TryNext(out var normalKeyword) || !Is(normalKeyword, "normal"))
                    throw Error(lineNumber, "expected 'facet normal ni nj nk'.");
                normal = ReadVector(ref tokens, lineNumber, requireFinite: false);
                inFacet = true;
                cornerCount = 0;
            }
            else if (Is(keyword, "outer"))
            {
                if (!inFacet || inLoop)
                    throw Error(lineNumber, "'outer loop' must appear once inside a facet.");
                if (!tokens.TryNext(out var loopKeyword) || !Is(loopKeyword, "loop"))
                    throw Error(lineNumber, "expected 'outer loop'.");
                inLoop = true;
            }
            else if (Is(keyword, "endloop"))
            {
                if (!inLoop)
                    throw Error(lineNumber, "'endloop' without 'outer loop'.");
                inLoop = false;
            }
            else if (Is(keyword, "endfacet"))
            {
                if (!inFacet || inLoop)
                    throw Error(lineNumber, "'endfacet' without a matching 'facet' / 'endloop'.");
                if (cornerCount != 3)
                    throw Error(lineNumber, $"facet has {cornerCount} vertices; exactly 3 are required.");
                facets.Add(new StlFacet(normal, corners[0], corners[1], corners[2]));
                inFacet = false;
            }
            else if (Is(keyword, "solid"))
            {
                if (inFacet)
                    throw Error(lineNumber, "'solid' found inside a facet.");
                name ??= tokens.Rest().Trim().ToString();
            }
            else if (Is(keyword, "endsolid"))
            {
                if (inFacet)
                    throw Error(lineNumber, "'endsolid' found inside a facet.");
            }
            else
            {
                throw Error(lineNumber, $"unexpected keyword '{keyword}'.");
            }
        }

        if (inFacet)
            throw new StlParseException("ASCII STL ended in the middle of a facet.");
        if (facets.Count == 0)
            throw new StlParseException("ASCII STL contains no facets.");

        return new StlDocument(string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name, StlFormat.Ascii, facets);
    }

    private static Vector3 ReadVector(ref Tokenizer tokens, int lineNumber, bool requireFinite)
    {
        var x = ReadFloat(ref tokens, lineNumber);
        var y = ReadFloat(ref tokens, lineNumber);
        var z = ReadFloat(ref tokens, lineNumber);
        if (requireFinite && !(float.IsFinite(x) && float.IsFinite(y) && float.IsFinite(z)))
            throw Error(lineNumber, "vertex has a NaN or infinite coordinate.");
        return new Vector3(x, y, z);
    }

    private static float ReadFloat(ref Tokenizer tokens, int lineNumber)
    {
        if (!tokens.TryNext(out var token))
            throw Error(lineNumber, "expected three numbers.");
        if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw Error(lineNumber, $"'{token}' is not a valid number.");
        return value;
    }

    private static bool Is(ReadOnlySpan<char> token, string keyword) =>
        token.Equals(keyword, StringComparison.OrdinalIgnoreCase);

    private static StlParseException Error(int lineNumber, string message) =>
        new($"ASCII STL line {lineNumber}: {message}");

    /// <summary>Allocation-free whitespace tokenizer over one line.</summary>
    private ref struct Tokenizer(ReadOnlySpan<char> line)
    {
        private ReadOnlySpan<char> _remaining = line;

        public bool TryNext(out ReadOnlySpan<char> token)
        {
            _remaining = _remaining.TrimStart();
            if (_remaining.IsEmpty)
            {
                token = default;
                return false;
            }

            var end = _remaining.IndexOfAny(' ', '\t');
            if (end < 0)
                end = _remaining.Length;

            token = _remaining[..end];
            _remaining = _remaining[end..];
            return true;
        }

        public readonly ReadOnlySpan<char> Rest() => _remaining;
    }
}
