use stl_analyzer::{analyze, Mesh, Triangle, Vec3};

#[test]
fn reports_area_bounds_and_degenerate_triangles() {
    let mesh = Mesh::new(vec![
        Triangle::new(
            Vec3::new(0.0, 0.0, 0.0),
            Vec3::new(2.0, 0.0, 0.0),
            Vec3::new(0.0, 2.0, 0.0),
        ),
        Triangle::new(
            Vec3::new(1.0, 1.0, 1.0),
            Vec3::new(2.0, 2.0, 2.0),
            Vec3::new(3.0, 3.0, 3.0),
        ),
    ]);

    let report = analyze(&mesh);
    assert_eq!(report.triangle_count, 2);
    assert!((report.surface_area - 2.0).abs() < 1e-12);
    assert_eq!(report.degenerate_triangles, 1);
    let bounds = report.bounds.unwrap();
    assert_eq!(bounds.min, Vec3::new(0.0, 0.0, 0.0));
    assert_eq!(bounds.max, Vec3::new(3.0, 3.0, 3.0));
}

#[test]
fn classifies_triangle_sizes_and_counts_unique_incident_edges() {
    let origin = Vec3::new(0.0, 0.0, 0.0);
    let mesh = Mesh::new(vec![
        Triangle::new(
            origin,
            Vec3::new(100.0, 0.0, 0.0),
            Vec3::new(100.0, 0.001, 0.0),
        ),
        Triangle::new(origin, Vec3::new(1.0, 0.0, 0.0), Vec3::new(0.0, 1.0, 0.0)),
        Triangle::new(
            origin,
            Vec3::new(100.0, 0.0, 0.0),
            Vec3::new(0.0, 100.0, 0.0),
        ),
        Triangle::new(
            origin,
            Vec3::new(10_000.0, 0.0, 0.0),
            Vec3::new(0.0, 10_000.0, 0.0),
        ),
        Triangle::new(origin, Vec3::new(0.0, 2.0, 0.0), Vec3::new(-2.0, 0.0, 0.0)),
        Triangle::new(origin, Vec3::new(-2.0, 0.0, 0.0), Vec3::new(0.0, -2.0, 0.0)),
    ]);

    let report = analyze(&mesh);
    assert_eq!(report.triangle_sizes.sliver, 1);
    assert_eq!(report.triangle_sizes.medium, 3);
    assert_eq!(report.triangle_sizes.large, 1);
    assert_eq!(report.triangle_sizes.extremely_large, 1);
    assert_eq!(report.high_incidence_vertices[0].position, origin);
    assert_eq!(report.high_incidence_vertices[0].edge_count, 10);
}
