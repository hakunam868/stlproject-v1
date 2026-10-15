# STL Format Notes

Binary STL is selected when its 80-byte header, little-endian triangle count, and exact `84 + 50 * count` file length agree. This safely handles binary headers that begin with `solid`. Otherwise, valid UTF-8 is parsed as strict ASCII STL with `solid`, `facet normal`, `outer loop`, exactly three `vertex` records, `endloop`, `endfacet`, and `endsolid`.

Both paths reject non-finite coordinates and report malformed structure through `Result`. File normals are retained but geometric checks calculate areas and shape from vertex positions. Duplicate vertices use exact `f32` values, with signed zero normalized; nearby coordinates are intentionally not welded.

Quality thresholds are configurable in `config.json`. A sliver has a minimum corner angle below `sliver_min_angle_degrees` or an aspect ratio above `sliver_aspect_ratio`. A large triangle has a longest edge strictly greater than `large_triangle_edge_to_diagonal_ratio` times the bounding-box diagonal. Incident-edge counts use unique, non-self indexed edges.
