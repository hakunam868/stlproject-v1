# ParseSTL — Rust demo

This branch contains the Rust implementation of the STL analyzer and test-model generator. The repository's `main` branch remains the original C# implementation.

The Rust project follows the same top-level organization:

| Folder | Purpose |
| --- | --- |
| `ParseSTL/` | STL parsing, mesh processing, analysis, configuration, and console UI |
| `StlGen/` | Shape generation, STL output, benchmark service, and generator UI |
| `ParseSTL.Tests/` | Rust integration tests for parsing, analysis, and generation |

## Build and test

Requires the stable Rust toolchain.

```powershell
cargo build
cargo test
```

## Analyze STL files

Place files in `ParseSTL/StlFiles` (configured by `ParseSTL/Config.json`) and run:

```powershell
cargo run --bin stl-analyzer
```

The menu reports the STL format, face count, exact-coordinate unique vertices, bounding box, and mesh-quality findings. ASCII and binary STL are supported.

## Generate models

```powershell
cargo run --bin stl-generator
cargo run --bin stl-generator -- cube --size 40 --density 16
cargo run --bin stl-generator -- prism --width 60 --height 25 --depth 10 --format ascii
cargo run --bin stl-generator -- torus --major-radius 30 --minor-radius 8
```

Supported shapes are cuboid/prism, cube, cylinder, cone, sphere, torus, and tetrahedron. Generated files go to `StlGen/StlFiles`, as configured by `StlGen/Config.json`; `--output` can override the destination.

## Layout

```text
ParseSTL/
  Analysis/       mesh-quality analysis
  Configuration/  analyzer settings
  Files/          file discovery
  Model/          vectors, triangles, indexed mesh
  Parsing/        ASCII and binary STL parsing
  Processing/     parsed-model loading and vertex indexing
  UI/             analyzer menu, commands, and reports
StlGen/
  Configuration/  generator configuration
  Geometry/       shared geometric primitives
  Output/         ASCII and binary STL writers
  Services/       benchmark fixture generation
  Shapes/         parameterized primitive generators
  UI/             interactive and command-line generator
ParseSTL.Tests/   Rust integration tests
```

The analyzer and generator share the mesh model and STL format types. The core has no third-party Rust dependencies.
