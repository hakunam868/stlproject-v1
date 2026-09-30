use std::env;
use std::time::Instant;

use stl_analyzer::parse_stl;

fn main() {
    let Some(path) = env::args().nth(1) else {
        eprintln!("Usage: cargo run --release --example benchmark -- <file.stl> [iterations]");
        std::process::exit(2);
    };
    let iterations = env::args()
        .nth(2)
        .and_then(|value| value.parse::<usize>().ok())
        .unwrap_or(100);
    let bytes = std::fs::read(path).expect("failed to read STL file");
    let start = Instant::now();
    let mut triangle_count = 0;
    for _ in 0..iterations {
        triangle_count = parse_stl(&bytes).expect("failed to parse STL file").len();
    }
    println!(
        "Parsed {triangle_count} triangles {iterations} times in {:.3?}",
        start.elapsed()
    );
}
