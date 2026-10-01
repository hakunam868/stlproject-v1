using System.Numerics;
using System.Text;
using ParseStl.Model;
using ParseStl.Parsing;

namespace ParseStl.Tests;

public class StlFormatDetectorTests
{
    private readonly StlFormatDetector _detector = new();

    [Fact]
    public void Detects_ascii()
    {
        using var stream = TestData.Ascii(TestData.Cube());
        Assert.Equal(StlFormat.Ascii, _detector.Detect(stream));
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void Detects_binary()
    {
        using var stream = TestData.Binary(TestData.Cube());
        Assert.Equal(StlFormat.Binary, _detector.Detect(stream));
    }

    [Fact]
    public void Binary_whose_header_starts_with_solid_is_detected_as_binary()
    {
        using var stream = TestData.Binary(TestData.Cube(), header: "solid exported by some CAD tool");
        Assert.Equal(StlFormat.Binary, _detector.Detect(stream));
    }

    [Fact]
    public void Empty_stream_throws()
    {
        Assert.Throws<StlParseException>(() => _detector.Detect(new MemoryStream()));
    }
}

public class AsciiStlParserTests
{
    private readonly AsciiStlParser _parser = new();

    [Fact]
    public void Parses_all_facets_and_name()
    {
        var cube = TestData.Cube(2.5f);
        var doc = _parser.Parse(TestData.Ascii(cube, "my cube"));

        Assert.Equal("my cube", doc.Name);
        Assert.Equal(StlFormat.Ascii, doc.Format);
        Assert.Equal(cube, doc.Facets);
    }

    [Fact]
    public void Accepts_mixed_case_keywords_scientific_notation_and_tabs()
    {
        const string text = "SOLID x\n\tFACET NORMAL 0 0 1\n\t\tOuter Loop\n vertex 0 0 0\nVERTEX 1.0E+00 0 0\nvertex 0 1e0 0\nendloop\nEndFacet\nendsolid";
        var doc = _parser.Parse(new MemoryStream(Encoding.ASCII.GetBytes(text)));

        var facet = Assert.Single(doc.Facets);
        Assert.Equal(new Vector3(1, 0, 0), facet.V2);
        Assert.Equal(new Vector3(0, 0, 1), facet.Normal);
    }

    [Theory]
    [InlineData("solid x\nfacet normal 0 0 1\nouter loop\nvertex 0 0 0\nvertex 1 0 0\nendloop\nendfacet\nendsolid", "3 are required")]
    [InlineData("solid x\nfacet normal 0 0 1\nouter loop\nvertex 0 0 abc\n", "line 4")]
    [InlineData("solid x\nfacet normal 0 0 1\nouter loop\nvertex 0 0 0\nvertex 1 0 0\nvertex 0 1 0\nendloop\n", "middle of a facet")]
    [InlineData("solid x\nendsolid x\n", "no facets")]
    [InlineData("solid x\nbogus 1 2 3\n", "unexpected keyword")]
    public void Malformed_input_throws_descriptive_error(string text, string expectedFragment)
    {
        var ex = Assert.Throws<StlParseException>(() => _parser.Parse(new MemoryStream(Encoding.ASCII.GetBytes(text))));
        Assert.Contains(expectedFragment, ex.Message);
    }
}

public class BinaryStlParserTests
{
    private readonly BinaryStlParser _parser = new();

    [Fact]
    public void Round_trips_facets_and_header()
    {
        var cube = TestData.Cube(3);
        var doc = _parser.Parse(TestData.Binary(cube, "hello"));

        Assert.Equal("hello", doc.Name);
        Assert.Equal(StlFormat.Binary, doc.Format);
        Assert.Equal(cube, doc.Facets);
    }

    [Fact]
    public void Truncated_file_throws()
    {
        var ex = Assert.Throws<StlParseException>(() => _parser.Parse(TestData.Binary(TestData.Cube(), declaredCount: 13)));
        Assert.Contains("truncated", ex.Message);
    }

    [Fact]
    public void Non_finite_vertex_throws()
    {
        var bad = new StlFacet(Vector3.Zero, new Vector3(float.NaN, 0, 0), Vector3.UnitX, Vector3.UnitY);
        Assert.Throws<StlParseException>(() => _parser.Parse(TestData.Binary([bad])));
    }
}
