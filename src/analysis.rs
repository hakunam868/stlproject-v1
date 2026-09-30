use std::cmp::Reverse;
use std::collections::{BinaryHeap, HashMap, HashSet};
use std::hash::{Hash, Hasher};

use crate::geometry::Vec3;
use crate::mesh::Mesh;

const TOP_VERTEX_COUNT: usize = 10;

#[derive(Clone, Copy, Debug, PartialEq)]
pub struct Bounds {
    pub min: Vec3,
    pub max: Vec3,
}

#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct TriangleSizeCounts {
    pub sliver: usize,
    pub medium: usize,
    pub large: usize,
    pub extremely_large: usize,
}

#[derive(Clone, Copy, Debug, PartialEq)]
pub struct VertexIncidence {
    pub position: Vec3,
    pub edge_count: usize,
}

#[derive(Clone, Debug, PartialEq)]
pub struct AnalysisReport {
    pub triangle_count: usize,
    pub surface_area: f64,
    pub degenerate_triangles: usize,
    pub bounds: Option<Bounds>,
    pub triangle_sizes: TriangleSizeCounts,
    pub high_incidence_vertices: Vec<VertexIncidence>,
}

pub fn analyze(mesh: &Mesh) -> AnalysisReport {
    let mut surface_area = 0.0;
    let mut degenerate_triangles = 0;
    let mut triangle_sizes = TriangleSizeCounts::default();
    for triangle in mesh.triangles() {
        let area = triangle.area();
        surface_area += area;
        if area <= f64::EPSILON {
            degenerate_triangles += 1;
        }
        classify_triangle(*triangle, area, &mut triangle_sizes);
    }

    let bounds = mesh.bounds().map(|(min, max)| Bounds { min, max });
    let high_incidence_vertices = measure_vertex_edge_degrees(mesh);

    AnalysisReport {
        triangle_count: mesh.len(),
        surface_area,
        degenerate_triangles,
        bounds,
        triangle_sizes,
        high_incidence_vertices,
    }
}

fn classify_triangle(
    triangle: crate::geometry::Triangle,
    area: f64,
    counts: &mut TriangleSizeCounts,
) {
    let [a, b, c] = triangle.vertices;
    let longest_edge = (b - a).length().max((c - b).length()).max((a - c).length());
    if longest_edge > 0.0 && area / (longest_edge * longest_edge) < 0.01 {
        counts.sliver += 1;
    } else if longest_edge < 10.0 {
        counts.medium += 1;
    } else if longest_edge < 1_000.0 {
        counts.large += 1;
    } else {
        counts.extremely_large += 1;
    }
}

fn measure_vertex_edge_degrees(mesh: &Mesh) -> Vec<VertexIncidence> {
    let mut vertex_indices = HashMap::<VertexKey, u32>::new();
    let mut vertex_positions = Vec::new();
    let mut edge_counts = Vec::<usize>::new();
    let mut edges = HashSet::<(u32, u32)>::new();

    for triangle in mesh.triangles() {
        let [a, b, c] = triangle.vertices;
        let indices = [a, b, c].map(|position| {
            let key = VertexKey::from(position);
            let next_index = vertex_positions.len() as u32;
            *vertex_indices.entry(key).or_insert_with(|| {
                vertex_positions.push(position);
                edge_counts.push(0);
                next_index
            })
        });

        for (left, right) in [
            (indices[0], indices[1]),
            (indices[1], indices[2]),
            (indices[2], indices[0]),
        ] {
            let edge = if left <= right {
                (left, right)
            } else {
                (right, left)
            };
            if edges.insert(edge) {
                edge_counts[left as usize] += 1;
                edge_counts[right as usize] += 1;
            }
        }
    }

    let mut top = BinaryHeap::<Reverse<(usize, usize)>>::with_capacity(TOP_VERTEX_COUNT);
    for (index, &edge_count) in edge_counts.iter().enumerate() {
        let candidate = Reverse((edge_count, index));
        if top.len() < TOP_VERTEX_COUNT {
            top.push(candidate);
        } else if top.peek().is_some_and(|smallest| candidate > *smallest) {
            top.pop();
            top.push(candidate);
        }
    }

    let mut result: Vec<_> = top
        .into_iter()
        .map(|Reverse((edge_count, index))| VertexIncidence {
            position: vertex_positions[index],
            edge_count,
        })
        .collect();
    result.sort_unstable_by(|left, right| right.edge_count.cmp(&left.edge_count));
    result
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
