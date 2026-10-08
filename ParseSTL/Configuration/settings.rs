use crate::analysis::Thresholds;
use serde::Deserialize;
use std::env;
use std::fs;
use std::io;
use std::path::{Path, PathBuf};

pub struct Config {
    pub stl_folder: PathBuf,
    pub thresholds: Thresholds,
}

#[derive(Deserialize)]
struct RawConfig {
    stl_folder: Option<PathBuf>,
    sliver_aspect_ratio: Option<f32>,
    micro_triangle_area_ratio: Option<f32>,
    large_triangle_area_ratio: Option<f32>,
    max_incident_edges: Option<usize>,
}

impl Config {
    pub fn load_default() -> io::Result<Self> {
        let executable = env::current_exe()?;
        let working_directory = env::current_dir()?;
        let mut candidates = Vec::new();
        candidates.push(working_directory.join("config.json"));

        let application_folder = if executable
            .file_stem()
            .is_some_and(|name| name.to_string_lossy().contains("stl-generator"))
        {
            "StlGen"
        } else {
            "ParseSTL"
        };
        candidates.push(
            working_directory
                .join(application_folder)
                .join("Config.json"),
        );
        if let Some(directory) = executable.parent() {
            candidates.push(directory.join("config.json"));
            candidates.push(directory.join("Config.json"));
        }
        if let Some(directory) = executable.parent() {
            candidates.extend(
                directory
                    .ancestors()
                    .map(|root| root.join(application_folder).join("Config.json")),
            );
        }

        let path = candidates
            .into_iter()
            .find(|candidate| candidate.is_file())
            .unwrap_or_else(|| {
                working_directory
                    .join(application_folder)
                    .join("Config.json")
            });
        Self::load_from(&path)
    }

    pub fn load_from(path: &Path) -> io::Result<Self> {
        let text = fs::read_to_string(path)?;
        let raw: RawConfig = serde_json::from_str(&text)
            .map_err(|error| invalid_config(&format!("invalid JSON: {error}")))?;
        let folder = raw
            .stl_folder
            .ok_or_else(|| invalid_config("missing stl_folder"))?;
        let base = path.parent().unwrap_or_else(|| Path::new("."));
        let thresholds = Thresholds {
            sliver_aspect_ratio: raw.sliver_aspect_ratio.unwrap_or(20.0),
            micro_triangle_area_ratio: raw.micro_triangle_area_ratio.unwrap_or(1.0e-8),
            large_triangle_area_ratio: raw.large_triangle_area_ratio.unwrap_or(0.25),
            max_incident_edges: raw.max_incident_edges.unwrap_or(12),
        };
        if !thresholds.sliver_aspect_ratio.is_finite()
            || thresholds.sliver_aspect_ratio <= 0.0
            || !thresholds.micro_triangle_area_ratio.is_finite()
            || thresholds.micro_triangle_area_ratio <= 0.0
            || !thresholds.large_triangle_area_ratio.is_finite()
            || thresholds.large_triangle_area_ratio <= 0.0
        {
            return Err(invalid_config(
                "area and aspect-ratio thresholds must be positive and finite",
            ));
        }
        Ok(Self {
            stl_folder: base.join(folder),
            thresholds,
        })
    }
}

fn invalid_config(message: &str) -> io::Error {
    io::Error::new(
        io::ErrorKind::InvalidData,
        format!("invalid config.json: {message}"),
    )
}
