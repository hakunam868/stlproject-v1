use crate::geometry::{Triangle, Vec3};
use std::collections::HashMap;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct IndexedTriangle {
    pub indices: [usize; 3],
}

#[derive(Debug, Clone)]
pub struct Mesh {
    pub vertices: Vec<Vec3>,
    pub triangles: Vec<IndexedTriangle>,
}

#[derive(Hash, PartialEq, Eq)]
struct VertexKey([u32; 3]);

impl VertexKey {
    fn from_vertex(vertex: Vec3) -> Self {
        fn bits(value: f32) -> u32 {
            if value == 0.0 {
                0
            } else {
                value.to_bits()
            }
        }
        Self([bits(vertex.x), bits(vertex.y), bits(vertex.z)])
    }
}

impl Mesh {
    pub fn from_triangles(triangles: Vec<Triangle>) -> Self {
        let mut vertices = Vec::new();
        let mut lookup = HashMap::with_capacity(triangles.len().saturating_mul(2));
        let mut indexed = Vec::with_capacity(triangles.len());

        for triangle in triangles {
            let mut indices = [0; 3];
            for (slot, vertex) in triangle.vertices.into_iter().enumerate() {
                let key = VertexKey::from_vertex(vertex);
                let next = vertices.len();
                indices[slot] = *lookup.entry(key).or_insert_with(|| {
                    vertices.push(vertex);
                    next
                });
            }
            indexed.push(IndexedTriangle { indices });
        }
        Self {
            vertices,
            triangles: indexed,
        }
    }

    pub fn triangle(&self, index: usize) -> Option<Triangle> {
        let indices = self.triangles.get(index)?.indices;
        Some(Triangle::new([
            *self.vertices.get(indices[0])?,
            *self.vertices.get(indices[1])?,
            *self.vertices.get(indices[2])?,
        ]))
    }
}
