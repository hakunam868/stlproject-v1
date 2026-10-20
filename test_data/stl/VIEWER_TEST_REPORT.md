# ParseSTL Viewer Test Report

**Date:** 2026-10-20
**Viewer:** ParseSTL.Viewer, `http://localhost:5318`
**Test method:** Browser-based file uploads through the viewer's Open input
**Corpus:** 36 STL fixtures in this directory

## Issues found and fixes

| ID | Severity | Fixture / area | Observed behavior | Status |
|---|---|---|---|---|
| V-01 | High | `invalid/truncated_binary.stl` | A binary STL whose declared facet is one byte short was accepted and displayed. | Fixed: declared binary length is checked before facet parsing; viewer reports expected and actual byte counts. |
| V-02 | High | `invalid/corrupted_triangle_nan.stl` | A triangle with a NaN coordinate was accepted; Three.js reported a NaN bounding-sphere warning. | Fixed: non-finite vertex coordinates and facet normals are rejected before adding a mesh. |
| V-03 | Medium | Invalid, empty, and zero-triangle inputs | Short inputs caused uncaught `RangeError`s; empty/malformed inputs were silent. | Fixed: parser checks short buffers, validates facet structure and non-empty geometry, and shows an accessible error toast. |
| V-04 | Medium | View buttons and very small/large models | Camera clipping planes were fixed at a range unsuitable for models at `1e-12` and `1e12` scales. | Fixed: camera distance and near/far planes are fitted to the model, projection, and view direction. |
| V-05 | Low | Initial camera framing | Newly opened models were not automatically framed. | Fixed: successful STL Open loads frame the complete model; Import retains the current camera view. |

Valid mesh-quality defects (degenerate, duplicate, inconsistent-normal, and
non-manifold examples) loaded successfully in the baseline. They must remain
renderable; parser rejection should be limited to structurally invalid STL or
coordinates that cannot be rendered.

## Verification

- **Corpus:** 36/36 expected outcomes matched in the actual viewer: 30 geometry
  fixtures rendered with finite coordinates; five structurally invalid files
  and the zero-triangle file were rejected with visible error messages.
- **Valid mesh defects:** Degenerate, duplicate-face, duplicate-vertex,
  inconsistent-normal, and non-manifold fixtures continued to load.
- **Camera framing:** The banana model fit in the initial camera view and all
  six standard views (all eight bounding-box corners inside the camera
  frustum). The `1e-12` and `1e12` scale triangles also fit after opening and
  after selecting standard views.
- **Controls:** A single wheel step changed camera distance by a factor of
  `0.95`; right-button drag changed camera position; middle-button drag
  changed the camera target.
- **Large mesh:** The existing 50,000,084-byte benchmark loaded and framed
  1,000,000 triangles in 719 ms in the local browser run, with no page error.
- **Errors:** Invalid fixtures produced handled console diagnostics and an
  accessible toast; there were no uncaught browser page errors.
- **Viewer build:** `dotnet build .\ParseSTL.Viewer\ParseSTL.Viewer.csproj --no-restore`
  passed with 0 warnings and 0 errors.
- **JavaScript syntax:** `node --check` passed for `STLLoader.js`,
  `fileHandlers.js`, and `main.js`.
- **Analyzer regressions:** `cargo test` passed (52 passed, 0 failed, 1 ignored).
- **Rust checks:** `cargo fmt --check` and `cargo check --all-targets` passed.
  `cargo clippy --all-targets` exited successfully with two existing warnings in
  `ParseSTL/Analysis/analysis.rs`.
- **Runtime:** The updated viewer was started on port 5318 because the existing
  listener on 5317 belonged to a process not started by this test run.

The one ignored Rust test is the repository's opt-in million-triangle vertex
welding benchmark; it is not a failing test. The browser benchmark above tests
the viewer's STL loading and framing path, not the Rust analyzer's performance.
