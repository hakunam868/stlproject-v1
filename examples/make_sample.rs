use std::env;
use std::process::ExitCode;

use stl_analyzer::samples::{write_varied_binary_stl, DEFAULT_SAMPLE_TRIANGLES};

fn main() -> ExitCode {
    let path = env::args().nth(1).unwrap_or_else(|| "sample.stl".into());
    let triangle_count = match env::args().nth(2) {
        Some(value) => match value.parse::<u32>() {
            Ok(count) => count,
            Err(error) => {
                eprintln!("Invalid triangle count: {error}");
                return ExitCode::from(2);
            }
        },
        None => DEFAULT_SAMPLE_TRIANGLES,
    };

    match write_varied_binary_stl(&path, triangle_count) {
        Ok(()) => {
            println!("Wrote {path} with {triangle_count} triangles");
            ExitCode::SUCCESS
        }
        Err(error) => {
            eprintln!("Could not write {path}: {error}");
            ExitCode::FAILURE
        }
    }
}
