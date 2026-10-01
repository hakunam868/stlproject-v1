# ParseSTL

A .NET 10 toolkit for reading, checking and generating **STL** meshes.

| Project | What it does |
| --- | --- |
| **ParseSTL** | Console app that parses ASCII and binary STL files, merges duplicate vertices and reports triangle and vertex counts, the bounding box, triangle-quality warnings (slivers, very large triangles, high-valence vertices) and watertightness. |
| **StlGen** | Console app that generates test models (box, cylinder, cone, prism, UV sphere, icosphere, torus, tube) at any density, in binary or ASCII. |
| **ParseSTL.Tests** | xUnit tests, including round-trips of every StlGen shape through the ParseSTL parsers and analyser. |

STL format reference: <https://top3dshop.com/blog/the-ultimate-guide-to-stl-format>

---

## Getting started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
git clone <repo-url>
cd ParseSTL
dotnet build
dotnet test
```

### Analyse a file

```powershell
dotnet run --project ParseSTL
```

```text
  1. Open STL file          <- lists *.stl files in the configured folder
  2. Analyse the open file  <- full report
  3. Close the application
```

Put your models in the folder named by `StlFolder` in `ParseSTL/Config.json` (default: `StlFiles` next to the exe;
a few samples are included).

### Generate test models

```powershell
dotnet run --project StlGen                      # interactive menu
dotnet run --project StlGen -- list              # shapes and parameters
dotnet run --project StlGen -- cylinder --radius 5 --height 80 --segments 256 --format both
dotnet run --project StlGen -- sphere --density 8
dotnet run --project StlGen -- cone --segments 4             # square pyramid
dotnet run --project StlGen -- suite --densities 1,4,16      # every shape at every density
```

`--density` multiplies every resolution parameter (segments, stacks, divisions...). For the icosphere it adds
log2(density) subdivision levels. Set `OutputFolder` in `StlGen/Config.json` to the same folder as ParseSTL's
`StlFolder` to analyse generated files straight away.

---

## What the analysis reports

```text
=== ANALYSIS ===           format, solid name, triangles, unique vertices, duplicates removed, timings
=== BOUNDING BOX ===       min, max, size, centre, diagonal
=== TRIANGLE QUALITY ===   surface area, area / min-angle / aspect-ratio statistics
=== TOPOLOGY ===           unique, boundary and non-manifold edges, vertex valence
=== MODEL CHECKS ===       [ OK ] / [WARN] for each model-level rule
!!! warning lists          slivers, very large triangles, high-valence vertices, degenerate triangles (worst first)
```

### Quality rules (defaults in `ParseSTL/Config.json`)

| Classification | Rule |
| --- | --- |
| **Sliver** | smallest angle < **5°** OR aspect ratio (longest edge / shortest altitude) > **20** |
| **Very large** | longest edge > **25 %** of the bounding-box diagonal |
| **Model warning** | slivers > **5 %** of faces, or very large triangles > **1 %** of faces |
| **High-valence vertex** | more than **10** unique incident edges |
| **Degenerate** | zero area (within float precision) or two corners welded together |

A high-valence vertex is often the hub of a fan of slivers, but many slivers (for example the long thin side strips
of a cylinder) have no high-valence corner. The report counts both, so you can see how far the two overlap.

### Performance

About 1M triangles (48 MB binary) are parsed, welded and analysed in under 1 second (Release build). Vertex welding
and edge counting are each a single hash-map pass.

---

## Configuration

<details>
<summary><b>ParseSTL/Config.json</b></summary>

| Key | Default | Meaning |
| --- | --- | --- |
| `StlFolder` | `StlFiles` | Folder scanned for `*.stl` (relative to the exe). |
| `Welding.Tolerance` | `0` | `0` = merge bit-identical vertices; `> 0` = snap to a grid of this size. |
| `Quality.SliverMinAngleDegrees` | `5` | Sliver if smallest angle is below this... |
| `Quality.SliverMaxAspectRatio` | `20` | ...or the aspect ratio is above this. |
| `Quality.LargeTriangleEdgeToDiagonalRatio` | `0.25` | Very large if longest edge > ratio × bounding-box diagonal. |
| `Quality.ModelSliverPercentThreshold` | `5` | Model warning threshold for slivers (%). |
| `Quality.ModelLargeTrianglePercentThreshold` | `1` | Model warning threshold for very large triangles (%). |
| `Quality.MaxVertexValence` | `10` | Valence limit per vertex. |
| `Quality.MaxListedWarnings` | `15` | Rows printed per warning list. |

</details>

<details>
<summary><b>StlGen/Config.json</b></summary>

| Key | Default | Meaning |
| --- | --- | --- |
| `OutputFolder` | `StlFiles` | Where generated files go (relative to the exe). |
| `DefaultFormats` | `["Binary"]` | `Binary`, `Ascii`, or both. |
| `MaxTriangles` | `20000000` | Safety limit per model. |
| `Suite.Densities` | `[1, 4, 16]` | Density multipliers used by `suite`. |
| `Suite.Formats` | `["Binary"]` | Formats used by `suite`. |

</details>

---

## Architecture

Both apps follow SOLID: small single-purpose classes behind interfaces, wired together only in each `Program.cs`
(the composition root). No third-party packages.

```text
ParseSTL/
  Configuration/   settings, validation, JSON loader
  Parsing/         format detector, AsciiStlParser, BinaryStlParser, StlReader (picks the parser)
  Processing/      VertexWelder (runs after parsing), ModelLoader pipeline
  Analysis/        metrics -> classifier -> topology/valence -> MeshAnalyzer -> Rules/ (IQualityRule)
  UI/              IConsole, numbered IMenuCommand entries, ConsoleReportRenderer
StlGen/
  Geometry/        MeshBuilder, Lathe (surface of revolution used by most shapes)
  Shapes/          IShapeGenerator + one class per shape
  Output/          BinaryStlWriter, AsciiStlWriter
  Services/        GenerationService (defaults -> overrides -> density -> validate -> build -> write), TestSuiteGenerator
  UI/              InteractiveRunner, CommandLineRunner
```

**Extending:**

- New model check: implement `IQualityRule` and register it in `ParseSTL/Program.cs`.
- New menu entry: implement `IMenuCommand`.
- New shape: implement `IShapeGenerator` and add it to the array in `StlGen/Program.cs`. The menu, the command line
  and the suite pick it up automatically. Add it to `GeneratorTests.Shapes` so the watertight, orientation and Euler
  tests cover it.

## Contributing

1. Branch from `main`.
2. `dotnet build` with no warnings and `dotnet test` all green.
3. Open a pull request.
