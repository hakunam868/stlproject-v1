# STL format notes

The loader accepts both common STL encodings:

- Binary STL: an 80-byte header, a little-endian 32-bit triangle count, then 50 bytes per triangle.
- ASCII STL: each triangle is read from three `vertex x y z` records. Facet normals and other structural lines are ignored.

The binary loader verifies that the file length exactly matches the declared triangle count and rejects non-finite vertex coordinates. The ASCII loader requires each coordinate to parse as a finite number and requires the vertex count to be divisible by three. Normals are not used for analysis; triangle area is computed from vertex positions. Vertex deduplication uses exact coordinates, treating negative and positive zero as equal; it does not merge nearby points.

Triangle size is measured by longest-edge length divided by the model bounding-box diagonal. Micro, medium, large, and extremely large thresholds are respectively `<= 1e-6`, `< 0.1`, `< 0.5`, and `>= 0.5`. Shape-based sliver detection uses normalized quality `4 * sqrt(3) * area / (a^2 + b^2 + c^2)` and marks non-degenerate triangles below `0.05`; this is separate from size classification.