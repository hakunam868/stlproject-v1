# STL format notes

The loader accepts both common STL encodings:

- Binary STL: an 80-byte header, a little-endian 32-bit triangle count, then 50 bytes per triangle.
- ASCII STL: each triangle is read from three `vertex x y z` records. Facet normals and other structural lines are ignored.

The binary loader verifies that the file length exactly matches the declared triangle count. The ASCII loader requires each coordinate to parse as a finite number and requires the vertex count to be divisible by three. Normals are not used for analysis; triangle area is computed from vertex positions.