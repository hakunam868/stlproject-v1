use stl_analyzer::analysis::{analyze, BoundingBox, Thresholds};
use stl_analyzer::geometry::{Mesh, Triangle, Vec3};

fn mesh(triangles: Vec<[Vec3; 3]>) -> Mesh {
    Mesh::from_triangles(triangles.into_iter().map(Triangle::new).collect())
}

#[test]
fn reports_counts_and_bounds() {
    let mesh = mesh(vec![[
        Vec3::new(0.0, 0.0, 0.0),
        Vec3::new(2.0, 0.0, 0.0),
        Vec3::new(0.0, 2.0, 0.0),
    ]]);
    assert_eq!(mesh.triangles.len(), 1);
    assert_eq!(mesh.vertices.len(), 3);
    let bounds = BoundingBox::from_vertices(&mesh.vertices).unwrap();
    assert_eq!(bounds.min, Vec3::new(0.0, 0.0, 0.0));
    assert_eq!(bounds.max, Vec3::new(2.0, 2.0, 0.0));
}

#[test]
fn finds_sliver_and_large_triangles() {
    let mesh = mesh(vec![
        [
            Vec3::ZERO,
            Vec3::new(10.0, 0.0, 0.0),
            Vec3::new(5.0, 0.001, 0.0),
        ],
        [
            Vec3::ZERO,
            Vec3::new(10.0, 0.0, 0.0),
            Vec3::new(0.0, 10.0, 0.0),
        ],
        [
            Vec3::new(0.0, 0.0, 10.0),
            Vec3::new(1.0, 0.0, 10.0),
            Vec3::new(0.0, 1.0, 10.0),
        ],
    ]);
    let thresholds = Thresholds {
        large_triangle_area_ratio: 0.1,
        ..Thresholds::default()
    };
    let report = analyze(&mesh, thresholds);
    assert_eq!(report.slivers.len(), 1);
    assert_eq!(report.large_triangles.len(), 1);
}

#[test]
fn reports_exact_unique_vertices_and_incident_edges() {
    let a = Vec3::new(0.0, 0.0, 0.0);
    let b = Vec3::new(1.0, 0.0, 0.0);
    let c = Vec3::new(0.0, 1.0, 0.0);
    let mesh = mesh(vec![[a, b, c], [a, b, c], [a, a, b]]);
    let report = analyze(
        &mesh,
        Thresholds {
            max_incident_edges: 1,
            ..Thresholds::default()
        },
    );
    assert_eq!(mesh.vertices.len(), 3);
    assert!(report
        .high_incident_vertices
        .iter()
        .any(|vertex| vertex.position == a && vertex.incident_edges == 2));
}
