use stl_analyzer::{parse_stl, samples::ASCII_TRIANGLE_STL};

#[test]
fn loads_ascii_stl() {
    let mesh = parse_stl(ASCII_TRIANGLE_STL.as_bytes()).unwrap();
    assert_eq!(mesh.len(), 1);
    assert_eq!(mesh.triangles()[0].area(), 0.5);
}

#[test]
fn loads_binary_stl() {
    let mut bytes = vec![0; 84 + 50];
    bytes[80..84].copy_from_slice(&1u32.to_le_bytes());
    let values = [
        0.0f32, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0,
    ];
    for (index, value) in values.iter().enumerate() {
        let offset = 84 + index * 4;
        bytes[offset..offset + 4].copy_from_slice(&value.to_le_bytes());
    }
    let mesh = parse_stl(&bytes).unwrap();
    assert_eq!(mesh.len(), 1);
    assert_eq!(mesh.triangles()[0].area(), 0.5);
}

#[test]
fn rejects_incomplete_ascii_triangle() {
    let result = parse_stl(b"solid bad\nvertex 0 0 0\nendsolid bad");
    assert!(result.is_err());
}
