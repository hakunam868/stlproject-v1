use std::path::Path;
use stl_analyzer::analysis::{analyze, Thresholds};
use stl_analyzer::processing::load_mesh;
use stl_analyzer::stl::{parse_file, StlFormat};

const ROOT: &str = "test_data/stl";

#[test]
fn basic_geometry_files_load_in_their_expected_formats() {
    for (path, expected_format) in [
        ("basic-geometry/cube_ascii.stl", StlFormat::Ascii),
        ("basic-geometry/cube_binary.stl", StlFormat::Binary),
        ("basic-geometry/cuboid_binary.stl", StlFormat::Binary),
        (
            "basic-geometry/low_poly_sphere_binary.stl",
            StlFormat::Binary,
        ),
        ("basic-geometry/sphere_ascii.stl", StlFormat::Ascii),
        ("basic-geometry/cylinder_binary.stl", StlFormat::Binary),
        ("basic-geometry/cone_ascii.stl", StlFormat::Ascii),
        ("basic-geometry/pyramid_binary.stl", StlFormat::Binary),
    ] {
        let parsed = parse_file(&Path::new(ROOT).join(path))
            .unwrap_or_else(|error| panic!("{path}: {error}"));
        assert_eq!(parsed.format, expected_format, "{path}");
        assert!(!parsed.triangles.is_empty(), "{path}");
    }
}

#[test]
fn license_verified_real_world_models_load_with_expected_triangle_counts() {
    for (path, expected_triangles) in [
        ("real-world/ceramic_pot_cc0.stl", 3_592),
        ("real-world/croissant_cc0.stl", 1_990),
        ("real-world/rubber_duck_toy_cc0.stl", 4_288),
        ("real-world/carved_wooden_elephant_cc0.stl", 2_752),
        ("real-world/bananas_cc0.stl", 46_868),
        ("real-world/food_apple_01_cc0.stl", 7_012),
        ("real-world/simple_gear_wheel_cc0.stl", 3_072),
        ("real-world/utah_teapot_solid_cc0.stl", 9_438),
    ] {
        let parsed = parse_file(&Path::new(ROOT).join(path))
            .unwrap_or_else(|error| panic!("{path}: {error}"));
        assert_eq!(parsed.format, StlFormat::Binary, "{path}");
        assert_eq!(parsed.triangles.len(), expected_triangles, "{path}");
    }
}

#[test]
fn complexity_fixtures_load_with_expected_triangle_counts() {
    let low =
        parse_file(&Path::new(ROOT).join("basic-geometry/low_poly_sphere_binary.stl")).unwrap();
    let high =
        parse_file(&Path::new(ROOT).join("mesh-complexity/high_poly_sphere_binary.stl")).unwrap();
    let large = parse_file(&Path::new(ROOT).join("mesh-complexity/large_grid_binary.stl")).unwrap();
    assert!(high.triangles.len() > low.triangles.len() * 100);
    assert_eq!(large.triangles.len(), 20_000);
}

#[test]
fn geometric_boundaries_parse_as_stl() {
    for path in [
        "geometric-edge-cases/thin_feature_ascii.stl",
        "geometric-edge-cases/sliver_triangle_ascii.stl",
        "geometric-edge-cases/sharp_edge_cube_binary.stl",
        "geometric-edge-cases/very_small_triangle_binary.stl",
        "geometric-edge-cases/very_large_triangle_binary.stl",
        "boundary-cases/single_triangle_ascii.stl",
        "boundary-cases/zero_triangle_ascii.stl",
        "boundary-cases/rotated_triangle_binary.stl",
    ] {
        parse_file(&Path::new(ROOT).join(path))
            .unwrap_or_else(|error| panic!("{path} should be structurally valid: {error}"));
    }
    assert_eq!(
        parse_file(&Path::new(ROOT).join("boundary-cases/zero_triangle_ascii.stl"))
            .unwrap()
            .triangles
            .len(),
        0
    );
}

#[test]
fn quality_defects_remain_readable_and_are_reported_by_analysis() {
    for path in [
        "quality-defects/degenerate_triangles_ascii.stl",
        "quality-defects/duplicate_faces_ascii.stl",
        "quality-defects/duplicate_vertices_binary.stl",
        "quality-defects/inconsistent_normals_ascii.stl",
        "quality-defects/non_manifold_edge_ascii.stl",
    ] {
        let (format, mesh) = load_mesh(&Path::new(ROOT).join(path))
            .unwrap_or_else(|error| panic!("{path} should parse despite quality defects: {error}"));
        assert!(!mesh.triangles.is_empty(), "{path}");
        assert!(matches!(format, StlFormat::Ascii | StlFormat::Binary));
    }

    let (_, degenerate) =
        load_mesh(&Path::new(ROOT).join("quality-defects/degenerate_triangles_ascii.stl")).unwrap();
    let degenerate_report = analyze(&degenerate, Thresholds::default());
    assert!(!degenerate_report.micro_triangles.is_empty());

    let (_, non_manifold) =
        load_mesh(&Path::new(ROOT).join("quality-defects/non_manifold_edge_ascii.stl")).unwrap();
    let non_manifold_report = analyze(&non_manifold, Thresholds::default());
    assert_eq!(non_manifold_report.non_manifold_edges.len(), 1);
}

#[test]
fn structurally_invalid_fixtures_are_rejected_by_parser() {
    for path in [
        "invalid/empty_file.stl",
        "invalid/truncated_binary.stl",
        "invalid/malformed_ascii.stl",
        "invalid/incorrect_header.stl",
        "invalid/corrupted_triangle_nan.stl",
    ] {
        assert!(
            parse_file(&Path::new(ROOT).join(path)).is_err(),
            "{path} should be rejected as malformed"
        );
    }
}
