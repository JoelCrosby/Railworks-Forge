use anyhow::{Context, Result};
use std::{io::Read, path::Path};
use zip::ZipArchive;

/// Checks whether an entry exists inside a .ap (ZIP) archive.
pub fn entry_exists(archive_path: &Path, entry_name: &str) -> bool {
    let Ok(file) = std::fs::File::open(archive_path) else {
        return false;
    };
    let Ok(mut archive) = ZipArchive::new(file) else {
        return false;
    };
    let normalised = normalise_entry_name(entry_name);
    (0..archive.len()).any(|i| {
        archive
            .by_index(i)
            .map(|e| normalise_entry_name(e.name()) == normalised)
            .unwrap_or(false)
    })
}

/// Checks whether any entry inside a .ap archive starts with the given prefix.
pub fn entry_with_prefix_exists(archive_path: &Path, prefix: &str) -> bool {
    let Ok(file) = std::fs::File::open(archive_path) else {
        return false;
    };
    let Ok(mut archive) = ZipArchive::new(file) else {
        return false;
    };
    let normalised_prefix = normalise_entry_name(prefix);
    (0..archive.len()).any(|i| {
        archive
            .by_index(i)
            .map(|e| normalise_entry_name(e.name()).starts_with(&normalised_prefix))
            .unwrap_or(false)
    })
}

/// Reads the raw bytes of a named entry from a .ap archive.
pub fn read_entry(archive_path: &Path, entry_name: &str) -> Result<Vec<u8>> {
    let file = std::fs::File::open(archive_path)
        .with_context(|| format!("opening archive: {}", archive_path.display()))?;
    let mut archive = ZipArchive::new(file)
        .with_context(|| format!("reading archive: {}", archive_path.display()))?;

    let normalised = normalise_entry_name(entry_name);
    let index = (0..archive.len())
        .find(|&i| {
            archive
                .by_index(i)
                .map(|e| normalise_entry_name(e.name()) == normalised)
                .unwrap_or(false)
        })
        .with_context(|| {
            format!(
                "entry '{entry_name}' not found in {}",
                archive_path.display()
            )
        })?;

    let mut entry = archive.by_index(index)?;
    let mut bytes = Vec::with_capacity(entry.size() as usize);
    entry.read_to_end(&mut bytes)?;
    Ok(bytes)
}

/// Reads an entry as a UTF-8 string.
pub fn read_entry_as_string(archive_path: &Path, entry_name: &str) -> Result<String> {
    let bytes = read_entry(archive_path, entry_name)?;
    String::from_utf8(bytes).context("archive entry is not valid UTF-8")
}

/// Lists all entry names within a .ap archive.
pub fn list_entries(archive_path: &Path) -> Result<Vec<String>> {
    let file = std::fs::File::open(archive_path)
        .with_context(|| format!("opening archive: {}", archive_path.display()))?;
    let mut archive = ZipArchive::new(file)
        .with_context(|| format!("reading archive: {}", archive_path.display()))?;

    let names = (0..archive.len())
        .filter_map(|i| archive.by_index(i).ok().map(|e| e.name().to_string()))
        .collect();
    Ok(names)
}

/// Returns all entries in a .ap archive whose names start with the given prefix.
pub fn entries_with_prefix(archive_path: &Path, prefix: &str) -> Result<Vec<String>> {
    let normalised_prefix = normalise_entry_name(prefix);
    Ok(list_entries(archive_path)?
        .into_iter()
        .filter(|name| normalise_entry_name(name).starts_with(&normalised_prefix))
        .collect())
}

fn normalise_entry_name(name: &str) -> String {
    name.replace('\\', "/").to_lowercase()
}
