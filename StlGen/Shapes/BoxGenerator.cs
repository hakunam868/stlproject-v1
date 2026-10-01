using System.Numerics;
using StlGen.Geometry;

namespace StlGen.Shapes;

/// <summary>Axis-aligned box centred on the origin. Each face is a grid, and grid points on shared edges are shared.</summary>
public sealed class BoxGenerator : IShapeGenerator
{
    public string Name => "box";

    public string Description => "Box / cube centred on the origin, each face split into a grid";

    public IReadOnlyList<ShapeParameter> Parameters { get; } =
    [
        ShapeParameter.Length("width", "size along X", 10),
        ShapeParameter.Length("depth", "size along Y", 10),
        ShapeParameter.Length("height", "size along Z", 10),
        ShapeParameter.Resolution("divisionsX", "grid divisions along X", 1, 1),
        ShapeParameter.Resolution("divisionsY", "grid divisions along Y", 1, 1),
        ShapeParameter.Resolution("divisionsZ", "grid divisions along Z", 1, 1),
    ];

    public void Build(ParameterSet p, MeshBuilder builder)
    {
        // Each axis's coordinates are computed once, so faces that share an edge get identical floats.
        float[][] axes =
        [
            Coordinates(p["width"], p.Int("divisionsX")),
            Coordinates(p["depth"], p.Int("divisionsY")),
            Coordinates(p["height"], p.Int("divisionsZ")),
        ];

        for (var axis = 0; axis < 3; axis++)
        {
            AddFace(builder, axes, axis, maxSide: false);
            AddFace(builder, axes, axis, maxSide: true);
        }
    }

    private static float[] Coordinates(double size, int divisions)
    {
        var c = new float[divisions + 1];
        for (var i = 0; i <= divisions; i++)
            c[i] = (float)(i == divisions ? size / 2 : -size / 2 + size * i / divisions);
        return c;
    }

    /// <summary>The face perpendicular to <paramref name="axis"/>. The grid runs over the other two axes u = axis+1, v = axis+2,
    /// so u × v points along +axis and the winding is reversed on the min side.</summary>
    private static void AddFace(MeshBuilder builder, float[][] axes, int axis, bool maxSide)
    {
        int u = (axis + 1) % 3, v = (axis + 2) % 3;
        var fixedValue = maxSide ? axes[axis][^1] : axes[axis][0];
        var us = axes[u];
        var vs = axes[v];

        int Vertex(int iu, int iv)
        {
            Span<float> c = stackalloc float[3];
            c[axis] = fixedValue;
            c[u] = us[iu];
            c[v] = vs[iv];
            return builder.AddSharedVertex(new Vector3(c[0], c[1], c[2]));
        }

        for (var iu = 0; iu < us.Length - 1; iu++)
        for (var iv = 0; iv < vs.Length - 1; iv++)
        {
            int p00 = Vertex(iu, iv), p10 = Vertex(iu + 1, iv), p11 = Vertex(iu + 1, iv + 1), p01 = Vertex(iu, iv + 1);
            if (maxSide)
            {
                builder.AddTriangle(p00, p10, p11);
                builder.AddTriangle(p00, p11, p01);
            }
            else
            {
                builder.AddTriangle(p00, p11, p10);
                builder.AddTriangle(p00, p01, p11);
            }
        }
    }
}
