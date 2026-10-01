using System.Numerics;
using ParseStl.Analysis;
using ParseStl.Configuration;
using ParseStl.Model;
using ParseStl.Parsing;
using ParseStl.Processing;
using StlGen.Configuration;
using StlGen.Geometry;
using StlGen.Output;
using StlGen.Services;
using StlGen.Shapes;

namespace ParseStl.Tests;

/// <summary>Round-trips StlGen output through the ParseSTL parsers, welder and topology analyzer.</summary>
public class GeneratorTests
{
    private static readonly IShapeGenerator[] Shapes =
    [
        new BoxGenerator(), new CylinderGenerator(), new ConeGenerator(), new PrismGenerator(),
        new SphereGenerator(), new IcosphereGenerator(), new TorusGenerator(), new TubeGenerator(),
    ];

    private static readonly GenerationService Service =
        new([new BinaryStlWriter(), new AsciiStlWriter()], new GeneratorSettings { MaxTriangles = 5_000_000 });

    public static TheoryData<string, double> ShapeAndDensity()
    {
        var data = new TheoryData<string, double>();
        foreach (var shape in Shapes)
        foreach (var density in new[] { 1.0, 3.0 })
            data.Add(shape.Name, density);
        return data;
    }

    private static IShapeGenerator Shape(string name) => Shapes.Single(s => s.Name == name);

    private static MeshData Generate(string shape, double density = 1, Dictionary<string, double>? overrides = null)
    {
        var s = Shape(shape);
        return Service.Build(s, Service.Resolve(s, overrides ?? [], density));
    }

    private static Mesh RoundTrip(MeshData data, IStlWriter writer, IStlParser parser)
    {
        using var stream = new MemoryStream();
        writer.Write(data, "test", stream);
        stream.Position = 0;
        Assert.Equal(parser.Format, new StlFormatDetector().Detect(stream));
        return new VertexWelder(new WeldingSettings()).Weld(parser.Parse(stream).Facets);
    }

    private static double SignedVolume(Mesh mesh)
    {
        var v = mesh.Vertices;
        var idx = mesh.Indices;
        double volume = 0;
        for (var i = 0; i < idx.Length; i += 3)
            volume += Vector3.Dot(v[idx[i]], Vector3.Cross(v[idx[i + 1]], v[idx[i + 2]])) / 6.0;
        return volume;
    }

    [Theory]
    [MemberData(nameof(ShapeAndDensity))]
    public void Binary_round_trip_is_watertight_outward_and_has_correct_euler_characteristic(string shape, double density)
    {
        var data = Generate(shape, density);
        var mesh = RoundTrip(data, new BinaryStlWriter(), new BinaryStlParser());

        // Welding the parsed file gives back exactly the generator's vertices.
        Assert.Equal(data.TriangleCount, mesh.TriangleCount);
        Assert.Equal(data.VertexCount, mesh.VertexCount);

        var topology = new EdgeTopologyAnalyzer().Analyze(mesh);
        Assert.True(topology.IsWatertight, $"{shape}: {topology.BoundaryEdgeCount} boundary, {topology.NonManifoldEdgeCount} non-manifold edges");

        var expectedEuler = shape is "torus" or "tube" ? 0 : 2; // genus 1 vs genus 0
        Assert.Equal(expectedEuler, mesh.VertexCount - topology.UniqueEdgeCount + mesh.TriangleCount);

        Assert.True(SignedVolume(mesh) > 0, $"{shape}: normals point inwards");
    }

    [Theory]
    [MemberData(nameof(ShapeAndDensity))]
    public void Ascii_round_trip_matches_binary(string shape, double density)
    {
        var data = Generate(shape, density);
        var ascii = RoundTrip(data, new AsciiStlWriter(), new AsciiStlParser());
        var binary = RoundTrip(data, new BinaryStlWriter(), new BinaryStlParser());

        // G9 text is exact, so the welded meshes are identical (same vertex order and same indices).
        Assert.Equal(data.VertexCount, ascii.VertexCount);
        Assert.Equal(binary.Vertices.ToArray(), ascii.Vertices.ToArray());
        Assert.Equal(binary.Indices.ToArray(), ascii.Indices.ToArray());
    }

    [Theory]
    [InlineData("box", 10 * 10 * 10)]
    [InlineData("cylinder", Math.PI * 25 * 20)]
    [InlineData("cone", Math.PI * 25 * 10 / 3)]
    [InlineData("sphere", 4.0 / 3 * Math.PI * 1000)]
    [InlineData("icosphere", 4.0 / 3 * Math.PI * 1000)]
    [InlineData("torus", 2 * Math.PI * Math.PI * 10 * 9)]
    [InlineData("tube", Math.PI * (100 - 49) * 20)]
    public void High_density_volume_approaches_analytic_volume(string shape, double expected)
    {
        var data = Generate(shape, density: 8);
        var mesh = new Mesh(data.Vertices, data.Indices);

        Assert.Equal(1.0, SignedVolume(mesh) / expected, 2); // within 0.5%
    }

    [Fact]
    public void Prism_volume_is_exact()
    {
        var data = Generate("prism", overrides: new() { ["sides"] = 6, ["radius"] = 2, ["height"] = 3 });
        var expected = 6 / 2.0 * 4 * Math.Sin(2 * Math.PI / 6) * 3;

        Assert.Equal(expected, SignedVolume(new Mesh(data.Vertices, data.Indices)), 3);
    }

    [Theory]
    [InlineData(32, 1, 1, 32 * 2 + 2 * 32)]               // side quads + two fan caps
    [InlineData(32, 4, 3, 32 * 2 * 4 + 2 * 32 * 5)]       // cap with 3 rings = n(2c - 1) triangles
    [InlineData(16, 2, 0, 16 * 2 * 2)]                    // open tube, no caps
    public void Cylinder_triangle_count(int segments, int stacks, int capRings, int expected)
    {
        var data = Generate("cylinder", overrides: new() { ["segments"] = segments, ["stacks"] = stacks, ["capRings"] = capRings });
        Assert.Equal(expected, data.TriangleCount);
    }

    [Fact]
    public void Open_cylinder_has_boundary_edges()
    {
        var data = Generate("cylinder", overrides: new() { ["segments"] = 16, ["capRings"] = 0 });
        var topology = new EdgeTopologyAnalyzer().Analyze(new Mesh(data.Vertices, data.Indices));

        Assert.Equal(32, topology.BoundaryEdgeCount);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 80)]
    [InlineData(3, 1280)]
    public void Icosphere_has_20_times_4_to_the_n_faces(int subdivisions, int expected)
    {
        Assert.Equal(expected, Generate("icosphere", overrides: new() { ["subdivisions"] = subdivisions }).TriangleCount);
    }

    [Fact]
    public void Density_scales_resolution_parameters_only()
    {
        var p = Service.Resolve(Shape("cylinder"), new Dictionary<string, double> { ["segments"] = 10, ["radius"] = 3 }, 4);

        Assert.Equal(40, p["segments"]);
        Assert.Equal(4, p["stacks"]);
        Assert.Equal(3, p["radius"]);

        var ico = Service.Resolve(Shape("icosphere"), new Dictionary<string, double>(), 4);
        Assert.Equal(4, ico["subdivisions"]); // default 2 + log2(4)
    }

    [Fact]
    public void Invalid_parameters_are_rejected()
    {
        Assert.Throws<GenerationException>(() => Service.Resolve(Shape("torus"), new Dictionary<string, double> { ["minorRadius"] = 20 }, 1));
        Assert.Throws<GenerationException>(() => Service.Resolve(Shape("cylinder"), new Dictionary<string, double> { ["segments"] = 2 }, 1));
        Assert.Throws<GenerationException>(() => Service.Resolve(Shape("cylinder"), new Dictionary<string, double> { ["bogus"] = 1 }, 1));
    }

    [Fact]
    public void Triangle_limit_is_enforced()
    {
        var small = new GenerationService([new BinaryStlWriter()], new GeneratorSettings { MaxTriangles = 100 });
        var shape = Shape("sphere");

        var ex = Assert.Throws<GenerationException>(() => small.Build(shape, small.Resolve(shape, new Dictionary<string, double>(), 1)));
        Assert.Contains("MaxTriangles", ex.Message);
    }

    [Fact]
    public void Uv_sphere_poles_are_high_valence_while_icosphere_is_not()
    {
        var analyzer = new EdgeTopologyAnalyzer();
        var uv = Generate("sphere", overrides: new() { ["segments"] = 64 });
        var ico = Generate("icosphere");

        Assert.Equal(64, analyzer.Analyze(new Mesh(uv.Vertices, uv.Indices)).MaxValence);
        Assert.Equal(6, analyzer.Analyze(new Mesh(ico.Vertices, ico.Indices)).MaxValence);
    }
}
