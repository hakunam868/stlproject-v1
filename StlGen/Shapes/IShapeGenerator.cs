using StlGen.Geometry;

namespace StlGen.Shapes;

/// <summary>
/// One primitive shape. To add a shape, implement this interface and register it in Program.cs;
/// the menu, the command line and the test suite pick it up automatically.
/// </summary>
public interface IShapeGenerator
{
    /// <summary>Identifier used on the command line and in file names (lower case, no spaces).</summary>
    string Name { get; }

    string Description { get; }

    IReadOnlyList<ShapeParameter> Parameters { get; }

    /// <summary>Checks that involve more than one parameter. Single-value range checks are done by <see cref="ShapeParameter"/>.</summary>
    IEnumerable<string> Validate(ParameterSet p) => [];

    void Build(ParameterSet p, MeshBuilder builder);
}
