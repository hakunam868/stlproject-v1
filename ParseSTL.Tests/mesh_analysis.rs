use std::collections::HashSet;
use std::fs::{self, File};
use std::time::{SystemTime, UNIX_EPOCH};
use stl_analyzer::analysis::{analyze, BoundingBox, Thresholds};
use stl_analyzer::app::analyze_path;
use stl_analyzer::geometry::{IndexedTriangle, Mesh, Triangle, Vec3};
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
    let report = analyze(&mesh, Thresholds::default());
    assert_eq!(report.slivers.len(), 1);
    assert_eq!(report.large_triangles.len(), 2);
    assert_eq!(report.large_triangles[0].triangle, 0);
    assert_eq!(report.large_triangles[1].triangle, 1);
}

#[test]
fn flags_cube_face_triangle_by_longest_edge_to_model_diagonal_ratio() {
    let mesh = mesh(vec![
        [
            Vec3::ZERO,
            Vec3::new(10.0, 0.0, 0.0),
            Vec3::new(0.0, 10.0, 0.0),
        ],
        [
            Vec3::new(0.0, 0.0, 10.0),
            Vec3::new(0.0, 0.0, 10.0),
            Vec3::new(0.0, 0.0, 10.0),
        ],
    ]);

    let report = analyze(&mesh, Thresholds::default());

    assert_eq!(report.large_triangles.len(), 1);
    assert_eq!(report.large_triangles[0].triangle, 0);
    assert!((report.large_triangles[0].longest_edge - 10.0_f64 * 2.0_f64.sqrt()).abs() < 1.0e-6);
    assert!(report.large_triangles[0].edge_to_diagonal_ratio > 0.25);
}

#[test]
fn computes_thin_triangle_metrics_with_f64_precision() {
    let height = 1.0e-3_f32;
    let mesh = Mesh {
        vertices: vec![
            Vec3::ZERO,
            Vec3::new(1.0e4, 0.0, 0.0),
            Vec3::new(1.0e4, height, 0.0),
        ],
        triangles: vec![IndexedTriangle { indices: [0, 1, 2] }],
    };
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
fn welding_preserves_short_edges_in_large_models() {
    let height = 1.0e-3_f32;
    let mesh = mesh(vec![[
        Vec3::ZERO,
        Vec3::new(1.0e4, 0.0, 0.0),
        Vec3::new(1.0e4, height, 0.0),
    ]]);
    let report = analyze(&mesh, Thresholds::default());

    assert_eq!(mesh.vertices.len(), 3);
    assert_eq!(report.micro_triangles.len(), 0);
    let expected_area = 0.5 * 1.0e4 * f64::from(height);
    assert!((report.slivers[0].area - expected_area).abs() / expected_area < 1.0e-9);
}

#[test]
fn flags_acute_triangle_by_minimum_angle_even_when_aspect_is_below_threshold() {
    let mesh = mesh(vec![[
        Vec3::ZERO,
        Vec3::new(10.0, 0.0, 0.0),
        Vec3::new(1.0, 0.05, 0.0),
    ]]);
    let report = analyze(
        &mesh,
        Thresholds {
            sliver_aspect_ratio: 300.0,
            ..Thresholds::default()
        },
    );

    assert_eq!(report.slivers.len(), 1);
    assert!(report.slivers[0].aspect_ratio < 300.0);
}

#[test]
fn degenerate_triangle_is_micro_but_not_a_sliver() {
    let mesh = mesh(vec![[Vec3::ZERO, Vec3::new(1.0, 0.0, 0.0), Vec3::ZERO]]);
    let report = analyze(&mesh, Thresholds::default());

    assert!(report.slivers.is_empty());
    assert_eq!(report.micro_triangles.len(), 1);
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
fn generated_shapes_are_watertight_and_have_expected_topology() {
    for kind in [
        SampleKind::Prism,
        SampleKind::Cube,
        SampleKind::Torus,
        SampleKind::Cylinder,
        SampleKind::Cone,
        SampleKind::Sphere,
        SampleKind::Tetrahedron,
    ] {
        let mut ascii = Vec::new();
        write_ascii(&mut ascii, "generated", &generate(kind, 8).unwrap()).unwrap();
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
        assert!(report.is_watertight, "{kind:?} was not marked watertight");
        assert!(
            report.micro_triangles.is_empty(),
            "{kind:?} had {} micro triangles",
            report.micro_triangles.len()
        );
        let edges = mesh
            .triangles
            .iter()
            .flat_map(|triangle| {
                let [a, b, c] = triangle.indices;
                [
                    (a.min(b), a.max(b)),
                    (b.min(c), b.max(c)),
                    (c.min(a), c.max(a)),
                ]
            })
            .collect::<HashSet<_>>();
        let expected_euler_characteristic = if kind == SampleKind::Torus { 0 } else { 2 };
        assert_eq!(
            mesh.vertices.len() as isize - edges.len() as isize + mesh.triangles.len() as isize,
            expected_euler_characteristic,
            "{kind:?} had an unexpected Euler characteristic"
        );
        assert!(
            signed_volume(&mesh) > 0.0,
            "{kind:?} did not have positive signed volume"
        );
    }
}

fn signed_volume(mesh: &Mesh) -> f64 {
    mesh.triangles
        .iter()
        .map(|triangle| {
            let [a, b, c] = triangle.indices.map(|index| {
                let vertex = mesh.vertices[index];
                [
                    f64::from(vertex.x),
                    f64::from(vertex.y),
                    f64::from(vertex.z),
                ]
            });
            a[0] * (b[1] * c[2] - b[2] * c[1])
                + a[1] * (b[2] * c[0] - b[0] * c[2])
                + a[2] * (b[0] * c[1] - b[1] * c[0])
        })
        .sum::<f64>()
        / 6.0
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
    assert!(!report.is_watertight);
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
    assert!(report.contains("Topology: not watertight (FAIL; 6 open edges, 1 non-manifold edges)"));
    assert!(report.contains("Open edge"));
    assert!(report.contains("Non-manifold edge"));
}

#[test]
fn report_summarizes_quality_percentages_and_status() {
    let stamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!("stl-quality-summary-{stamp}.stl"));
    let triangles = generate(SampleKind::Tetrahedron, 1).unwrap();
    write_ascii(&mut File::create(&path).unwrap(), "tetrahedron", &triangles).unwrap();

    let passing_report = analyze_path(
        &path,
        Thresholds {
            large_triangle_edge_to_diagonal_ratio: 2.0,
            ..Thresholds::default()
        },
    )
    .unwrap();
    assert!(passing_report.contains("slivers 0/4 (0.0%)"));
    assert!(passing_report.contains("Quality status: PASS (0 findings)"));

    let configured_limit_report = analyze_path(
        &path,
        Thresholds {
            max_quality_issue_percentage: 100.0,
            ..Thresholds::default()
        },
    )
    .unwrap();
    assert!(configured_limit_report.contains("large triangles 4/4 (100.0%)"));
    assert!(configured_limit_report
        .contains("Quality status: PASS (4 findings within configured limits)"));

    write_ascii(
        &mut File::create(&path).unwrap(),
        "open-triangle",
        &[Triangle::new([
            Vec3::ZERO,
            Vec3::new(1.0, 0.0, 0.0),
            Vec3::new(0.0, 1.0, 0.0),
        ])],
    )
    .unwrap();
    let failing_report = analyze_path(&path, Thresholds::default()).unwrap();
    fs::remove_file(path).unwrap();
    assert!(failing_report.contains("Quality status: FAIL ("));
}

#[test]
fn report_caps_detailed_findings_and_reports_omitted_count() {
    let stamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!("stl-report-cap-{stamp}.stl"));
    let triangles = (0..105)
        .map(|index| {
            let x = index as f32 * 2.0;
            Triangle::new([
                Vec3::new(x, 0.0, 0.0),
                Vec3::new(x + 1.0, 0.0, 0.0),
                Vec3::new(x, 1.0, 0.0),
            ])
        })
        .collect::<Vec<_>>();
    write_ascii(&mut File::create(&path).unwrap(), "report-cap", &triangles).unwrap();

    let report = analyze_path(&path, Thresholds::default()).unwrap();

    fs::remove_file(path).unwrap();
    assert!(report.contains("Quality warnings: 315 total; showing at most 100 (215 omitted)"));
    assert_eq!(
        report
            .lines()
            .filter(|line| line.starts_with("- Open edge"))
            .count(),
        100
    );
}

#[test]
fn empty_mesh_is_not_reported_as_watertight() {
    let report = analyze(&Mesh::from_triangles(Vec::new()), Thresholds::default());
    assert!(!report.is_watertight);
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
fn welding_checks_all_candidates_in_a_grid_cell() {
    let zero = Vec3::ZERO;
    let one_x = Vec3::new(1.0, 0.0, 0.0);
    let one_y = Vec3::new(0.0, 1.0, 0.0);
    let one_z = Vec3::new(0.0, 0.0, 1.0);
    let one_xy = Vec3::new(1.0, 1.0, 0.0);
    let tolerance = 3.0_f32.sqrt() * 1.0e-6;
    let first_in_cell = Vec3::new(0.9 * tolerance, 0.9 * tolerance, 0.0);
    let near_origin = Vec3::new(0.1 * tolerance, 0.1 * tolerance, 0.0);
    let one_xyz = Vec3::new(1.0, 1.0, 1.0);
    let mesh = mesh(vec![
        [zero, one_x, one_y],
        [zero, one_xy, one_z],
        [first_in_cell, near_origin, one_xyz],
    ]);

    assert_eq!(mesh.vertices.len(), 7);
    assert_eq!(mesh.triangles[2].indices, [5, 0, 6]);
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
fn default_micro_threshold_is_relative_and_does_not_double_report_slivers() {
    let mesh = mesh(vec![
        [
            Vec3::ZERO,
            Vec3::new(1.0, 0.0, 0.0),
            Vec3::new(0.0, 1.0, 0.0),
        ],
        [
            Vec3::new(0.2, 0.0, 1.0),
            Vec3::new(0.21, 0.0, 1.0),
            Vec3::new(0.2, 1.0e-12, 1.0),
        ],
        [
            Vec3::new(0.0, 0.0, 1.0),
            Vec3::new(0.1, 0.0, 1.0),
            Vec3::new(0.0, 1.0e-5, 1.0),
        ],
    ]);

    let report = analyze(&mesh, Thresholds::default());

    assert_eq!(Thresholds::default().micro_triangle_area_ratio, 1.0e-12);
    assert_eq!(report.micro_triangles.len(), 1);
    assert_eq!(report.micro_triangles[0].triangle, 1);
    assert!(report.micro_triangles[0].relative_area <= 1.0e-12);
    assert_eq!(report.slivers.len(), 1);
    assert_eq!(report.slivers[0].triangle, 2);
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

#[test]
#[ignore = "manual performance check; run with cargo test --release --test mesh_analysis benchmark_vertex_welding_million_triangles -- --ignored --nocapture"]
fn benchmark_vertex_welding_million_triangles() {
    const TRIANGLE_COUNT: u32 = 1_000_000;
    let mut bytes = Vec::with_capacity(84 + TRIANGLE_COUNT as usize * 50);
    stl_analyzer::services::write_benchmark_binary(&mut bytes, TRIANGLE_COUNT).unwrap();
    let parsed = parse_bytes(&bytes).unwrap();
    assert_eq!(parsed.triangles.len(), TRIANGLE_COUNT as usize);

    let mut elapsed = Vec::with_capacity(5);
    for _ in 0..5 {
        let triangles = parsed.triangles.clone();
        let start = std::time::Instant::now();
        let mesh = Mesh::from_triangles(triangles);
        elapsed.push(start.elapsed());
        assert_eq!(mesh.triangles.len(), TRIANGLE_COUNT as usize);
        assert_eq!(mesh.vertices.len(), 501_416);
    }
    elapsed.sort_unstable();
    println!(
        "1M triangle weld median: {:?}; runs: {:?}",
        elapsed[2], elapsed
    );
}
