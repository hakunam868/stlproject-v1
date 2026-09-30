use crate::geometry::{Triangle, Vec3};

#[derive(Clone, Debug, Default, PartialEq)]
pub struct Mesh {
    triangles: Vec<Triangle>,
}

impl Mesh {
    pub fn new(triangles: Vec<Triangle>) -> Self {
        Self { triangles }
    }

    pub fn triangles(&self) -> &[Triangle] {
        &self.triangles
    }

    pub fn len(&self) -> usize {
        self.triangles.len()
    }

    pub fn is_empty(&self) -> bool {
        self.triangles.is_empty()
    }

    pub fn bounds(&self) -> Option<(Vec3, Vec3)> {
        let mut vertices = self
            .triangles
            .iter()
            .flat_map(|triangle| triangle.vertices.iter());
        let first = *vertices.next()?;
        Some(vertices.fold((first, first), |(min, max), vertex| {
            (min.min(*vertex), max.max(*vertex))
        }))
    }
}
