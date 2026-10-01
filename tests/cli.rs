use std::fs;
use std::process::Command;
use std::time::{SystemTime, UNIX_EPOCH};

use stl_analyzer::samples::ASCII_TRIANGLE_STL;

#[test]
fn cli_reports_mesh_statistics() {
    let stamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!("stl-analyzer-{stamp}.stl"));
    fs::write(&path, ASCII_TRIANGLE_STL).unwrap();

    let output = Command::new(env!("CARGO_BIN_EXE_stl-analyzer"))
        .arg(&path)
        .output()
        .unwrap();
    fs::remove_file(path).unwrap();

    assert!(output.status.success());
    let stdout = String::from_utf8(output.stdout).unwrap();
    assert!(stdout.contains("Total faces (triangles): 1"));
    assert!(stdout.contains("Unique vertices (exact coordinates): 3"));
    assert!(stdout.contains("Triangle size counts (relative to bounding-box diagonal):"));
    assert!(stdout.contains("Top vertices by unique incident-edge count:"));
    assert!(stdout.contains("Surface area: 0.500000"));
}

#[test]
fn cli_requires_a_path() {
    let output = Command::new(env!("CARGO_BIN_EXE_stl-analyzer"))
        .output()
        .unwrap();
    assert_eq!(output.status.code(), Some(2));
    assert!(String::from_utf8_lossy(&output.stderr).contains("Usage:"));
}
