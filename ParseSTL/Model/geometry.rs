pub use crate::mesh::{IndexedTriangle, Mesh};

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Vec3 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

impl Vec3 {
    pub const ZERO: Self = Self::new(0.0, 0.0, 0.0);

    pub const fn new(x: f32, y: f32, z: f32) -> Self {
        Self { x, y, z }
    }

    pub fn dot(self, other: Self) -> f32 {
        self.x * other.x + self.y * other.y + self.z * other.z
    }

    pub fn cross(self, other: Self) -> Self {
        Self::new(
            self.y * other.z - self.z * other.y,
            self.z * other.x - self.x * other.z,
            self.x * other.y - self.y * other.x,
        )
    }

    pub fn length(self) -> f32 {
        self.dot(self).sqrt()
    }

    pub fn normalized(self) -> Self {
        let length = self.length();
        if length > 0.0 {
            self * (1.0 / length)
        } else {
            Self::ZERO
        }
    }

    pub fn distance(self, other: Self) -> f32 {
        (self - other).length()
    }

    pub fn is_finite(self) -> bool {
        self.x.is_finite() && self.y.is_finite() && self.z.is_finite()
    }
}

impl std::ops::Add for Vec3 {
    type Output = Self;
    fn add(self, other: Self) -> Self {
        Self::new(self.x + other.x, self.y + other.y, self.z + other.z)
    }
}

impl std::ops::Sub for Vec3 {
    type Output = Self;
    fn sub(self, other: Self) -> Self {
        Self::new(self.x - other.x, self.y - other.y, self.z - other.z)
    }
}

impl std::ops::Mul<f32> for Vec3 {
    type Output = Self;
    fn mul(self, scalar: f32) -> Self {
        Self::new(self.x * scalar, self.y * scalar, self.z * scalar)
    }
}

#[derive(Debug, Clone, Copy, PartialEq)]
pub struct Triangle {
    pub normal: Vec3,
    pub vertices: [Vec3; 3],
}

impl Triangle {
    pub fn new(vertices: [Vec3; 3]) -> Self {
        let mut triangle = Self {
            normal: Vec3::ZERO,
            vertices,
        };
        triangle.recalculate_normal();
        triangle
    }

    pub fn recalculate_normal(&mut self) {
        self.normal = (self.vertices[1] - self.vertices[0])
            .cross(self.vertices[2] - self.vertices[0])
            .normalized();
    }

    pub fn area(&self) -> f32 {
        0.5 * (self.vertices[1] - self.vertices[0])
            .cross(self.vertices[2] - self.vertices[0])
            .length()
    }
}
