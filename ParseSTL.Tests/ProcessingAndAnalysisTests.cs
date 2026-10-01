using System.Numerics;
using ParseStl.Analysis;
using ParseStl.Analysis.Rules;
using ParseStl.Configuration;
using ParseStl.Model;
using ParseStl.Processing;

namespace ParseStl.Tests;

public class VertexWelderTests
{
    [Fact]
    public void Cube_welds_to_8_vertices_and_keeps_12_triangles()
    {
        var mesh = new VertexWelder(new WeldingSettings()).Weld(TestData.Cube());

        Assert.Equal(8, mesh.VertexCount);
        Assert.Equal(12, mesh.TriangleCount);
    }

    [Fact]
    public void Positive_and_negative_zero_are_the_same_vertex()
    {
        var facets = new List<StlFacet>
        {
            new(Vector3.Zero, new Vector3(0f, 0, 0), Vector3.UnitX, Vector3.UnitY),
            new(Vector3.Zero, new Vector3(-0f, 0, 0), Vector3.UnitY, Vector3.UnitZ),
        };

        Assert.Equal(4, new VertexWelder(new WeldingSettings()).Weld(facets).VertexCount);
    }

    [Fact]
    public void Exact_mode_keeps_nearly_equal_vertices_and_tolerance_mode_merges_them()
    {
        var facets = new List<StlFacet>
        {
            new(Vector3.Zero, new Vector3(1.0f, 1, 1), Vector3.UnitX, Vector3.UnitY),
            new(Vector3.Zero, new Vector3(1.00001f, 1, 1), Vector3.UnitX, Vector3.UnitY),
        };

        Assert.Equal(4, new VertexWelder(new WeldingSettings()).Weld(facets).VertexCount);
        Assert.Equal(3, new VertexWelder(new WeldingSettings { Tolerance = 0.001 }).Weld(facets).VertexCount);
    }
}

public class TriangleMetricsCalculatorTests
{
    private readonly TriangleMetricsCalculator _calculator = new();

    [Fact]
    public void Equilateral_triangle()
    {
        var m = _calculator.Calculate(Vector3.Zero, new Vector3(2, 0, 0), new Vector3(1, MathF.Sqrt(3), 0));

        Assert.Equal(60, m.MinAngleDegrees, 3);
        Assert.Equal(2 / Math.Sqrt(3), m.AspectRatio, 4);
        Assert.Equal(Math.Sqrt(3), m.Area, 5);
        Assert.Equal(2, m.LongestEdge, 5);
    }

    [Fact]
    public void Right_isosceles_triangle()
    {
        var m = _calculator.Calculate(Vector3.Zero, Vector3.UnitX, Vector3.UnitY);

        Assert.Equal(45, m.MinAngleDegrees, 4);
        Assert.Equal(0.5, m.Area, 6);
        Assert.Equal(2, m.AspectRatio, 5); // L² / 2A = 2 / 1
    }

    [Fact]
    public void Collinear_points_are_degenerate()
    {
        var m = _calculator.Calculate(Vector3.Zero, Vector3.UnitX, new Vector3(2, 0, 0));

        Assert.Equal(0, m.Area);
        Assert.Equal(0, m.MinAngleDegrees);
        Assert.True(double.IsPositiveInfinity(m.AspectRatio));
    }
}

public class TriangleClassifierTests
{
    private readonly TriangleClassifier _classifier = new(new QualitySettings());

    [Theory]
    [InlineData(1.0, 45, 2, 1, TriangleClassification.Normal)]
    [InlineData(1.0, 4, 10, 1, TriangleClassification.Sliver)]        // angle rule
    [InlineData(1.0, 30, 25, 1, TriangleClassification.Sliver)]       // aspect-ratio rule
    [InlineData(10.0, 45, 2, 30, TriangleClassification.VeryLarge)]   // 30 > 0.25 * 100
    [InlineData(10.0, 1, 80, 30, TriangleClassification.Sliver | TriangleClassification.VeryLarge)]
    [InlineData(0.0, 0, double.PositiveInfinity, 1, TriangleClassification.Degenerate)]
    public void Classifies(double area, double minAngle, double aspect, double longestEdge, TriangleClassification expected)
    {
        var metrics = new TriangleMetrics(area, minAngle, aspect, longestEdge);
        Assert.Equal(expected, _classifier.Classify(metrics, boundingBoxDiagonal: 100, hasRepeatedVertex: false));
    }

    [Fact]
    public void Repeated_vertex_is_degenerate()
    {
        var metrics = new TriangleMetrics(1, 45, 2, 1);
        Assert.Equal(TriangleClassification.Degenerate, _classifier.Classify(metrics, 100, hasRepeatedVertex: true));
    }
}

public class EdgeTopologyAnalyzerTests
{
    [Fact]
    public void Closed_cube_is_watertight_with_18_edges()
    {
        var mesh = new VertexWelder(new WeldingSettings()).Weld(TestData.Cube());
        var topology = new EdgeTopologyAnalyzer().Analyze(mesh);

        Assert.Equal(18, topology.UniqueEdgeCount);
        Assert.True(topology.IsWatertight);
        Assert.Equal(36, topology.Valence.Sum()); // 2 × edges
    }

    [Fact]
    public void Single_triangle_has_three_boundary_edges()
    {
        var mesh = new Mesh([Vector3.Zero, Vector3.UnitX, Vector3.UnitY], [0, 1, 2]);
        var topology = new EdgeTopologyAnalyzer().Analyze(mesh);

        Assert.Equal(3, topology.BoundaryEdgeCount);
        Assert.Equal([2, 2, 2], topology.Valence);
    }

    [Fact]
    public void Edge_shared_by_three_triangles_is_non_manifold()
    {
        Vector3[] v = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ];
        var mesh = new Mesh(v, [0, 1, 2, 1, 0, 3, 0, 1, 4]);

        Assert.Equal(1, new EdgeTopologyAnalyzer().Analyze(mesh).NonManifoldEdgeCount);
    }
}

public class MeshAnalyzerTests
{
    private static MeshAnalyzer CreateAnalyzer(QualitySettings settings) => new(
        new TriangleMetricsCalculator(),
        new TriangleClassifier(settings),
        new EdgeTopologyAnalyzer(),
        [new SliverPercentageRule(settings), new LargeTrianglePercentageRule(settings), new HighValenceVertexRule(settings),
         new DegenerateTriangleRule(), new WatertightRule()],
        settings);

    private static LoadedModel Load(IReadOnlyList<StlFacet> facets)
    {
        var mesh = new VertexWelder(new WeldingSettings()).Weld(facets);
        return new LoadedModel("test.stl", 0, "test", StlFormat.Binary, mesh, facets.Count * 3, TimeSpan.Zero, TimeSpan.Zero);
    }

    /// <summary>A closed double-sided disc: n thin wedges around a hub on top and bottom.</summary>
    private static List<StlFacet> Fan(int n)
    {
        var facets = new List<StlFacet>();
        for (var i = 0; i < n; i++)
        {
            var a = new Vector3(MathF.Cos(2 * MathF.PI * i / n), MathF.Sin(2 * MathF.PI * i / n), 0);
            var j = (i + 1) % n; // close the ring on the exact same point; sin(2π) is not 0 in float
            var b = new Vector3(MathF.Cos(2 * MathF.PI * j / n), MathF.Sin(2 * MathF.PI * j / n), 0);
            facets.Add(new StlFacet(Vector3.Zero, Vector3.Zero, a, b));
            facets.Add(new StlFacet(Vector3.Zero, Vector3.Zero, b, a));
        }

        return facets;
    }

    [Fact]
    public void Clean_cube_has_bounding_box_and_no_warnings()
    {
        var report = CreateAnalyzer(new QualitySettings { LargeTriangleEdgeToDiagonalRatio = 1 }).Analyze(Load(TestData.Cube(2)));

        Assert.Equal(new BoundingBox(Vector3.Zero, new Vector3(2)), report.BoundingBox);
        Assert.Equal(Math.Sqrt(12), report.BoundingBox.Diagonal, 5);
        Assert.Equal(24, report.Statistics.SurfaceArea, 4);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void Fan_hub_has_high_valence_and_touches_slivers()
    {
        // 120 wedges -> 3° hub angle -> every wedge is a sliver, and the hub has valence 120.
        var report = CreateAnalyzer(new QualitySettings { LargeTriangleEdgeToDiagonalRatio = 1 }).Analyze(Load(Fan(120)));

        var hub = Assert.Single(report.Findings.HighValenceVertices);
        Assert.Equal(120, hub.Valence);
        Assert.True(hub.TouchesSliver);
        Assert.Equal(100, report.Findings.SliverPercent, 6);
        Assert.Equal(0, report.Findings.SliversWithoutHighValenceVertex);
        Assert.Contains(report.Warnings, w => w.Check == "Sliver triangles");
    }

    [Fact]
    public void High_valence_does_not_imply_slivers()
    {
        // 24 wedges -> 15° hub angle -> no slivers, but the hub valence is still 24.
        var report = CreateAnalyzer(new QualitySettings { LargeTriangleEdgeToDiagonalRatio = 1 }).Analyze(Load(Fan(24)));

        Assert.Empty(report.Findings.Slivers);
        Assert.False(Assert.Single(report.Findings.HighValenceVertices).TouchesSliver);
    }

    [Fact]
    public void Model_flags_large_triangles_against_bounding_box()
    {
        var report = CreateAnalyzer(new QualitySettings()).Analyze(Load(TestData.Cube()));

        // Every cube face diagonal (1.414) is longer than 25% of the box diagonal (1.732).
        Assert.Equal(12, report.Findings.LargeTriangles.Count);
        Assert.Contains(report.Warnings, w => w.Check == "Very large triangles");
    }
}

public class ConfigurationTests
{
    [Fact]
    public void Loads_json_with_comments_and_resolves_relative_folder()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "Config.json");
            File.WriteAllText(path, """{ /* c */ "StlFolder": "models", "Quality": { "MaxVertexValence": 10, }, }""");

            var settings = new JsonAppSettingsProvider(path).Load();

            Assert.Equal(Path.Combine(dir.FullName, "models"), settings.StlFolder);
            Assert.Equal(10, settings.Quality.MaxVertexValence);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Invalid_values_are_rejected()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "Config.json");
            File.WriteAllText(path, """{ "Quality": { "SliverMinAngleDegrees": 90 } }""");

            var ex = Assert.Throws<ConfigurationException>(() => new JsonAppSettingsProvider(path).Load());
            Assert.Contains("SliverMinAngleDegrees", ex.Message);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
