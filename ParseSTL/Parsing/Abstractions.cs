using ParseStl.Model;

namespace ParseStl.Parsing;

/// <summary>Decides whether a stream holds ASCII or binary STL.</summary>
public interface IStlFormatDetector
{
    /// <summary>Inspects a seekable stream and restores its position before returning.</summary>
    StlFormat Detect(Stream stream);
}

/// <summary>Parses one STL encoding into raw facets.</summary>
public interface IStlParser
{
    StlFormat Format { get; }

    StlDocument Parse(Stream stream);
}

/// <summary>Reads an STL file from disk regardless of its encoding.</summary>
public interface IStlReader
{
    StlDocument Read(string path);
}

public sealed class StlParseException(string message, Exception? inner = null) : Exception(message, inner);
