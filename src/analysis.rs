use std::cmp::Reverse;
use std::collections::{BinaryHeap, HashSet};

use crate::geometry::Vec3;
use crate::mesh::Mesh;

const TOP_VERTEX_COUNT: usize = 10;
const SLIVER_QUALITY_THRESHOLD: f64 = 0.05;
const MICRO_EDGE_RATIO: f64 = 1e-6;
const LARGE_EDGE_RATIO: f64 = 0.1;
const EXTREMELY_LARGE_EDGE_RATIO: f64 = 0.5;

#[derive(Clone, Copy, Debug, PartialEq)]
pub struct Bounds {
    pub min: Vec3,
    pub max: Vec3,
}

#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct TriangleSizeCounts {
    pub micro: usize,
    pub medium: usize,
    pub large: usize,
    pub extremely_large: usize,
}

#[derive(Clone, Copy, Debug, Default, PartialEq)]
pub struct TriangleShapeCounts {
    pub sliver: usize,
}

#[derive(Clone, Copy, Debug, PartialEq)]
pub struct VertexIncidence {
    pub position: Vec3,
    pub edge_count: usize,
}

#[derive(Clone, Debug, PartialEq)]
pub struct AnalysisReport {
    pub triangle_count: usize,
    pub unique_vertex_count: usize,
    pub surface_area: f64,
    pub degenerate_triangles: usize,
    pub bounds: Option<Bounds>,
    pub triangle_sizes: TriangleSizeCounts,
    pub triangle_shapes: TriangleShapeCounts,
    pub high_incidence_vertices: Vec<VertexIncidence>,
}

pub fn analyze(mesh: &Mesh) -> AnalysisReport {
    let bounds = mesh.bounds().map(|(min, max)| Bounds { min, max });
    let model_diagonal = bounds
        .map(|bounds| (bounds.max - bounds.min).length())
        .unwrap_or(0.0);
    let mut surface_area = 0.0;
    let mut degenerate_triangles = 0;
    let mut triangle_sizes = TriangleSizeCounts::default();
    let mut triangle_shapes = TriangleShapeCounts::default();
    for triangle in mesh.triangles() {
        let area = triangle.area();
        surface_area += area;
        let is_degenerate = area == 0.0;
        if is_degenerate {
            degenerate_triangles += 1;
        }
        classify_triangle(
            triangle,
            area,
            model_diagonal,
            is_degenerate,
            &mut triangle_sizes,
            &mut triangle_shapes,
        );
    }

    let high_incidence_vertices = measure_vertex_edge_degrees(mesh);

    AnalysisReport {
        triangle_count: mesh.len(),
        unique_vertex_count: mesh.vertex_count(),
        surface_area,
        degenerate_triangles,
        bounds,
        triangle_sizes,
        triangle_shapes,
        high_incidence_vertices,
    }
}

fn classify_triangle(
    triangle: crate::geometry::Triangle,
    area: f64,
    model_diagonal: f64,
    is_degenerate: bool,
    counts: &mut TriangleSizeCounts,
    shapes: &mut TriangleShapeCounts,
) {
    let [a, b, c] = triangle.vertices;
    let edge_ab = (b - a).length();
    let edge_bc = (c - b).length();
    let edge_ca = (a - c).length();
    let longest_edge = edge_ab.max(edge_bc).max(edge_ca);
    let squared_edge_sum = edge_ab * edge_ab + edge_bc * edge_bc + edge_ca * edge_ca;

    if !is_degenerate
        && squared_edge_sum > 0.0
        && 4.0 * 3.0_f64.sqrt() * area / squared_edge_sum < SLIVER_QUALITY_THRESHOLD
    {
        shapes.sliver += 1;
    }

    let relative_size = if model_diagonal > 0.0 {
        longest_edge / model_diagonal
    } else {
        0.0
    };
    if relative_size <= MICRO_EDGE_RATIO {
        counts.micro += 1;
    } else if relative_size < LARGE_EDGE_RATIO {
        counts.medium += 1;
    } else if relative_size < EXTREMELY_LARGE_EDGE_RATIO {
        counts.large += 1;
    } else {
        counts.extremely_large += 1;
    }
}

fn measure_vertex_edge_degrees(mesh: &Mesh) -> Vec<VertexIncidence> {
    let vertices = mesh.vertices();
    let mut edge_counts = vec![0; vertices.len()];
    let mut edges = HashSet::<(usize, usize)>::new();

    for [a, b, c] in mesh.triangle_indices() {
        let indices = [*a, *b, *c];
        for (left, right) in [
            (indices[0], indices[1]),
            (indices[1], indices[2]),
            (indices[2], indices[0]),
        ] {
            if left == right {
                continue;
            }
            let edge = if left <= right {
                (left, right)
            } else {
                (right, left)
            };
            if edges.insert(edge) {
                edge_counts[left] += 1;
                edge_counts[right] += 1;
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
            position: vertices[index],
            edge_count,
        })
        .collect();
    result.sort_unstable_by_key(|vertex| Reverse(vertex.edge_count));
    result
}
