# Bug Allocation Tracking

The provided assignment report does not label any issue fixed, resolved, closed, or completed at intake. Current repository audit: all Agni-owned bugs (BUG-02, BUG-03, BUG-04, BUG-08, BUG-09, BUG-11, and BUG-12) are addressed. Sidd-owned BUG-01, BUG-05, BUG-06, and BUG-07 are implemented; BUG-10 has been optimized but remains slower than the report's C# performance comparison.

| Bug ID/Name | Owner | Status (Fixed By You / Remaining Pending) | Notes |
|---|---|---|---|
| BUG-01 — Silent data loss in multi-solid ASCII STL files | Sidd | Already resolved before this run | Parser consumes subsequent solids; regression test verifies triangles from two solids are both parsed. |
| BUG-02 — Generated sphere has a hole and degenerate triangles | Agni | Already resolved before this run | Existing generated-shape regression test checks sphere watertightness. |
| BUG-03 — Cube and cuboid generators are not watertight | Agni | Already resolved before this run | Existing generated-shape regression test checks cube and prism topology; cuboid uses the prism generator. |
| BUG-04 — Tetrahedron generator produces open edges | Agni | Already resolved before this run | Existing generated-shape regression test checks tetrahedron watertightness. |
| BUG-05 — Sliver detection does not match the reference rule | Sidd | Already resolved before this run | Minimum-angle and aspect-ratio checks are implemented and have an acute-triangle regression test. |
| BUG-06 — Large-triangle detection uses the wrong metric | Sidd | Already resolved before this run | Uses longest-edge/model-diagonal ratio with strict boundary coverage. |
| BUG-07 — Degenerate/micro triangle handling differs and can double-report | Sidd | Fixed By You | Uses the report's `1e-12` dimensionless area/model-diagonal-squared cutoff; degenerate/micro findings take precedence over sliver findings to avoid duplicate classification. Added relative-threshold and overlap regression coverage. |
| BUG-08 — Missing topology analysis | Agni | Already resolved before this run | Analysis reports boundary/non-manifold edges and watertight status. |
| BUG-09 — Missing model-level pass/fail rules and report summary | Agni | Fixed By You | Added finding percentages, a configurable per-category percentage limit, and a model quality PASS/FAIL verdict that also requires watertight topology. |
| BUG-10 — Vertex welding is significantly slower than C# | Sidd | Remaining Pending | Added exact-coordinate memoization and pruned tolerance-neighbor bucket lookups. Release 1M-triangle median improved from 327.658 ms to 255.710 ms, but remains above the report's 65–75 ms C# comparison; parity is not yet met. |
| BUG-11 — Configuration validation is incomplete/fragile | Agni | Already resolved before this run | Typed JSON parsing and finite/range validation are present. |
| BUG-12 — Report can become excessively verbose | Agni | Already resolved before this run | Detailed findings are capped, with omitted totals reported. |
| Verification gap — generated mesh winding | Agni | Already resolved before this run | Cone, cylinder, and torus winding was corrected; tests check all seven shapes for watertightness, expected Euler characteristic, and positive signed volume. |
