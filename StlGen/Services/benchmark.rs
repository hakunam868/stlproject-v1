use crate::generator_geometry::{Triangle, Vec3};
use crate::output::write_binary_triangle;
use crate::stl::StlError;
use std::io::Write;

pub fn write_benchmark_binary<W: Write>(writer: &mut W, triangles: u32) -> Result<(), StlError> {
    let mut header = [0_u8; 80];
    header[..18].copy_from_slice(b"benchmark grid STL");
    writer.write_all(&header)?;
    writer.write_all(&triangles.to_le_bytes())?;
    let width = ((triangles as f64 / 2.0).sqrt().ceil() as u32).max(1);
    for index in 0..triangles {
        let cell = index / 2;
        let x = (cell % width) as f32;
        let y = (cell / width) as f32;
        let triangle = if index % 2 == 0 {
            Triangle::new([
                Vec3::new(x, y, 0.0),
                Vec3::new(x + 1.0, y, 0.0),
                Vec3::new(x + 1.0, y + 1.0, 0.0),
            ])
        } else {
            Triangle::new([
                Vec3::new(x, y, 0.0),
                Vec3::new(x + 1.0, y + 1.0, 0.0),
                Vec3::new(x, y + 1.0, 0.0),
            ])
        };
        write_binary_triangle(writer, &triangle)?;
    }
    Ok(())
}
