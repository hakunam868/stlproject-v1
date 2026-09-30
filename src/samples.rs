use std::fs;
use std::io::{self, BufWriter, Write};
use std::path::Path;

pub const ASCII_TRIANGLE_STL: &str = "solid sample\n  facet normal 0 0 1\n    outer loop\n      vertex 0 0 0\n      vertex 1 0 0\n      vertex 0 1 0\n    endloop\n  endfacet\nendsolid sample\n";
pub const DEFAULT_SAMPLE_TRIANGLES: u32 = 1_000_000;
const GRID_COLUMNS: u32 = 500;
const SIZE_CLASSES: [(f32, f32, f32); 4] = [
    (1.0, 0.001, 0.0),
    (1.0, 1.0, 1_000.0),
    (100.0, 100.0, 3_000.0),
    (10_000.0, 10_000.0, 100_000.0),
];

pub fn write_sample_stl(path: impl AsRef<Path>) -> io::Result<()> {
    fs::write(path, ASCII_TRIANGLE_STL)
}

pub fn write_varied_binary_stl(path: impl AsRef<Path>, triangle_count: u32) -> io::Result<()> {
    let file = fs::File::create(path)?;
    let mut output = BufWriter::new(file);
    let mut header = [0u8; 80];
    let description = b"STL analyzer varied-size sample";
    header[..description.len()].copy_from_slice(description);
    output.write_all(&header)?;
    output.write_all(&triangle_count.to_le_bytes())?;

    for triangle_index in 0..triangle_count {
        let class_index =
            (triangle_index as u64 * SIZE_CLASSES.len() as u64 / triangle_count as u64) as usize;
        let (width, height, group_offset) = SIZE_CLASSES[class_index];
        let column = triangle_index % GRID_COLUMNS;
        let row = triangle_index / GRID_COLUMNS;
        let x = group_offset + column as f32 * width * 1.5;
        let y = row as f32 * height * 1.5;

        write_vec3(&mut output, [0.0, 0.0, 1.0])?;
        write_vec3(&mut output, [x, y, 0.0])?;
        write_vec3(&mut output, [x + width, y, 0.0])?;
        write_vec3(&mut output, [x, y + height, 0.0])?;
        output.write_all(&0u16.to_le_bytes())?;
    }

    output.flush()
}

fn write_vec3(output: &mut impl Write, vector: [f32; 3]) -> io::Result<()> {
    for component in vector {
        output.write_all(&component.to_le_bytes())?;
    }
    Ok(())
}
