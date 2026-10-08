use crate::geometry::Vec3;
use crate::mesh::Mesh;

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct BoundingBox {
    pub min: Vec3,
    pub max: Vec3,
}

impl BoundingBox {
    pub fn from_vertices(vertices: &[Vec3]) -> Option<Self> {
        let first = *vertices.first()?;
        let mut bounds = Self {
            min: first,
            max: first,
        };
        for &vertex in &vertices[1..] {
            bounds.min.x = bounds.min.x.min(vertex.x);
            bounds.min.y = bounds.min.y.min(vertex.y);
            bounds.min.z = bounds.min.z.min(vertex.z);
            bounds.max.x = bounds.max.x.max(vertex.x);
            bounds.max.y = bounds.max.y.max(vertex.y);
            bounds.max.z = bounds.max.z.max(vertex.z);
        }
        Some(bounds)
    }

    pub fn diagonal_squared(self) -> f64 {
        let dx = f64::from(self.max.x) - f64::from(self.min.x);
        let dy = f64::from(self.max.y) - f64::from(self.min.y);
        let dz = f64::from(self.max.z) - f64::from(self.min.z);
        dx * dx + dy * dy + dz * dz
    }

    pub fn diagonal(self) -> f64 {
        self.diagonal_squared().sqrt()
    }
}

#[derive(Debug, Clone, Copy)]
pub struct Thresholds {
    pub sliver_aspect_ratio: f32,
    pub micro_triangle_area_ratio: f32,
    pub large_triangle_area_ratio: f32,
    pub max_incident_edges: usize,
}

impl Default for Thresholds {
    fn default() -> Self {
        Self {
            sliver_aspect_ratio: 20.0,
            micro_triangle_area_ratio: 1.0e-8,
            large_triangle_area_ratio: 0.25,
            max_incident_edges: 12,
        }
    }
}

#[derive(Debug)]
pub struct SliverTriangle {
    pub triangle: usize,
    pub area: f64,
    pub aspect_ratio: f64,
}
#[derive(Debug)]
pub struct LargeTriangle {
    pub triangle: usize,
    pub area: f64,
    pub relative_area: f64,
}
#[derive(Debug)]
pub struct MicroTriangle {
    pub triangle: usize,
    pub area: f64,
    pub relative_area: f64,
}
#[derive(Debug)]
pub struct HighIncidentVertex {
    pub vertex: usize,
    pub position: Vec3,
    pub incident_edges: usize,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct TopologyEdge {
    pub vertices: [usize; 2],
    pub incident_faces: usize,
}

#[derive(Debug)]
pub struct Analysis {
    pub bounds: Option<BoundingBox>,
    pub slivers: Vec<SliverTriangle>,
    pub micro_triangles: Vec<MicroTriangle>,
    pub large_triangles: Vec<LargeTriangle>,
    pub high_incident_vertices: Vec<HighIncidentVertex>,
    pub open_edges: Vec<TopologyEdge>,
    pub non_manifold_edges: Vec<TopologyEdge>,
    pub is_watertight: bool,
}

pub fn analyze(mesh: &Mesh, thresholds: Thresholds) -> Analysis {
    let bounds = BoundingBox::from_vertices(&mesh.vertices);
    let model_scale_squared = bounds.map_or(0.0, BoundingBox::diagonal_squared);
    let mut slivers = Vec::new();
    let mut micro_triangles = Vec::new();
    let mut large_triangles = Vec::new();
    let mut edges = Vec::with_capacity(mesh.triangles.len().saturating_mul(3));

    for (index, indexed) in mesh.triangles.iter().enumerate() {
        let [a, b, c] = indexed.indices;
        let vertices = [
            to_f64(mesh.vertices[a]),
            to_f64(mesh.vertices[b]),
            to_f64(mesh.vertices[c]),
        ];
        let edge_ab = subtract(vertices[1], vertices[0]);
        let edge_ac = subtract(vertices[2], vertices[0]);
        let area = 0.5 * length(cross(edge_ab, edge_ac));
        let longest_squared = distance_squared(vertices[0], vertices[1])
            .max(distance_squared(vertices[1], vertices[2]))
            .max(distance_squared(vertices[2], vertices[0]));
        let aspect_ratio = if area > 0.0 {
            longest_squared / (2.0 * area)
        } else {
            f64::INFINITY
        };
        if aspect_ratio >= f64::from(thresholds.sliver_aspect_ratio) {
            slivers.push(SliverTriangle {
                triangle: index,
                area,
                aspect_ratio,
            });
        }
        if area == 0.0
            || (model_scale_squared > 0.0
                && area / model_scale_squared <= f64::from(thresholds.micro_triangle_area_ratio))
        {
            micro_triangles.push(MicroTriangle {
                triangle: index,
                area,
                relative_area: if model_scale_squared > 0.0 {
                    area / model_scale_squared
                } else {
                    0.0
                },
            });
        }
        if model_scale_squared > 0.0
            && area / model_scale_squared >= f64::from(thresholds.large_triangle_area_ratio)
        {
            large_triangles.push(LargeTriangle {
                triangle: index,
                area,
                relative_area: area / model_scale_squared,
            });
        }
        let mut triangle_edges = [(usize::MAX, usize::MAX); 3];
        let mut triangle_edge_count = 0;
        for (start, end) in [(a, b), (b, c), (c, a)] {
            if start != end {
                let edge = (start.min(end), start.max(end));
                if !triangle_edges[..triangle_edge_count].contains(&edge) {
                    edges.push(edge);
                    triangle_edges[triangle_edge_count] = edge;
                    triangle_edge_count += 1;
                }
            }
        }
    }

    edges.sort_unstable();
    let mut degrees = vec![0_usize; mesh.vertices.len()];
    let mut open_edges = Vec::new();
    let mut non_manifold_edges = Vec::new();
    let mut edge_index = 0;
    while edge_index < edges.len() {
        let (a, b) = edges[edge_index];
        let mut end = edge_index + 1;
        while end < edges.len() && edges[end] == (a, b) {
            end += 1;
        }
        let incident_faces = end - edge_index;
        degrees[a] += 1;
        degrees[b] += 1;
        if incident_faces == 1 {
            open_edges.push(TopologyEdge {
                vertices: [a, b],
                incident_faces,
            });
        } else if incident_faces > 2 {
            non_manifold_edges.push(TopologyEdge {
                vertices: [a, b],
                incident_faces,
            });
        }
        edge_index = end;
    }
    let high_incident_vertices = degrees
        .into_iter()
        .enumerate()
        .filter_map(|(vertex, incident_edges)| {
            (incident_edges > thresholds.max_incident_edges).then(|| HighIncidentVertex {
                vertex,
                position: mesh.vertices[vertex],
                incident_edges,
            })
        })
        .collect();
    let is_watertight = !mesh.triangles.is_empty()
        && open_edges.is_empty()
        && non_manifold_edges.is_empty()
        && !micro_triangles.iter().any(|triangle| triangle.area == 0.0);
    Analysis {
        bounds,
        slivers,
        micro_triangles,
        large_triangles,
        high_incident_vertices,
        open_edges,
        non_manifold_edges,
        is_watertight,
    }
}

fn to_f64(vertex: Vec3) -> [f64; 3] {
    [
        f64::from(vertex.x),
        f64::from(vertex.y),
        f64::from(vertex.z),
    ]
}

fn subtract(left: [f64; 3], right: [f64; 3]) -> [f64; 3] {
    [left[0] - right[0], left[1] - right[1], left[2] - right[2]]
}

fn cross(left: [f64; 3], right: [f64; 3]) -> [f64; 3] {
    [
        left[1] * right[2] - left[2] * right[1],
        left[2] * right[0] - left[0] * right[2],
        left[0] * right[1] - left[1] * right[0],
    ]
}

fn length(vector: [f64; 3]) -> f64 {
    (vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]).sqrt()
}

fn distance_squared(left: [f64; 3], right: [f64; 3]) -> f64 {
    let delta = subtract(left, right);
    delta[0] * delta[0] + delta[1] * delta[1] + delta[2] * delta[2]
}
