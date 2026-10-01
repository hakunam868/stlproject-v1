use std::fmt;
use std::fs;
use std::path::Path;

use crate::geometry::{Triangle, Vec3};
use crate::mesh::Mesh;

#[derive(Debug)]
pub enum StlError {
    Io(std::io::Error),
    InvalidFormat(String),
}

impl fmt::Display for StlError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            Self::Io(error) => write!(f, "{error}"),
            Self::InvalidFormat(message) => write!(f, "{message}"),
        }
    }
}

impl std::error::Error for StlError {}

impl From<std::io::Error> for StlError {
    fn from(error: std::io::Error) -> Self {
        Self::Io(error)
    }
}

pub fn load_stl(path: impl AsRef<Path>) -> Result<Mesh, StlError> {
    parse_stl(&fs::read(path)?)
}

pub fn parse_stl(bytes: &[u8]) -> Result<Mesh, StlError> {
    if bytes.len() >= 84 {
        let triangle_count = u32::from_le_bytes(bytes[80..84].try_into().unwrap()) as usize;
        if 84usize.checked_add(triangle_count.saturating_mul(50)) == Some(bytes.len()) {
            return parse_binary(bytes, triangle_count);
        }
    }
    parse_ascii(bytes)
}

fn parse_binary(bytes: &[u8], triangle_count: usize) -> Result<Mesh, StlError> {
    let mut triangles = Vec::with_capacity(triangle_count);
    for index in 0..triangle_count {
        let start = 84 + index * 50 + 12;
        let mut vertices = [Vec3::default(); 3];
        for (vertex_index, vertex) in vertices.iter_mut().enumerate() {
            let offset = start + vertex_index * 12;
            let coordinates = [
                read_f32(bytes, offset),
                read_f32(bytes, offset + 4),
                read_f32(bytes, offset + 8),
            ];
            if !coordinates.iter().all(|value| value.is_finite()) {
                return Err(StlError::InvalidFormat(format!(
                    "non-finite coordinate in binary triangle {}",
                    index + 1
                )));
            }
            *vertex = Vec3::new(
                coordinates[0] as f64,
                coordinates[1] as f64,
                coordinates[2] as f64,
            );
        }
        triangles.push(Triangle::new(vertices[0], vertices[1], vertices[2]));
    }
    Ok(Mesh::new(triangles))
}

fn read_f32(bytes: &[u8], offset: usize) -> f32 {
    f32::from_le_bytes(bytes[offset..offset + 4].try_into().unwrap())
}

fn parse_ascii(bytes: &[u8]) -> Result<Mesh, StlError> {
    let text = std::str::from_utf8(bytes).map_err(|_| {
        StlError::InvalidFormat("input is neither valid binary nor UTF-8 ASCII STL".into())
    })?;
    let mut vertices = Vec::new();
    for (line_number, line) in text.lines().enumerate() {
        let mut fields = line.split_whitespace();
        if fields.next() != Some("vertex") {
            continue;
        }
        let coordinates: Result<Vec<f64>, _> = fields.take(3).map(str::parse).collect();
        let coordinates = coordinates.map_err(|_| {
            StlError::InvalidFormat(format!("invalid vertex on line {}", line_number + 1))
        })?;
        if coordinates.len() != 3 || !coordinates.iter().all(|value| value.is_finite()) {
            return Err(StlError::InvalidFormat(format!(
                "invalid vertex on line {}",
                line_number + 1
            )));
        }
        vertices.push(Vec3::new(coordinates[0], coordinates[1], coordinates[2]));
    }
    if vertices.len() % 3 != 0 {
        return Err(StlError::InvalidFormat(
            "ASCII STL must contain vertices in groups of three".into(),
        ));
    }
    let triangles = vertices
        .chunks_exact(3)
        .map(|vertices| Triangle::new(vertices[0], vertices[1], vertices[2]))
        .collect();
    Ok(Mesh::new(triangles))
}
