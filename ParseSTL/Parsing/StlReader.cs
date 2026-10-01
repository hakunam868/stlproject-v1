using ParseStl.Model;

namespace ParseStl.Parsing;

/// <summary>Opens a file, detects its encoding and delegates to the matching parser.</summary>
public sealed class StlReader : IStlReader
{
    private readonly IStlFormatDetector _detector;
    private readonly IReadOnlyDictionary<StlFormat, IStlParser> _parsers;

    public StlReader(IStlFormatDetector detector, IEnumerable<IStlParser> parsers)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        ArgumentNullException.ThrowIfNull(parsers);
        _parsers = parsers.ToDictionary(p => p.Format);
    }

    public StlDocument Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                          bufferSize: 1 << 16, FileOptions.SequentialScan);

        var format = _detector.Detect(stream);
        if (!_parsers.TryGetValue(format, out var parser))
            throw new StlParseException($"No parser is registered for {format} STL.");

        return parser.Parse(stream);
    }
}
