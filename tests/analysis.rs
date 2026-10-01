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
fn classifies_sizes_relative_to_bounds_and_slivers_by_shape() {
    let mesh = Mesh::new(vec![
        Triangle::new(
            Vec3::new(0.0, 0.0, 0.0),
            Vec3::new(1000.0, 0.0, 0.0),
            Vec3::new(0.0, 1000.0, 0.0),
        ),
        Triangle::new(
            Vec3::new(10.0, 10.0, 0.0),
            Vec3::new(10.001, 10.0, 0.0),
            Vec3::new(10.0, 10.001, 0.0),
        ),
        Triangle::new(
            Vec3::new(100.0, 100.0, 0.0),
            Vec3::new(150.0, 100.0, 0.0),
            Vec3::new(100.0, 150.0, 0.0),
        ),
        Triangle::new(
            Vec3::new(300.0, 300.0, 0.0),
            Vec3::new(500.0, 300.0, 0.0),
            Vec3::new(300.0, 500.0, 0.0),
        ),
        Triangle::new(
            Vec3::new(600.0, 600.0, 0.0),
            Vec3::new(610.0, 600.0, 0.0),
            Vec3::new(610.0, 600.001, 0.0),
        ),
    ]);

    let report = analyze(&mesh);
    assert_eq!(report.triangle_sizes.micro, 1);
    assert_eq!(report.triangle_sizes.medium, 2);
    assert_eq!(report.triangle_sizes.large, 1);
    assert_eq!(report.triangle_sizes.extremely_large, 1);
    assert_eq!(report.triangle_shapes.sliver, 1);
}

#[test]
fn reports_exact_unique_vertices_and_ignores_self_edges() {
    let a = Vec3::new(0.0, 0.0, 0.0);
    let b = Vec3::new(1.0, 0.0, 0.0);
    let c = Vec3::new(0.0, 1.0, 0.0);
    let mesh = Mesh::new(vec![
        Triangle::new(a, b, c),
        Triangle::new(a, b, c),
        Triangle::new(a, a, b),
    ]);

    let report = analyze(&mesh);
    assert_eq!(report.unique_vertex_count, 3);
    assert_eq!(mesh.vertex_count(), 3);
    assert!(report
        .high_incidence_vertices
        .iter()
        .any(|vertex| vertex.position == a && vertex.edge_count == 2));
    assert!(report
        .high_incidence_vertices
        .iter()
        .all(|vertex| vertex.edge_count == 2));
}

#[test]
fn tiny_valid_triangle_is_not_marked_degenerate() {
    let mesh = Mesh::new(vec![
        Triangle::new(
            Vec3::new(0.0, 0.0, 0.0),
            Vec3::new(1e-6, 0.0, 0.0),
            Vec3::new(0.0, 1e-6, 0.0),
        ),
        Triangle::new(
            Vec3::new(10.0, 0.0, 0.0),
            Vec3::new(0.0, 10.0, 0.0),
            Vec3::new(10.0, 10.0, 0.0),
        ),
    ]);

    let report = analyze(&mesh);
    assert_eq!(report.degenerate_triangles, 0);
    assert_eq!(report.triangle_sizes.micro, 1);
}
