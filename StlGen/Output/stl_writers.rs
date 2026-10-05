use crate::geometry::Triangle;
use crate::stl::StlError;
use std::io::Write;

pub fn write_ascii<W: Write>(
    writer: &mut W,
    name: &str,
    triangles: &[Triangle],
) -> Result<(), StlError> {
    writeln!(writer, "solid {name}")?;
    for triangle in triangles {
        writeln!(
            writer,
            "  facet normal {} {} {}",
            triangle.normal.x, triangle.normal.y, triangle.normal.z
        )?;
        writeln!(writer, "    outer loop")?;
        for vertex in triangle.vertices {
            writeln!(
                writer,
                "      vertex {} {} {}",
                vertex.x, vertex.y, vertex.z
            )?;
        }
        writeln!(writer, "    endloop\n  endfacet")?;
    }
    writeln!(writer, "endsolid {name}")?;
    Ok(())
}

pub fn write_binary<W: Write>(
    writer: &mut W,
    name: &str,
    triangles: &[Triangle],
) -> Result<(), StlError> {
    let mut header = [0_u8; 80];
    let label = name.as_bytes();
    header[..label.len().min(80)].copy_from_slice(&label[..label.len().min(80)]);
    writer.write_all(&header)?;
    writer.write_all(&(triangles.len() as u32).to_le_bytes())?;
    for triangle in triangles {
        write_binary_triangle(writer, triangle)?;
    }
    Ok(())
}

pub fn write_binary_triangle<W: Write>(
    writer: &mut W,
    triangle: &Triangle,
) -> Result<(), StlError> {
    for vector in [
        triangle.normal,
        triangle.vertices[0],
        triangle.vertices[1],
        triangle.vertices[2],
    ] {
        for value in [vector.x, vector.y, vector.z] {
            writer.write_all(&value.to_le_bytes())?;
        }
    }
    writer.write_all(&0_u16.to_le_bytes())?;
    Ok(())
}
