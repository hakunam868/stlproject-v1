using StlGen.Geometry;

namespace StlGen.Shapes;

// Shapes built by sweeping a profile around the Z axis (see Lathe). All are centred on the origin.

public sealed class CylinderGenerator : IShapeGenerator
{
    public string Name => "cylinder";

    public string Description => "Cylinder; fan caps (capRings = 1) give a high-valence hub, capRings = 0 leaves the ends open";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        ShapeParameter.Length("radius", "radius", 5),
        ShapeParameter.Length("height", "height along Z", 20),
        ShapeParameter.Resolution("segments", "divisions around the axis", 32, 3),
        ShapeParameter.Resolution("stacks", "divisions along the height", 1, 1),
        ShapeParameter.Resolution("capRings", "concentric rings per cap (0 = open ends, 1 = fan)", 1, 0),
    ];

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        double r = p["radius"], h = p["height"];
        var profile = Profile.Line(new(r, -h / 2), new(r, h / 2), p.Int("stacks")).ToList();
        Lathe.Build(builder, profile, closed: false, p.Int("segments"), p.Int("capRings"));
    }
}

public sealed class ConeGenerator : IShapeGenerator
{
    public string Name => "cone";

    public string Description => "Cone (topRadius = 0) or frustum (topRadius > 0); few segments give a pyramid";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        ShapeParameter.Length("bottomRadius", "radius at the base", 5),
        new("topRadius", "radius at the top (0 = pointed apex)", 0, 0, 1e9),
        ShapeParameter.Length("height", "height along Z", 10),
        ShapeParameter.Resolution("segments", "divisions around the axis", 32, 3),
        ShapeParameter.Resolution("stacks", "divisions along the height", 1, 1),
        ShapeParameter.Resolution("capRings", "concentric rings per cap (0 = open, 1 = fan)", 1, 0),
    ];

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        var h = p["height"];
        var profile = Profile.Line(new(p["bottomRadius"], -h / 2), new(p["topRadius"], h / 2), p.Int("stacks")).ToList();
        Lathe.Build(builder, profile, closed: false, p.Int("segments"), p.Int("capRings"));
    }
}

public sealed class PrismGenerator : IShapeGenerator
{
    public string Name => "prism";

    public string Description => "Right prism with a regular polygon cross-section (3 = triangular, 6 = hexagonal...)";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        new("sides", "number of polygon sides (not scaled by density)", 6, 3, 10_000, IsInteger: true),
        ShapeParameter.Length("radius", "circumradius of the polygon", 5),
        ShapeParameter.Length("height", "height along Z", 10),
        ShapeParameter.Resolution("stacks", "divisions along the height", 1, 1),
        ShapeParameter.Resolution("capRings", "concentric rings per cap (0 = open, 1 = fan)", 1, 0),
    ];

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        double r = p["radius"], h = p["height"];
        var sides = p.Int("sides");
        var profile = Profile.Line(new(r, -h / 2), new(r, h / 2), p.Int("stacks")).ToList();
        // Rotating by half a side puts a flat face toward +X (a square prism becomes axis-aligned).
        Lathe.Build(builder, profile, closed: false, sides, p.Int("capRings"), angleOffset: Math.PI / sides);
    }
}

public sealed class SphereGenerator : IShapeGenerator
{
    public string Name => "sphere";

    public string Description => "UV (latitude/longitude) sphere; the triangles get thinner towards the poles";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        ShapeParameter.Length("radius", "radius", 10),
        ShapeParameter.Resolution("segments", "longitude divisions", 32, 3),
        ShapeParameter.Resolution("rings", "latitude divisions (pole to pole)", 16, 2),
    ];

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        var r = p["radius"];
        var rings = p.Int("rings");
        var profile = new List<ProfilePoint>(rings + 1);
        for (var j = 0; j <= rings; j++)
        {
            var theta = Math.PI * j / rings;
            // The poles get radius exactly 0 so they become a single vertex (sin(π) is not exactly 0 in floating point).
            var radius = j == 0 || j == rings ? 0 : r * Math.Sin(theta);
            var z = j == 0 ? -r : j == rings ? r : -r * Math.Cos(theta);
            profile.Add(new ProfilePoint(radius, z));
        }

        Lathe.Build(builder, profile, closed: false, p.Int("segments"));
    }
}

public sealed class TorusGenerator : IShapeGenerator
{
    public string Name => "torus";

    public string Description => "Ring torus around the Z axis (genus 1, so Euler characteristic 0)";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        ShapeParameter.Length("majorRadius", "distance from the axis to the tube centre", 10),
        ShapeParameter.Length("minorRadius", "tube radius", 3),
        ShapeParameter.Resolution("majorSegments", "divisions around the Z axis", 48, 3),
        ShapeParameter.Resolution("minorSegments", "divisions around the tube", 24, 3),
    ];

    public IEnumerable<string> Validate(ParameterSet p)
    {
        if (p["minorRadius"] >= p["majorRadius"])
            yield return "minorRadius must be smaller than majorRadius (otherwise the torus intersects itself).";
    }

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        double major = p["majorRadius"], minor = p["minorRadius"];
        var n = p.Int("minorSegments");
        var profile = Enumerable.Range(0, n)
            .Select(k => 2 * Math.PI * k / n)
            .Select(a => new ProfilePoint(major + minor * Math.Cos(a), minor * Math.Sin(a)))
            .ToList();
        Lathe.Build(builder, profile, closed: true, p.Int("majorSegments"));
    }
}

public sealed class TubeGenerator : IShapeGenerator
{
    public string Name => "tube";

    public string Description => "Hollow cylinder (pipe) with flat annular ends (genus 1)";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        ShapeParameter.Length("outerRadius", "outer radius", 10),
        ShapeParameter.Length("innerRadius", "inner (bore) radius", 7),
        ShapeParameter.Length("height", "height along Z", 20),
        ShapeParameter.Resolution("segments", "divisions around the axis", 48, 3),
        ShapeParameter.Resolution("stacks", "divisions along the height", 1, 1),
        ShapeParameter.Resolution("wallDivisions", "divisions across the wall on each end", 1, 1),
    ];

    public IEnumerable<string> Validate(ParameterSet p)
    {
        if (p["innerRadius"] >= p["outerRadius"])
            yield return "innerRadius must be smaller than outerRadius.";
    }

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        double ro = p["outerRadius"], ri = p["innerRadius"], h = p["height"];
        int stacks = p.Int("stacks"), wall = p.Int("wallDivisions");
        ProfilePoint bottomIn = new(ri, -h / 2), bottomOut = new(ro, -h / 2), topOut = new(ro, h / 2), topIn = new(ri, h / 2);

        // Closed rectangle, counter-clockwise in (radius, z): bottom end -> outer wall -> top end -> inner wall.
        var profile = Profile.Line(bottomIn, bottomOut, wall, includeEnd: false)
            .Concat(Profile.Line(bottomOut, topOut, stacks, includeEnd: false))
            .Concat(Profile.Line(topOut, topIn, wall, includeEnd: false))
            .Concat(Profile.Line(topIn, bottomIn, stacks, includeEnd: false))
            .ToList();
        Lathe.Build(builder, profile, closed: true, p.Int("segments"));
    }
}
