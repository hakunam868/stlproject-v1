# STL Format Notes

Binary STL is selected when its 80-byte header and little-endian triangle count describe a payload that fits in the file. If a UTF-8 file also matches that length rule, it is treated as ASCII only when the complete file parses as ASCII STL; otherwise, the binary interpretation is retained. This handles binary headers that begin with `solid` while avoiding accidental binary classification of a valid ASCII file. ASCII parsing requires `solid`, `facet normal`, `outer loop`, exactly three `vertex` records, `endloop`, `endfacet`, and `endsolid`.

Both paths reject non-finite coordinates and report malformed structure through `Result`. File normals are retained but geometric checks calculate areas and shape from vertex positions. Duplicate vertices use exact `f32` values, with signed zero normalized; nearby coordinates are intentionally not welded.

Quality thresholds are configurable in `config.json`. A sliver has a minimum corner angle below `sliver_min_angle_degrees` or an aspect ratio above `sliver_aspect_ratio`. A large triangle has a longest edge strictly greater than `large_triangle_edge_to_diagonal_ratio` times the bounding-box diagonal. Incident-edge counts use unique, non-self indexed edges.
