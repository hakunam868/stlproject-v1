use crate::geometry::{Triangle, Vec3};
use crate::stl::{write_binary_triangle, StlError};
use std::f32::consts::PI;
use std::io::Write;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum SampleKind {
    Prism,
    Torus,
    Cylinder,
    Cube,
    Cone,
    Sphere,
}

impl SampleKind {
    pub fn parse(value: &str) -> Option<Self> {
        match value.to_ascii_lowercase().as_str() {
            "prism" => Some(Self::Prism),
            "torus" => Some(Self::Torus),
            "cylinder" => Some(Self::Cylinder),
            "cube" => Some(Self::Cube),
            "cone" => Some(Self::Cone),
            "sphere" => Some(Self::Sphere),
            _ => None,
        }
    }
}

#[derive(Debug, Clone, Copy)]
pub struct ShapeDimensions {
    pub width: f32,
    pub height: f32,
    pub depth: f32,
    pub radius: f32,
    pub major_radius: f32,
    pub minor_radius: f32,
}

impl ShapeDimensions {
    pub fn defaults(kind: SampleKind) -> Self {
        match kind {
            SampleKind::Prism => Self {
                width: 2.0,
                height: 1.0,
                depth: 1.0,
                ..Self::unit()
            },
            SampleKind::Cube => Self {
                width: 1.0,
                height: 1.0,
                depth: 1.0,
                ..Self::unit()
            },
            SampleKind::Torus => Self {
                major_radius: 1.0,
                minor_radius: 0.35,
                ..Self::unit()
            },
            SampleKind::Cylinder | SampleKind::Cone => Self {
                radius: 1.0,
                height: 2.0,
                ..Self::unit()
            },
            SampleKind::Sphere => Self {
                radius: 1.0,
                ..Self::unit()
            },
        }
    }

    const fn unit() -> Self {
        Self {
            width: 1.0,
            height: 1.0,
            depth: 1.0,
            radius: 1.0,
            major_radius: 1.0,
            minor_radius: 0.35,
        }
    }

    pub fn validate(self, kind: SampleKind) -> Result<(), String> {
        let positive = |value: f32, name: &str| {
            (value.is_finite() && value > 0.0)
                .then_some(())
                .ok_or_else(|| format!("{name} must be a positive finite number"))
        };
        match kind {
            SampleKind::Prism => {
                positive(self.width, "width")?;
                positive(self.height, "height")?;
                positive(self.depth, "depth")?;
            }
            SampleKind::Cube => positive(self.width, "size")?,
            SampleKind::Cylinder | SampleKind::Cone => {
                positive(self.radius, "radius")?;
                positive(self.height, "height")?;
            }
            SampleKind::Sphere => positive(self.radius, "radius")?,
            SampleKind::Torus => {
                positive(self.major_radius, "major radius")?;
                positive(self.minor_radius, "minor radius")?;
                if self.major_radius <= self.minor_radius {
                    return Err("major radius must be greater than minor radius".into());
                }
            }
        }
        Ok(())
    }
}

pub fn generate(kind: SampleKind, density: usize) -> Vec<Triangle> {
    generate_with_dimensions(kind, density, ShapeDimensions::defaults(kind))
        .expect("built-in defaults are valid")
}

pub fn generate_with_dimensions(
    kind: SampleKind,
    density: usize,
    dimensions: ShapeDimensions,
) -> Result<Vec<Triangle>, String> {
    dimensions.validate(kind)?;
    match kind {
        SampleKind::Prism => Ok(cuboid(
            dimensions.width,
            dimensions.height,
            dimensions.depth,
            density,
        )),
        SampleKind::Cube => Ok(cuboid(
            dimensions.width,
            dimensions.width,
            dimensions.width,
            density,
        )),
        SampleKind::Torus => Ok(torus(
            density,
            dimensions.major_radius,
            dimensions.minor_radius,
        )),
        SampleKind::Cylinder => Ok(cylinder(density, dimensions.radius, dimensions.height)),
        SampleKind::Cone => Ok(cone(density, dimensions.radius, dimensions.height)),
        SampleKind::Sphere => Ok(sphere(density, dimensions.radius)),
    }
}

fn add_quad(output: &mut Vec<Triangle>, a: Vec3, b: Vec3, c: Vec3, d: Vec3) {
    output.push(Triangle::new([a, b, c]));
    output.push(Triangle::new([a, c, d]));
}

fn cuboid(width: f32, height: f32, depth: f32, density: usize) -> Vec<Triangle> {
    let n = density.max(1);
    let mut triangles = Vec::with_capacity(12 * n * n);
    let x = width / 2.0;
    let y = height / 2.0;
    let z = depth / 2.0;
    let faces = [
        (
            Vec3::new(-x, -y, z),
            Vec3::new(x, -y, z),
            Vec3::new(x, y, z),
            Vec3::new(-x, y, z),
        ),
        (
            Vec3::new(x, -y, -z),
            Vec3::new(-x, -y, -z),
            Vec3::new(-x, y, -z),
            Vec3::new(x, y, -z),
        ),
        (
            Vec3::new(-x, -y, -z),
            Vec3::new(-x, -y, z),
            Vec3::new(-x, y, z),
            Vec3::new(-x, y, -z),
        ),
        (
            Vec3::new(x, -y, z),
            Vec3::new(x, -y, -z),
            Vec3::new(x, y, -z),
            Vec3::new(x, y, z),
        ),
        (
            Vec3::new(-x, y, z),
            Vec3::new(x, y, z),
            Vec3::new(x, y, -z),
            Vec3::new(-x, y, -z),
        ),
        (
            Vec3::new(-x, -y, -z),
            Vec3::new(x, -y, -z),
            Vec3::new(x, -y, z),
            Vec3::new(-x, -y, z),
        ),
    ];
    for (a, b, c, d) in faces {
        for row in 0..n {
            for col in 0..n {
                let u0 = col as f32 / n as f32;
                let u1 = (col + 1) as f32 / n as f32;
                let v0 = row as f32 / n as f32;
                let v1 = (row + 1) as f32 / n as f32;
                let point = |u, v| {
                    a * ((1.0 - u) * (1.0 - v))
                        + b * (u * (1.0 - v))
                        + c * (u * v)
                        + d * ((1.0 - u) * v)
                };
                add_quad(
                    &mut triangles,
                    point(u0, v0),
                    point(u1, v0),
                    point(u1, v1),
                    point(u0, v1),
                );
            }
        }
    }
    triangles
}

fn cylinder(density: usize, radius: f32, height: f32) -> Vec<Triangle> {
    let n = density.max(3);
    let mut triangles = Vec::with_capacity(4 * n);
    let top = height / 2.0;
    let bottom = -height / 2.0;
    for i in 0..n {
        let angle = |index: usize| 2.0 * PI * index as f32 / n as f32;
        let p = |index, y| Vec3::new(radius * angle(index).cos(), y, radius * angle(index).sin());
        let a = p(i, bottom);
        let b = p((i + 1) % n, bottom);
        let c = p((i + 1) % n, top);
        let d = p(i, top);
        add_quad(&mut triangles, a, b, c, d);
        triangles.push(Triangle::new([Vec3::new(0.0, top, 0.0), d, c]));
        triangles.push(Triangle::new([Vec3::new(0.0, bottom, 0.0), b, a]));
    }
    triangles
}

fn cone(density: usize, radius: f32, height: f32) -> Vec<Triangle> {
    let n = density.max(3);
    let apex = Vec3::new(0.0, height / 2.0, 0.0);
    let center = Vec3::new(0.0, -height / 2.0, 0.0);
    let mut triangles = Vec::with_capacity(2 * n);
    for i in 0..n {
        let point = |index: usize| {
            let a = 2.0 * PI * index as f32 / n as f32;
            Vec3::new(radius * a.cos(), -height / 2.0, radius * a.sin())
        };
        let a = point(i);
        let b = point((i + 1) % n);
        triangles.push(Triangle::new([apex, a, b]));
        triangles.push(Triangle::new([center, b, a]));
    }
    triangles
}

fn torus(density: usize, major_radius: f32, minor_radius: f32) -> Vec<Triangle> {
    let major = density.max(3);
    let minor = (density / 2).max(3);
    let mut triangles = Vec::with_capacity(2 * major * minor);
    let point = |u: usize, v: usize| {
        let a = 2.0 * PI * u as f32 / major as f32;
        let b = 2.0 * PI * v as f32 / minor as f32;
        let r = major_radius + minor_radius * b.cos();
        Vec3::new(r * a.cos(), minor_radius * b.sin(), r * a.sin())
    };
    for u in 0..major {
        for v in 0..minor {
            add_quad(
                &mut triangles,
                point(u, v),
                point((u + 1) % major, v),
                point((u + 1) % major, (v + 1) % minor),
                point(u, (v + 1) % minor),
            );
        }
    }
    triangles
}

fn sphere(density: usize, radius: f32) -> Vec<Triangle> {
    let latitudes = density.max(2);
    let longitudes = (density * 2).max(3);
    let mut triangles = Vec::with_capacity(2 * longitudes * latitudes);
    let top = Vec3::new(0.0, radius, 0.0);
    let bottom = Vec3::new(0.0, -radius, 0.0);
    let ring = |latitude: usize, longitude: usize| {
        let theta = PI * latitude as f32 / latitudes as f32;
        let phi = 2.0 * PI * longitude as f32 / longitudes as f32;
        Vec3::new(
            radius * theta.sin() * phi.cos(),
            radius * theta.cos(),
            radius * theta.sin() * phi.sin(),
        )
    };
    for longitude in 0..longitudes {
        let next = (longitude + 1) % longitudes;
        triangles.push(Triangle::new([top, ring(1, next), ring(1, longitude)]));
    }
    for latitude in 1..latitudes - 1 {
        for longitude in 0..longitudes {
            let next = (longitude + 1) % longitudes;
            add_quad(
                &mut triangles,
                ring(latitude, longitude),
                ring(latitude, next),
                ring(latitude + 1, next),
                ring(latitude + 1, longitude),
            );
        }
    }
    for longitude in 0..longitudes {
        let next = (longitude + 1) % longitudes;
        triangles.push(Triangle::new([
            bottom,
            ring(latitudes, longitude),
            ring(latitudes, next),
        ]));
    }
    triangles
}

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

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn all_samples_are_generated_and_density_changes_cube_size() {
        for kind in [
            SampleKind::Prism,
            SampleKind::Torus,
            SampleKind::Cylinder,
            SampleKind::Cube,
            SampleKind::Cone,
            SampleKind::Sphere,
        ] {
            assert!(!generate(kind, 4).is_empty());
        }
        assert!(generate(SampleKind::Cube, 3).len() > generate(SampleKind::Cube, 1).len());
    }

    #[test]
    fn custom_cube_dimensions_change_its_bounds() {
        let triangles = generate_with_dimensions(
            SampleKind::Cube,
            1,
            ShapeDimensions {
                width: 6.0,
                ..ShapeDimensions::defaults(SampleKind::Cube)
            },
        )
        .unwrap();
        let min_x = triangles
            .iter()
            .flat_map(|triangle| triangle.vertices)
            .map(|vertex| vertex.x)
            .fold(f32::INFINITY, f32::min);
        let max_x = triangles
            .iter()
            .flat_map(|triangle| triangle.vertices)
            .map(|vertex| vertex.x)
            .fold(f32::NEG_INFINITY, f32::max);
        assert_eq!((min_x, max_x), (-3.0, 3.0));
    }
}
