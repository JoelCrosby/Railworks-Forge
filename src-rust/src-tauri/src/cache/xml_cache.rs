use crate::platform::app_cache_dir;
use anyhow::Result;
use std::path::{Path, PathBuf};

const CACHE_DIR: &str = "xml-cache";

/// Returns the disk cache path for a given .bin source file.
/// Uses MD5 of the source path string to create a flat cache structure.
pub fn cache_path_for(bin_path: &Path) -> Result<PathBuf> {
    let dir = app_cache_dir()?.join(CACHE_DIR);
    std::fs::create_dir_all(&dir)?;

    let key = format!("{:x}", md5::compute(bin_path.to_string_lossy().as_bytes()));
    let xml_name = bin_path
        .file_name()
        .and_then(|n| n.to_str())
        .unwrap_or("file")
        .to_string()
        + ".xml";

    Ok(dir.join(format!("{key}-{xml_name}")))
}

/// Returns true if the cached XML file exists and is newer than the source .bin file.
pub async fn is_valid(xml_path: &Path, bin_path: &Path) -> Result<bool> {
    if !xml_path.exists() {
        return Ok(false);
    }

    let xml_meta = tokio::fs::metadata(xml_path).await?;
    let bin_meta = tokio::fs::metadata(bin_path).await?;

    let xml_modified = xml_meta.modified()?;
    let bin_modified = bin_meta.modified()?;

    Ok(xml_modified >= bin_modified)
}

/// Removes all cached XML files — call when the user explicitly requests a refresh.
pub fn clear() -> Result<()> {
    let dir = app_cache_dir()?.join(CACHE_DIR);
    if dir.exists() {
        std::fs::remove_dir_all(&dir)?;
    }
    Ok(())
}
