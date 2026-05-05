use crate::{cache::xml_cache, platform::to_windows_path};
use anyhow::{Context, Result};
use std::path::{Path, PathBuf};
use tokio::process::Command;

/// Converts a .bin file to its .xml equivalent via serz64.
/// Result is cached; subsequent calls return the cached path unless `force` is true.
pub async fn convert_to_xml(bin_path: &Path, force: bool) -> Result<PathBuf> {
    let xml_path = xml_cache::cache_path_for(bin_path)?;

    if !force && xml_cache::is_valid(&xml_path, bin_path).await? {
        tracing::debug!("xml cache hit: {}", xml_path.display());
        return Ok(xml_path);
    }

    tracing::info!("converting {} via serz", bin_path.display());
    run_serz(bin_path)
        .await
        .with_context(|| format!("serz conversion failed for: {}", bin_path.display()))?;

    Ok(xml_path)
}

/// Converts an .xml file back to its .bin equivalent via serz64.
pub async fn convert_to_bin(xml_path: &Path) -> Result<PathBuf> {
    tracing::info!("converting {} to bin via serz", xml_path.display());
    run_serz(xml_path)
        .await
        .with_context(|| format!("serz bin conversion failed for: {}", xml_path.display()))?;

    let bin_path = xml_path.with_extension("");
    Ok(bin_path)
}

async fn run_serz(input: &Path) -> Result<()> {
    let windows_path = to_windows_path(input);
    let (program, args) = serz_command(&windows_path);

    let output = Command::new(&program)
        .args(&args)
        .output()
        .await
        .context("failed to spawn serz64 process")?;

    if !output.status.success() {
        let stderr = String::from_utf8_lossy(&output.stderr);
        anyhow::bail!("serz64 exited with status {}: {stderr}", output.status);
    }

    Ok(())
}

#[cfg(windows)]
fn serz_command(windows_path: &str) -> (String, Vec<String>) {
    (
        serz_executable_path(),
        vec![windows_path.to_string()],
    )
}

#[cfg(not(windows))]
fn serz_command(windows_path: &str) -> (String, Vec<String>) {
    (
        "wine".to_string(),
        vec![serz_executable_path(), windows_path.to_string()],
    )
}

fn serz_executable_path() -> String {
    // serz64.exe must be on PATH or adjacent to the game executable.
    // On Windows this is typically in the Railworks root.
    // The path can be overridden via settings.
    "serz64.exe".to_string()
}
