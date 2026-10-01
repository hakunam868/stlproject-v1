# STL Analyzer

A small Rust command-line utility for loading ASCII or binary STL files and reporting basic mesh statistics.

## Build and run

```sh
cargo build --release
cargo run --release -- model.stl
```

The report includes triangle and exact unique-vertex totals, surface area, axis-aligned bounds, degenerate triangles, triangle-size counts relative to the bounding-box diagonal, shape-based sliver counts, and the top 10 vertices by unique incident-edge count. Vertex identity uses exact coordinates (`-0.0` equals `0.0`); nearby but unequal coordinates are not welded.

Size thresholds use the model bounding-box diagonal `D`: micro triangles have longest edge `<= 1e-6 * D`, medium triangles have longest edge `< 0.1 * D`, large triangles have longest edge `< 0.5 * D`, and extremely large triangles have longest edge `>= 0.5 * D`. Sliver shape quality is `q = 4 * sqrt(3) * area / (a^2 + b^2 + c^2)`, where `a`, `b`, and `c` are edge lengths; non-degenerate triangles with `q < 0.05` are counted as slivers. Sliver counts are independent of size counts. These are documented analysis heuristics, not manufacturing tolerances. The crate uses only the Rust standard library.

## Project layout

- `src/lib.rs` exports the reusable geometry, loading, mesh, and analysis APIs.
- `src/main.rs` implements the `stl-analyzer` command-line program.
- `tests/` contains integration coverage for analysis, loading, and the CLI.
- `examples/` contains a sample-file generator and a loading benchmark.
- `docs/` documents the supported STL format behavior.

Run all checks with `cargo test`.

## Generate a varied-size sample

The sample generator creates a binary STL with one million triangles by default, covering varied triangle sizes and shapes:

```sh
cargo run --release --example make_sample -- varied-million.stl
cargo run --release -- varied-million.stl
```

Pass a second argument to choose a different triangle count. Measure end-to-end load and analysis throughput with `cargo run --release --example benchmark -- varied-million.stl`.