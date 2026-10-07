use crate::geometry::{Triangle, Vec3};
use std::collections::HashMap;

const RELATIVE_WELD_TOLERANCE: f64 = 1.0e-6;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct IndexedTriangle {
    pub indices: [usize; 3],
}

#[derive(Debug, Clone)]
pub struct Mesh {
    pub vertices: Vec<Vec3>,
    pub triangles: Vec<IndexedTriangle>,
}

impl Mesh {
    pub fn from_triangles(triangles: Vec<Triangle>) -> Self {
        let (min, max, max_abs) = triangles
            .iter()
            .flat_map(|triangle| triangle.vertices)
            .fold(
                ([f64::INFINITY; 3], [f64::NEG_INFINITY; 3], 0.0_f64),
                |(mut min, mut max, max_abs), vertex| {
                    for (axis, value) in [vertex.x, vertex.y, vertex.z].into_iter().enumerate() {
                        let value = f64::from(value);
                        min[axis] = min[axis].min(value);
                        max[axis] = max[axis].max(value);
                    }
                    let max_abs = max_abs
                        .max(f64::from(vertex.x).abs())
                        .max(f64::from(vertex.y).abs())
                        .max(f64::from(vertex.z).abs());
                    (min, max, max_abs)
                },
            );
        let diagonal = if min[0].is_finite() {
            ((max[0] - min[0]).powi(2) + (max[1] - min[1]).powi(2) + (max[2] - min[2]).powi(2))
                .sqrt()
        } else {
            0.0
        };
        let tolerance =
            (diagonal * RELATIVE_WELD_TOLERANCE).max(max_abs * f64::from(f32::EPSILON) * 4.0);
        let cell_size = if tolerance > 0.0 { tolerance } else { 1.0 };

        let mut vertices: Vec<Vec3> = Vec::new();
        let mut lookup: HashMap<[i64; 3], Vec<usize>> =
            HashMap::with_capacity(triangles.len().saturating_mul(2));
        let mut indexed = Vec::with_capacity(triangles.len());

        for triangle in triangles {
            let mut indices = [0; 3];
            for (slot, vertex) in triangle.vertices.into_iter().enumerate() {
                let point = [
                    f64::from(vertex.x),
                    f64::from(vertex.y),
                    f64::from(vertex.z),
                ];
                let cell = point.map(|value| (value / cell_size).floor() as i64);
                let mut match_index = None;
                'neighbors: for x in -1..=1 {
                    for y in -1..=1 {
                        for z in -1..=1 {
                            let neighbor = [
                                cell[0].saturating_add(x),
                                cell[1].saturating_add(y),
                                cell[2].saturating_add(z),
                            ];
                            if let Some(candidates) = lookup.get(&neighbor) {
                                match_index = candidates.iter().copied().find(|&candidate| {
                                    let existing = vertices[candidate];
                                    let dx = point[0] - f64::from(existing.x);
                                    let dy = point[1] - f64::from(existing.y);
                                    let dz = point[2] - f64::from(existing.z);
                                    dx * dx + dy * dy + dz * dz <= tolerance * tolerance
                                });
                                if match_index.is_some() {
                                    break 'neighbors;
                                }
                            }
                        }
                    }
                }
                indices[slot] = match match_index {
                    Some(index) => index,
                    None => {
                        let index = vertices.len();
                        vertices.push(vertex);
                        lookup.entry(cell).or_default().push(index);
                        index
                    }
                };
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
