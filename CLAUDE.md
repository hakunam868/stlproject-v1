# CLAUDE.md

Context for Claude Code when working in this repository.

## What this is

.NET 10 (C# 14) solution `ParseSTL.sln` with three projects and no third-party runtime packages:

- `ParseSTL/`: console app. It parses ASCII and binary STL, welds duplicate vertices, and analyses the mesh (counts,
  bounding box, triangle quality, topology, valence).
- `StlGen/`: console app that generates primitive test meshes with adjustable density. It is independent of
  ParseSTL; do not add a project reference between them.
- `ParseSTL.Tests/`: xUnit tests. They reference both apps; `GeneratorTests` round-trips every StlGen shape through
  ParseSTL.

## Commands

```powershell
dotnet build ParseSTL.sln          # must stay at 0 warnings
dotnet test ParseSTL.sln           # all tests must pass
dotnet run --project ParseSTL      # interactive: 1 open, 2 analyse, 3 exit
dotnet run --project StlGen -- list
dotnet run --project StlGen -- suite --densities 1,4,16 --out <folder>
```

Each app reads `Config.json` from `AppContext.BaseDirectory` (copied to the output folder on build). Relative
folders in the config resolve against the exe folder, not the working directory.

## Design rules (keep these)

- **SOLID / composition root**: concrete types are created only in each `Program.cs`. Classes take interfaces through
  primary constructors. Extend by adding an implementation (`IQualityRule`, `IMenuCommand`, `IStlParser`,
  `IShapeGenerator`, `IStlWriter`) and registering it, not by editing switch statements.
- **Pipeline order** in ParseSTL: `StlReader` (detect, then parse into a raw `StlDocument` triangle soup), then
  `VertexWelder` (indexed `Mesh`), then `MeshAnalyzer`. Welding happens after parsing; keep it that way.
- **Format detection**: binary when `84 + 50 × count == file length`; otherwise ASCII only if the file starts with
  `solid` and the start is printable text. Many binary files start with "solid", so never detect by keyword alone.
- **Performance**: welding and edge counting are single passes using `CollectionsMarshal.GetValueRefOrAddDefault`.
  Do not use `long` as a dictionary key for packed pairs: `long.GetHashCode()` is `hi ^ lo` and collides badly
  (this once made a 1M-triangle analysis hang). Use a record struct with `HashCode.Combine`.
- **Precision**: STL is float32, so metrics are computed in double. Angles use `atan2(|u×v|, u·v)`, not `acos`.
- **Testability**: all console I/O goes through `IConsole`, and prompts through `INumberPrompt` / `IPrompt`.
- StlGen writers must keep shared vertices bit-identical (indexed `MeshData`, ASCII uses `G9`) so ParseSTL welds
  generated files exactly. Binary headers must not start with "solid".

## Quality definitions (agreed with the user)

- Sliver: min angle < `SliverMinAngleDegrees` (5) OR aspect ratio `L² / 2A` > `SliverMaxAspectRatio` (20).
- Very large: longest edge > `LargeTriangleEdgeToDiagonalRatio` (0.25) × bounding-box diagonal.
- Model warning: sliver % > 5, very-large % > 1.
- High-valence vertex: unique incident edges > `MaxVertexValence` (10). High valence suggests slivers, but slivers do
  not imply high valence; the report shows both directions.
- Degenerate triangles (repeated index, or area ≤ 1e-12 × diagonal²) are reported separately and excluded from
  the statistics.

## StlGen notes

- Most shapes use `Geometry/Lathe`, which sweeps a (radius, z) profile around Z. Radius 0 becomes a single pole or
  apex vertex. The profile runs counter-clockwise (upwards on the outside) so normals point outwards. `capRings`:
  0 = open, 1 = fan cap, n = concentric rings.
- `ShapeParameter.Scaling`: `Linear` parameters are multiplied by density, `Log2` adds log2(density) (icosphere),
  `None` is left alone.
- New shapes must pass `GeneratorTests`: watertight, Euler characteristic 2 (or 0 for genus 1), positive signed
  volume, and the ASCII round-trip identical to binary.

## Conventions

- File-scoped namespaces (`ParseStl.*`, `StlGen.*`), nullable enabled, `InvariantGlobalization`.
- Domain errors use `StlParseException`, `ConfigurationException` and `GenerationException`. The UI catches only
  those plus IO exceptions.
- Match the existing comment density: XML docs on public types and non-obvious members only.
