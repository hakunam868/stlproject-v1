# STL Analyzer

A small Rust command-line utility for loading ASCII or binary STL files and reporting basic mesh statistics.

## Build and run

```sh
cargo build --release
cargo run --release -- model.stl
```

The report includes total faces, estimated sliver/medium/large/extremely-large triangle counts, surface area, axis-aligned bounds, degenerate triangles, and the top 10 vertices by unique incident-edge count. Size classes use a sliver threshold of `area / longest_edge^2 < 0.01`; other triangles are medium below 10 units, large below 1,000 units, and extremely large at or above 1,000 units. Vertices are joined by exact coordinates (with `-0.0` treated as `0.0`). The crate uses only the Rust standard library.

## Project layout

- `src/lib.rs` exports the reusable geometry, loading, mesh, and analysis APIs.
- `src/main.rs` implements the `stl-analyzer` command-line program.
- `tests/` contains integration coverage for analysis, loading, and the CLI.
- `examples/` contains a sample-file generator and a loading benchmark.
- `docs/` documents the supported STL format behavior.

Run all checks with `cargo test`.

## Generate a varied-size sample

The sample generator creates a binary STL with one million triangles by default, divided among sliver, medium, large, and extremely large triangles:

```sh
cargo run --release --example make_sample -- varied-million.stl
cargo run --release -- varied-million.stl
```

Pass a second argument to choose a different triangle count.