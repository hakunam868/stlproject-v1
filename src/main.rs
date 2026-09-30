use std::process::ExitCode;

use stl_analyzer::{analyze, load_stl};

fn main() -> ExitCode {
    let mut args = std::env::args_os().skip(1);
    let Some(path) = args.next() else {
        eprintln!("Usage: stl-analyzer <file.stl>");
        return ExitCode::from(2);
    };
    if args.next().is_some() {
        eprintln!("Usage: stl-analyzer <file.stl>");
        return ExitCode::from(2);
    }

    match load_stl(&path) {
        Ok(mesh) => {
            let report = analyze(&mesh);
            println!("Total faces (triangles): {}", report.triangle_count);
            println!("Triangle size counts (heuristic):");
            println!("  Sliver: {}", report.triangle_sizes.sliver);
            println!("  Medium: {}", report.triangle_sizes.medium);
            println!("  Large: {}", report.triangle_sizes.large);
            println!(
                "  Extremely large: {}",
                report.triangle_sizes.extremely_large
            );
            println!("Surface area: {:.6}", report.surface_area);
            println!("Degenerate triangles: {}", report.degenerate_triangles);
            match report.bounds {
                Some(bounds) => {
                    println!("Bounds min: {}", bounds.min);
                    println!("Bounds max: {}", bounds.max);
                }
                None => println!("Bounds: empty mesh"),
            }
            println!("Top vertices by unique incident-edge count:");
            for vertex in report.high_incidence_vertices {
                println!("  {}: {} edges", vertex.position, vertex.edge_count);
            }
            ExitCode::SUCCESS
        }
        Err(error) => {
            eprintln!("Failed to analyze STL: {error}");
            ExitCode::FAILURE
        }
    }
}
