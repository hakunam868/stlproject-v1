use crate::mesh::Mesh;
use crate::stl::{parse_file, StlError, StlFormat};
use std::path::Path;

pub fn load_mesh(path: &Path) -> Result<(StlFormat, Mesh), StlError> {
    let parsed = parse_file(path)?;
    let mesh = Mesh::from_triangles(parsed.triangles);
    Ok((parsed.format, mesh))
}
