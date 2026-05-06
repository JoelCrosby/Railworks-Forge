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
    run_serz(bin_path, Some(("xml", &xml_path)))
        .await
        .with_context(|| format!("serz conversion failed for: {}", bin_path.display()))?;

    if !xml_path.exists() {
        // Older Serz invocations write beside the input. Keep this as a
        // compatibility fallback, but prefer the explicit output path above.
        let adjacent_output = default_serz_xml_output_path(bin_path);
        if adjacent_output.exists() {
            tokio::fs::copy(&adjacent_output, &xml_path)
                .await
                .with_context(|| {
                    format!(
                        "copying fallback serz output {} to cache {}",
                        adjacent_output.display(),
                        xml_path.display()
                    )
                })?;
            let _ = tokio::fs::remove_file(&adjacent_output).await;
        } else {
            anyhow::bail!(
                "serz did not create expected XML output at {} or fallback output at {}",
                xml_path.display(),
                adjacent_output.display()
            );
        }
    }

    Ok(xml_path)
}

/// Converts an .xml file back to its .bin equivalent via serz64.
pub async fn convert_to_bin(xml_path: &Path, bin_path: &Path) -> Result<PathBuf> {
    tracing::info!("converting {} to bin via serz", xml_path.display());
    run_serz(xml_path, Some(("bin", bin_path)))
        .await
        .with_context(|| format!("serz bin conversion failed for: {}", xml_path.display()))?;

    anyhow::ensure!(
        bin_path.exists(),
        "serz did not create expected binary output at {}",
        bin_path.display()
    );
    Ok(bin_path.to_path_buf())
}

async fn run_serz(input: &Path, output: Option<(&str, &Path)>) -> Result<()> {
    let windows_path = to_windows_path(input);
    let output_arg = if let Some((kind, path)) = output {
        if let Some(parent) = path.parent() {
            std::fs::create_dir_all(parent)
                .with_context(|| format!("creating serz output directory: {}", parent.display()))?;
        }
        Some(format!("\\{kind}: {}", to_windows_path(path)))
    } else {
        None
    };
    let (program, args) = serz_command(&windows_path, output_arg);

    let child = Command::new(&program)
        .args(&args)
        .kill_on_drop(true)
        .spawn()
        .context("failed to spawn serz64 process")?;

    let output = tokio::time::timeout(
        std::time::Duration::from_secs(120),
        child.wait_with_output(),
    )
    .await
    .context("serz64 timed out after 120s")?
    .context("serz64 process error")?;

    if !output.status.success() {
        let stderr = String::from_utf8_lossy(&output.stderr);
        let stdout = String::from_utf8_lossy(&output.stdout);
        anyhow::bail!(
            "serz64 exited with status {}: stdout={stdout}; stderr={stderr}",
            output.status
        );
    }

    Ok(())
}

#[cfg(windows)]
fn serz_command(windows_path: &str, output_arg: Option<String>) -> (String, Vec<String>) {
    let mut args = vec![windows_path.to_string()];
    if let Some(output_arg) = output_arg {
        args.push(output_arg);
    }
    (serz_executable_path(), args)
}

#[cfg(not(windows))]
fn serz_command(windows_path: &str, output_arg: Option<String>) -> (String, Vec<String>) {
    let serz_win_path = to_windows_path(&std::path::PathBuf::from(serz_executable_path()));
    let mut args = vec![serz_win_path, windows_path.to_string()];
    if let Some(output_arg) = output_arg {
        args.push(output_arg);
    }
    ("wine".to_string(), args)
}

fn serz_executable_path() -> String {
    // Prefer the serz64.exe that ships with the game. Fall back to PATH lookup.
    if let Ok(game_dir) = crate::platform::find_game_directory() {
        let candidate = game_dir.join("serz64.exe");
        if candidate.exists() {
            return candidate.to_string_lossy().into_owned();
        }
    }
    "serz64.exe".to_string()
}

fn default_serz_xml_output_path(bin_path: &Path) -> PathBuf {
    bin_path.with_extension(
        bin_path
            .extension()
            .map(|e| format!("{}.xml", e.to_string_lossy()))
            .unwrap_or_else(|| "xml".to_string()),
    )
}
