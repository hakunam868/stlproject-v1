use std::fs;
use std::io;
use std::path::{Path, PathBuf};

pub fn list_stl_files(folder: &Path) -> io::Result<Vec<PathBuf>> {
    let mut files = Vec::new();
    if !folder.exists() {
        return Ok(files);
    }
    for entry in fs::read_dir(folder)? {
        let path = entry?.path();
        if path.is_file()
            && path
                .extension()
                .is_some_and(|extension| extension.eq_ignore_ascii_case("stl"))
        {
            files.push(path);
        }
    }
    files.sort();
    Ok(files)
}
