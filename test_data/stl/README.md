# STL viewer/parser test corpus

This corpus is a compact test set for the Rust parser, mesh loader, analysis
engine, and ParseSTL viewer. It contains existing locally authored synthetic
fixtures for parser boundary cases, plus separately downloaded real-world models
with redistribution rights and attribution documented below. The synthetic
fixtures are not downloaded models; their fixture-generation source has been
removed from the project.

```powershell
cargo test --test viewer_dataset
dotnet run --project ParseSTL.Viewer
```

With the viewer open at `http://localhost:5317`, use **Open** to select each
fixture listed below. Actual browser test results and fixed issues are recorded
in [VIEWER_TEST_REPORT.md](./VIEWER_TEST_REPORT.md).

## Expected behavior

| File | Format / test | Parser and viewer expectation |
|---|---|---|
| `basic-geometry/cube_ascii.stl` | ASCII cube | Parses and renders |
| `basic-geometry/cube_binary.stl` | Binary cube | Parses and renders |
| `basic-geometry/cuboid_binary.stl` | Binary subdivided cuboid | Parses and renders |
| `basic-geometry/low_poly_sphere_binary.stl` | Binary low-poly sphere | Parses and renders |
| `basic-geometry/sphere_ascii.stl` | ASCII low-poly sphere | Parses and renders |
| `basic-geometry/cylinder_binary.stl` | Binary cylinder | Parses and renders |
| `basic-geometry/cone_ascii.stl` | ASCII cone | Parses and renders |
| `basic-geometry/pyramid_binary.stl` | Binary tetrahedral pyramid | Parses and renders |
| `mesh-complexity/high_poly_sphere_binary.stl` | Binary sphere, 25,280 triangles | Parses and renders; useful for interactive responsiveness checks |
| `mesh-complexity/large_grid_binary.stl` | Binary 20,000-triangle grid | Parses and renders; moderate large-mesh fixture |
| `geometric-edge-cases/thin_feature_ascii.stl` | Long, thin triangle | Parses and renders; analysis should identify a sliver |
| `geometric-edge-cases/sliver_triangle_ascii.stl` | Acute triangle | Parses and renders; analysis should identify a sliver |
| `geometric-edge-cases/sharp_edge_cube_binary.stl` | Binary cube with sharp edges | Parses and renders |
| `geometric-edge-cases/very_small_triangle_binary.stl` | Binary triangle at 1e-12 scale | Parses; viewer should frame small finite coordinates |
| `geometric-edge-cases/very_large_triangle_binary.stl` | Binary triangle at 1e12 scale | Parses; viewer should frame large finite coordinates |
| `boundary-cases/single_triangle_ascii.stl` | One valid ASCII face | Parses and renders; open edges are a quality finding, not a parse error |
| `boundary-cases/zero_triangle_ascii.stl` | ASCII solid with no facets | Rust parser accepts zero triangles; viewer reports that there is no geometry to display |
| `boundary-cases/rotated_triangle_binary.stl` | Binary face not aligned to XY | Parses and renders in its source orientation |
| `quality-defects/degenerate_triangles_ascii.stl` | Zero-area/repeated-vertex faces | Parses; viewer should not crash; analysis reports micro/degenerate geometry |
| `quality-defects/duplicate_faces_ascii.stl` | Repeated face | Parses; viewer renders the overlapping faces; analysis reports topology/quality as applicable |
| `quality-defects/duplicate_vertices_binary.stl` | Repeated coordinates across facets | Parses and renders; mesh loader applies the existing vertex-welding policy |
| `quality-defects/inconsistent_normals_ascii.stl` | Valid geometry with deliberately wrong supplied normal | Parses and renders from vertices; supplied facet normal is not a parser validity criterion |
| `quality-defects/non_manifold_edge_ascii.stl` | Three faces share one edge | Parses and renders; analysis reports a non-manifold edge |
| `real-world/ceramic_pot_cc0.stl` | Binary real-world model, 3,592 triangles | Parses and renders; CC0; provenance below |
| `real-world/croissant_cc0.stl` | Binary real-world model, 1,990 triangles | Parses and renders; CC0; provenance below |
| `real-world/rubber_duck_toy_cc0.stl` | Binary real-world model, 4,288 triangles | Parses and renders; CC0; provenance below |
| `real-world/carved_wooden_elephant_cc0.stl` | Binary real-world model, 2,752 triangles | Parses and renders; CC0; provenance below |
| `real-world/bananas_cc0.stl` | Binary detailed organic model, 46,868 triangles | Parses and renders; CC0; higher-detail responsiveness case; provenance below |
| `real-world/food_apple_01_cc0.stl` | Binary organic model, 7,012 triangles | Parses and renders; CC0; provenance below |
| `real-world/simple_gear_wheel_cc0.stl` | Binary mechanical gear, 3,072 triangles | Parses and renders; CC0; mechanical geometry; provenance below |
| `real-world/utah_teapot_solid_cc0.stl` | Binary curved classic model, 9,438 triangles | Parses and renders; CC0; provenance below |
| `invalid/empty_file.stl` | Zero-byte file | Parser rejects; viewer reports an empty STL |
| `invalid/truncated_binary.stl` | Binary count says one facet, payload is one byte short | Parser rejects length/count mismatch; viewer reports truncation |
| `invalid/malformed_ascii.stl` | Incomplete facet grammar | Parser rejects malformed ASCII; viewer reports invalid geometry |
| `invalid/incorrect_header.stl` | UTF-8 text not starting with `solid` | Parser rejects invalid ASCII; viewer reports an invalid or truncated STL |
| `invalid/corrupted_triangle_nan.stl` | Binary facet contains NaN coordinate | Parser rejects non-finite coordinate; viewer rejects non-finite vertex data |

## Coverage notes and manual viewer checks

- **Real-world asset provenance:** These models are redistributed under CC0 1.0
  Universal. Poly Haven's [asset license policy](https://polyhaven.com/license)
  confirms CC0 applies to its assets and excludes website content (which is not
  included here). Files were downloaded from the
  [rights-checked source collection](https://github.com/andycana/image3d-free-stl-library/tree/607ad74aef7d2bb12ea0ded859795b0e5495ecf5/models);
  the original model page, credited author, and SHA-256 are retained for each:

  | Local file | Original source / author | License | SHA-256 |
  |---|---|---|---|
  | `real-world/ceramic_pot_cc0.stl` | [Ceramic Pot](https://polyhaven.com/a/ceramic_pot), Aron Łyczek | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `bb73367b52c744e11c66b7e3432db8d56c71bdbf41fbecb4fd2461d892525327` |
  | `real-world/croissant_cc0.stl` | [Croissant](https://polyhaven.com/a/croissant), Greg Zaal and Dario Barresi | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `0a4534060417162de32ae9b3363d52f09978a594ef50fb25d8fda5d3f3a4bd52` |
  | `real-world/rubber_duck_toy_cc0.stl` | [Rubber Duck Toy](https://polyhaven.com/a/rubber_duck_toy), Plat251 | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `c832cda14061305e7c03dc2cb6936da557bccae0955babdd18839a5eaa6354f2` |
  | `real-world/carved_wooden_elephant_cc0.stl` | [Carved Wooden Elephant](https://polyhaven.com/a/carved_wooden_elephant), Greg Zaal | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `31602fe1cd6b4284b6d46751a5e6716fca261f5a837fb63746e0caeb4a9507ff` |
  | `real-world/bananas_cc0.stl` | [Bananas](https://polyhaven.com/a/bananas), Alexander Shulha | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `1b0225fc0ad109d1af24560e92117ed0de9f5f0b83b0848efbb5b9df72cf19b0` |
  | `real-world/food_apple_01_cc0.stl` | [Food Apple 01](https://polyhaven.com/a/food_apple_01), Oliver Harries | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `99e9876f738485b9f3a6f047f8f94743816b4d393928c71ed2c65bcc6332c226` |
  | `real-world/simple_gear_wheel_cc0.stl` | [Simple Gear Wheel](https://commons.wikimedia.org/wiki/File:Zahnrad_20230523_001.stl), PantheraLeo1359531 | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `59e3ef9b9754ad22822b6d8026f1efe456d5ffd4039e176faa58dcec63241150` |
  | `real-world/utah_teapot_solid_cc0.stl` | [Utah Teapot (solid)](https://commons.wikimedia.org/wiki/File:Utah_teapot_(solid).stl), zzubnik (Nik Clark) | [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) | `0ba068b3ffd2a927b72d5037a163fcc17860bc474c06f0e85a78ad34faccce15` |

  All eight files are binary STL, totaling 3,951,272 bytes. Hashes were checked
  against the source collection's per-model metadata at download time. Model
  authors are credited even where CC0 does not require attribution. These assets
  are geometry test inputs; their presence is not a claim that they are print-ready.
- These fixtures intentionally separate **structural validity** from **mesh
  quality**. Defect fixtures should load if their STL structure and coordinates
  are valid; analyzer findings do not make them unreadable.
- The project already contains `ParseSTL/StlFiles/benchmark_1000000.stl` (50 MB)
  for million-triangle performance work. It is not duplicated here to avoid
  adding another large asset; loading it can require substantial time and memory.
- Existing `stl_files/` and `ParseSTL/StlFiles/` contain additional repository
  fixtures used by parser/analysis tests. Their third-party provenance is not
  documented, so this corpus does not copy or claim an external license for them.
- Rendering, visual scale/orientation, mouse interaction, and frame-rate remain
  manual checks because unit tests do not create a desktop graphics context.

## Manual viewer checks

```powershell
dotnet run --project ParseSTL.Viewer
```

Open `http://localhost:5317`, select **Open**, and choose each STL file. Check
ASCII and binary loading, framing, rotation, panning, zoom, loading successive
files, and how malformed and empty files are reported. The 36-file corpus and
the existing one-million-triangle benchmark have been exercised in the browser;
see the linked test report for results.
