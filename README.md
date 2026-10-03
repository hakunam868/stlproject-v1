# STL Analyzer

Rust console application for parsing and checking ASCII and binary STL meshes. It uses no external crates.

## Run

Build with stable Rust: `cargo build --release`.

Place `config.json` beside the executable when distributing it. For `cargo run`, the checked-in project copy is used as a development fallback. `stl_folder` is relative to that config file.

Run the numbered menu with `cargo run --release`. It lists `.stl` files in the configured folder and reports format, triangle count, deduplicated vertex count, bounds, and quality warnings.

Generate a sample with configurable density:

```sh
cargo run --release -- generate cube 12 ascii cube.stl
cargo run --release -- generate torus 48 binary torus.stl
```

Supported shapes are `prism`, `torus`, `cylinder`, `cube`, `cone`, and `sphere`.

## Dedicated Generator

`stl-generator` is a separate binary for dimensioned models. It writes binary STL to the configured `stl_folder` by default; use `--format ascii` or `--output` to override either choice.

Run it without arguments for the interactive generator. Choose Cube, Cylinder, or Torus, then enter dimensions; pressing Enter accepts each displayed default.

```sh
cargo run --release --bin stl-generator
```

```sh
cargo run --release --bin stl-generator -- cube --size 40 --density 16
cargo run --release --bin stl-generator -- prism --width 60 --height 25 --depth 10 --format ascii
cargo run --release --bin stl-generator -- cylinder --radius 12.5 --height 80
cargo run --release --bin stl-generator -- cone --radius 15 --height 50
cargo run --release --bin stl-generator -- sphere --radius 18
cargo run --release --bin stl-generator -- torus --major-radius 30 --minor-radius 8
```

Defaults: prism `2 x 1 x 1`, cube size `1`, cylinder/cone radius `1` and height `2`, sphere radius `1`, torus major radius `1` and minor radius `0.35`, density `24`.

Create and measure the required fixture:

```sh
cargo run --release -- benchmark 1000000 benchmark_1000000.stl
```

The benchmark streams the binary fixture to disk, then reports parsing, deduplication, analysis, total time, and estimated indexed-mesh storage. Run checks with `cargo test`.

## Components

- `geometry.rs`: vectors, raw triangles, and exact-coordinate indexed mesh deduplication.
- `stl.rs`: structural binary detection, strict ASCII parsing, and STL writers.
- `analysis.rs`: bounds, sliver, relative-area, and unique incident-edge checks.
- `samples.rs`: parameterized primitives and the streaming grid benchmark writer.
- `app.rs`: config loading, menu, reports, commands, and benchmark timings.

Vertices are deduplicated by exact `f32` coordinate bits, with `-0.0` equivalent to `0.0`. The config defaults are an aspect ratio of `20.0` for slivers, relative area of `0.25` of the bounding-box diagonal squared for large triangles, and more than `12` unique incident edges for high-incidence vertices. These are mesh-review heuristics, not manufacturing tolerances.
