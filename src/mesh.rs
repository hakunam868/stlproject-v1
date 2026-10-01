use std::collections::HashMap;
use std::hash::{Hash, Hasher};

use crate::geometry::{Triangle, Vec3};

#[derive(Clone, Debug, Default, PartialEq)]
pub struct Mesh {
    vertices: Vec<Vec3>,
    triangle_indices: Vec<[usize; 3]>,
}

impl Mesh {
    pub fn new(triangles: Vec<Triangle>) -> Self {
        let mut vertex_indices = HashMap::<VertexKey, usize>::new();
        let mut vertices = Vec::new();
        let mut triangle_indices = Vec::with_capacity(triangles.len());

        for triangle in triangles {
            let indices = triangle.vertices.map(|position| {
                let key = VertexKey::from(position);
                let next_index = vertices.len();
                *vertex_indices.entry(key).or_insert_with(|| {
                    vertices.push(position);
                    next_index
                })
            });
            triangle_indices.push(indices);
        }

        Self {
            vertices,
            triangle_indices,
        }
    }

    pub fn triangles(&self) -> impl Iterator<Item = Triangle> + '_ {
        self.triangle_indices
            .iter()
            .map(|[a, b, c]| Triangle::new(self.vertices[*a], self.vertices[*b], self.vertices[*c]))
    }

    pub fn vertices(&self) -> &[Vec3] {
        &self.vertices
    }

    pub fn triangle_indices(&self) -> &[[usize; 3]] {
        &self.triangle_indices
    }

    pub fn vertex_count(&self) -> usize {
        self.vertices.len()
    }

    pub fn len(&self) -> usize {
        self.triangle_indices.len()
    }

    pub fn is_empty(&self) -> bool {
        self.triangle_indices.is_empty()
    }

    pub fn bounds(&self) -> Option<(Vec3, Vec3)> {
        let mut vertices = self.vertices.iter().copied();
        let first = vertices.next()?;
        Some(vertices.fold((first, first), |(min, max), vertex| {
            (min.min(vertex), max.max(vertex))
        }))
    }
}

#[derive(Clone, Copy, Eq)]
struct VertexKey([u64; 3]);

impl From<Vec3> for VertexKey {
    fn from(position: Vec3) -> Self {
        fn normalized_bits(value: f64) -> u64 {
            if value == 0.0 {
                0
            } else {
                value.to_bits()
            }
        }

        Self([
            normalized_bits(position.x),
            normalized_bits(position.y),
            normalized_bits(position.z),
        ])
    }
}

impl PartialEq for VertexKey {
    fn eq(&self, other: &Self) -> bool {
        self.0 == other.0
    }
}

impl Hash for VertexKey {
    fn hash<H: Hasher>(&self, state: &mut H) {
        self.0.hash(state);
    }
}
