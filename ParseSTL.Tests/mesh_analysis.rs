use std::fs::{self, File};
use std::time::{SystemTime, UNIX_EPOCH};
use stl_analyzer::analysis::{analyze, BoundingBox, Thresholds};
use stl_analyzer::app::analyze_path;
use stl_analyzer::geometry::{Mesh, Triangle, Vec3};
use stl_analyzer::samples::{generate, SampleKind};
use stl_analyzer::stl::{parse_bytes, write_ascii};

fn mesh(triangles: Vec<[Vec3; 3]>) -> Mesh {
    Mesh::from_triangles(triangles.into_iter().map(Triangle::new).collect())
}

#[test]
fn reports_counts_and_bounds() {
    let mesh = mesh(vec![[
        Vec3::new(-1.0, 2.0, 3.0),
        Vec3::new(4.0, -2.0, 8.0),
        Vec3::new(0.0, 1.0, -4.0),
    ]]);
    assert_eq!(mesh.triangles.len(), 1);
    assert_eq!(mesh.vertices.len(), 3);
    let bounds = BoundingBox::from_vertices(&mesh.vertices).unwrap();
    assert_eq!(bounds.min, Vec3::new(-1.0, -2.0, -4.0));
    assert_eq!(bounds.max, Vec3::new(4.0, 2.0, 8.0));
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
fn computes_thin_triangle_metrics_with_f64_precision() {
    let height = 1.0e-3_f32;
    let mesh = mesh(vec![[
        Vec3::ZERO,
        Vec3::new(1.0e4, 0.0, 0.0),
        Vec3::new(1.0e4, height, 0.0),
    ]]);
    let report = analyze(&mesh, Thresholds::default());
    let sliver = &report.slivers[0];

    let height = f64::from(height);
    let expected_area = 0.5 * 1.0e4 * height;
    let expected_aspect = (1.0e8 + height * height) / (2.0 * expected_area);
    assert!((sliver.area - expected_area).abs() / expected_area < 1.0e-9);
    assert!((sliver.aspect_ratio - expected_aspect).abs() / expected_aspect < 1.0e-9);

    let bounds = BoundingBox::from_vertices(&mesh.vertices).unwrap();
    let expected_diagonal = (1.0e8 + height * height).sqrt();
    assert!((bounds.diagonal() - expected_diagonal).abs() / expected_diagonal < 1.0e-12);
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

#[test]
fn welds_generated_cube_and_prism_seams_and_finds_no_topology_defects() {
    for kind in [SampleKind::Cube, SampleKind::Prism] {
        let mut ascii = Vec::new();
        write_ascii(&mut ascii, "generated", &generate(kind, 8)).unwrap();
        let parsed = parse_bytes(&ascii).unwrap();
        let mesh = Mesh::from_triangles(parsed.triangles);
        let report = analyze(&mesh, Thresholds::default());
        assert!(
            report.open_edges.is_empty(),
            "{kind:?} had {} open edges",
            report.open_edges.len()
        );
        assert!(
            report.non_manifold_edges.is_empty(),
            "{kind:?} had {} non-manifold edges",
            report.non_manifold_edges.len()
        );
    }
}

#[test]
fn detects_open_and_non_manifold_edges() {
    let a = Vec3::ZERO;
    let b = Vec3::new(1.0, 0.0, 0.0);
    let mesh = mesh(vec![
        [a, b, Vec3::new(0.0, 1.0, 0.0)],
        [b, a, Vec3::new(0.0, -1.0, 0.0)],
        [a, b, Vec3::new(0.0, 0.0, 1.0)],
    ]);

    let report = analyze(&mesh, Thresholds::default());

    assert_eq!(report.open_edges.len(), 6);
    assert_eq!(report.non_manifold_edges.len(), 1);
    assert_eq!(report.non_manifold_edges[0].incident_faces, 3);
}

#[test]
fn counts_a_degenerate_triangle_edge_only_once() {
    let mesh = mesh(vec![[Vec3::ZERO, Vec3::new(1.0, 0.0, 0.0), Vec3::ZERO]]);
    let report = analyze(&mesh, Thresholds::default());

    assert_eq!(report.open_edges.len(), 1);
    assert!(report.non_manifold_edges.is_empty());
}

#[test]
fn report_includes_open_and_non_manifold_edge_findings() {
    let stamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!("stl-topology-{stamp}.stl"));
    let triangles = [
        Triangle::new([
            Vec3::ZERO,
            Vec3::new(1.0, 0.0, 0.0),
            Vec3::new(0.0, 1.0, 0.0),
        ]),
        Triangle::new([
            Vec3::new(1.0, 0.0, 0.0),
            Vec3::ZERO,
            Vec3::new(0.0, -1.0, 0.0),
        ]),
        Triangle::new([
            Vec3::ZERO,
            Vec3::new(1.0, 0.0, 0.0),
            Vec3::new(0.0, 0.0, 1.0),
        ]),
    ];
    write_ascii(&mut File::create(&path).unwrap(), "topology", &triangles).unwrap();

    let report = analyze_path(&path, Thresholds::default()).unwrap();

    fs::remove_file(path).unwrap();
    assert!(report.contains("Open edge"));
    assert!(report.contains("Non-manifold edge"));
}

#[test]
fn welds_vertices_with_small_floating_point_differences() {
    let a = Vec3::ZERO;
    let b = Vec3::new(1.0, 0.0, 0.0);
    let shifted_b = Vec3::new(1.0 + 2.0e-7, 0.0, 0.0);
    let mesh = mesh(vec![
        [a, b, Vec3::new(0.0, 1.0, 0.0)],
        [shifted_b, a, Vec3::new(0.0, -1.0, 0.0)],
    ]);

    assert_eq!(mesh.vertices.len(), 4);
    assert_eq!(analyze(&mesh, Thresholds::default()).open_edges.len(), 4);
}

#[test]
fn finds_micro_triangles_relative_to_model_size() {
    let mesh = mesh(vec![
        [
            Vec3::ZERO,
            Vec3::new(10.0, 0.0, 0.0),
            Vec3::new(0.0, 10.0, 0.0),
        ],
        [
            Vec3::new(0.0, 0.0, 10.0),
            Vec3::new(0.01, 0.0, 10.0),
            Vec3::new(0.0, 0.01, 10.0),
        ],
    ]);
    let report = analyze(
        &mesh,
        Thresholds {
            micro_triangle_area_ratio: 1.0e-6,
            ..Thresholds::default()
        },
    );
    assert_eq!(report.micro_triangles.len(), 1);
    assert_eq!(report.micro_triangles[0].triangle, 1);
}

#[test]
fn treats_zero_area_triangles_as_micro_even_for_zero_size_meshes() {
    let mesh = mesh(vec![[Vec3::ZERO, Vec3::ZERO, Vec3::ZERO]]);
    let report = analyze(&mesh, Thresholds::default());
    assert_eq!(report.micro_triangles.len(), 1);
    assert_eq!(report.micro_triangles[0].relative_area, 0.0);
}

#[test]
fn report_includes_micro_triangle_findings() {
    let stamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!("stl-micro-triangle-{stamp}.stl"));
    let triangles = [
        Triangle::new([
            Vec3::ZERO,
            Vec3::new(10.0, 0.0, 0.0),
            Vec3::new(0.0, 10.0, 0.0),
        ]),
        Triangle::new([
            Vec3::new(0.0, 0.0, 10.0),
            Vec3::new(0.01, 0.0, 10.0),
            Vec3::new(0.0, 0.01, 10.0),
        ]),
    ];
    write_ascii(&mut File::create(&path).unwrap(), "micro", &triangles).unwrap();
    let report = analyze_path(
        &path,
        Thresholds {
            micro_triangle_area_ratio: 1.0e-6,
            ..Thresholds::default()
        },
    )
    .unwrap();
    fs::remove_file(path).unwrap();
    assert!(report.contains("Micro triangle #1"));
}
