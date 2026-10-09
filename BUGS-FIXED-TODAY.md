# Bug Tracking Log — 2026-10-09

| Bug ID/Name | Owner | Status (Fixed By You / Remaining Pending) | Notes |
|---|---|---|---|
| BUG-02 — Generated sphere has a hole and degenerate triangles | Agni | Fixed By You | Previously fixed; generated sphere is covered by the watertightness and degeneracy regression test. |
| BUG-03 — Cube and cuboid generators are not watertight | Agni | Fixed By You | Previously fixed; cube and prism/cuboid are covered by the generated-shape watertightness test. |
| BUG-04 — Tetrahedron generator produces open edges | Agni | Fixed By You | Previously fixed; tetrahedron is covered by the generated-shape watertightness test. |
| BUG-08 — Missing topology analysis | Agni | Fixed By You | Previously fixed; analysis reports open/non-manifold edges and a watertight flag. |
| BUG-09 — Missing model-level pass/fail rules and report summary | Agni | Remaining Pending | Topology PASS/FAIL exists, but configurable model-level quality limits and pass/fail rules remain unimplemented. |
| BUG-11 — Configuration validation is incomplete/fragile | Agni | Fixed By You | Typed JSON deserialization and validation for quality thresholds are present. |
| BUG-12 — Report can become excessively verbose | Agni | Fixed By You | Detailed report output is capped at 100 findings and states the total and omitted count. |
| Regression — Scale-derived weld tolerance collapses valid short edges | Agni | Fixed By You | Bound the weld tolerance to half the smallest nonzero triangle edge; added a large-span thin-triangle regression. |
