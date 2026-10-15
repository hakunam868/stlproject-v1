use crate::geometry::{Triangle, Vec3};
use rustc_hash::FxHashMap;

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
        let (min, max, max_abs, min_edge_squared) = triangles.iter().fold(
            (
                [f64::INFINITY; 3],
                [f64::NEG_INFINITY; 3],
                0.0_f64,
                f64::INFINITY,
            ),
            |(mut min, mut max, mut max_abs, mut min_edge_squared), triangle| {
                let vertices = triangle.vertices;
                for vertex in vertices {
                    for (axis, value) in [vertex.x, vertex.y, vertex.z].into_iter().enumerate() {
                        let value = f64::from(value);
                        min[axis] = min[axis].min(value);
                        max[axis] = max[axis].max(value);
                    }
                    max_abs = max_abs
                        .max(f64::from(vertex.x).abs())
                        .max(f64::from(vertex.y).abs())
                        .max(f64::from(vertex.z).abs());
                }
                for (start, end) in [
                    (vertices[0], vertices[1]),
                    (vertices[1], vertices[2]),
                    (vertices[2], vertices[0]),
                ] {
                    let distance_squared = distance_squared(start, end);
                    if distance_squared > 0.0 {
                        min_edge_squared = min_edge_squared.min(distance_squared);
                    }
                }
                (min, max, max_abs, min_edge_squared)
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
        let tolerance = if min_edge_squared.is_finite() {
            tolerance.min(min_edge_squared.sqrt() * 0.5)
        } else {
            tolerance
        };
        let tolerance_squared = tolerance * tolerance;
        let cell_size = if tolerance > 0.0 {
            tolerance * 2.0
        } else {
            1.0
        };

        let mut vertices: Vec<Vec3> = Vec::new();
        let mut lookup: FxHashMap<[i64; 3], (usize, usize)> =
            FxHashMap::with_capacity_and_hasher(triangles.len(), Default::default());
        let mut exact_lookup: FxHashMap<[u32; 3], usize> =
            FxHashMap::with_capacity_and_hasher(triangles.len(), Default::default());
        let mut next_in_cell = Vec::with_capacity(triangles.len());
        let mut indexed = Vec::with_capacity(triangles.len());

        for triangle in triangles {
            let mut indices = [0; 3];
            for (slot, vertex) in triangle.vertices.into_iter().enumerate() {
                let exact_key = exact_key(vertex);
                if let Some(index) = exact_key.and_then(|key| exact_lookup.get(&key).copied()) {
                    indices[slot] = index;
                    continue;
                }
                let point = [
                    f64::from(vertex.x),
                    f64::from(vertex.y),
                    f64::from(vertex.z),
                ];
                let cell = point.map(|value| (value / cell_size).floor() as i64);
                let (x_neighbors, x_count) = neighboring_cells(point[0], cell_size, tolerance);
                let (y_neighbors, y_count) = neighboring_cells(point[1], cell_size, tolerance);
                let (z_neighbors, z_count) = neighboring_cells(point[2], cell_size, tolerance);
                let mut match_index = None;
                'neighbors: for &x in &x_neighbors[..x_count] {
                    for &y in &y_neighbors[..y_count] {
                        for &z in &z_neighbors[..z_count] {
                            let neighbor = [
                                cell[0].saturating_add(x),
                                cell[1].saturating_add(y),
                                cell[2].saturating_add(z),
                            ];
                            let mut candidate = lookup.get(&neighbor).map(|&(head, _)| head);
                            while let Some(candidate_index) = candidate {
                                let existing = vertices[candidate_index];
                                let dx = point[0] - f64::from(existing.x);
                                let dy = point[1] - f64::from(existing.y);
                                let dz = point[2] - f64::from(existing.z);
                                if dx * dx + dy * dy + dz * dz <= tolerance_squared {
                                    match_index = Some(candidate_index);
                                    break 'neighbors;
                                }
                                candidate = (next_in_cell[candidate_index] != usize::MAX)
                                    .then_some(next_in_cell[candidate_index]);
                            }
                        }
                    }
                }
                indices[slot] = match match_index {
                    Some(index) => index,
                    None => {
                        let index = vertices.len();
                        vertices.push(vertex);
                        next_in_cell.push(usize::MAX);
                        match lookup.entry(cell) {
                            std::collections::hash_map::Entry::Occupied(mut entry) => {
                                let (head, tail) = *entry.get();
                                next_in_cell[tail] = index;
                                entry.insert((head, index));
                            }
                            std::collections::hash_map::Entry::Vacant(entry) => {
                                entry.insert((index, index));
                            }
                        }
                        index
                    }
                };
                if let Some(key) = exact_key {
                    exact_lookup.entry(key).or_insert(indices[slot]);
                }
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

fn neighboring_cells(value: f64, cell_size: f64, tolerance: f64) -> ([i64; 3], usize) {
    let scaled = value / cell_size;
    if !scaled.is_finite() {
        return ([-1, 0, 1], 3);
    }
    let fraction = scaled - scaled.floor();
    let mut neighbors = [0; 3];
    let mut len = 0;
    if fraction * cell_size <= tolerance {
        neighbors[len] = -1;
        len += 1;
    }
    neighbors[len] = 0;
    len += 1;
    if (1.0 - fraction) * cell_size <= tolerance {
        neighbors[len] = 1;
        len += 1;
    }
    (neighbors, len)
}

fn exact_key(vertex: Vec3) -> Option<[u32; 3]> {
    let coordinates = [vertex.x, vertex.y, vertex.z];
    coordinates
        .iter()
        .all(|coordinate| coordinate.is_finite())
        .then(|| {
            coordinates.map(|coordinate| if coordinate == 0.0 { 0.0 } else { coordinate }.to_bits())
        })
}

fn distance_squared(left: Vec3, right: Vec3) -> f64 {
    let dx = f64::from(left.x) - f64::from(right.x);
    let dy = f64::from(left.y) - f64::from(right.y);
    let dz = f64::from(left.z) - f64::from(right.z);
    dx * dx + dy * dy + dz * dz
}
