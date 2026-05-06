use anyhow::{Context, Result};
use std::path::{Path, PathBuf};

const APP_NAME: &str = "railworks-forge";

pub fn app_config_dir() -> Result<PathBuf> {
    dirs::config_dir()
        .map(|p| p.join(APP_NAME))
        .context("could not determine config directory")
}

pub fn app_cache_dir() -> Result<PathBuf> {
    dirs::cache_dir()
        .map(|p| p.join(APP_NAME))
        .context("could not determine cache directory")
}

pub fn app_log_dir() -> Result<PathBuf> {
    app_config_dir().map(|p| p.join("logs"))
}

/// Converts a native path to a Windows-style path string for serz64.exe invocation under Wine.
pub fn to_windows_path(path: &Path) -> String {
    #[cfg(windows)]
    {
        path.to_string_lossy().replace('/', "\\")
    }
    #[cfg(not(windows))]
    {
        // Under Wine, Linux paths are accessible as Z:\path\to\file
        format!("Z:{}", path.to_string_lossy().replace('/', "\\"))
    }
}

/// Locates the Railworks game installation directory.
///
/// Search order:
///   1. Windows registry (Windows only)
///   2. ~/.config/railworks-forge/settings.json `gamePath` key
///   3. Common Steam library paths
pub fn find_game_directory() -> Result<PathBuf> {
    #[cfg(windows)]
    if let Ok(path) = find_from_registry() {
        return Ok(path);
    }

    if let Ok(path) = find_from_settings() {
        return Ok(path);
    }

    find_from_steam_library()
        .context("Could not locate Railworks installation. Set the game path in Settings.")
}

#[cfg(windows)]
fn find_from_registry() -> Result<PathBuf> {
    use winreg::enums::HKEY_LOCAL_MACHINE;
    use winreg::RegKey;

    let hklm = RegKey::predef(HKEY_LOCAL_MACHINE);
    let key = hklm.open_subkey("SOFTWARE\\WOW6432Node\\Railsimulator.com\\RailWorks")?;
    let install_path: String = key.get_value("Install_Path")?;
    Ok(PathBuf::from(install_path))
}

fn find_from_settings() -> Result<PathBuf> {
    let settings_path = super::settings::settings_path()?;
    let content = std::fs::read_to_string(settings_path)?;
    let settings: super::settings::AppSettings = serde_json::from_str(&content)?;
    let path = settings
        .game_path
        .context("gamePath not set in settings.json")?;
    Ok(PathBuf::from(path))
}

fn find_from_steam_library() -> Result<PathBuf> {
    let candidates = steam_library_candidates();
    for candidate in candidates {
        if candidate.exists() {
            return Ok(candidate);
        }
    }
    anyhow::bail!("no Steam library candidate found")
}

fn steam_library_candidates() -> Vec<PathBuf> {
    let mut candidates = Vec::new();

    #[cfg(target_os = "linux")]
    {
        if let Some(home) = dirs::home_dir() {
            candidates.push(home.join(".steam/steam/steamapps/common/RailWorks"));
            candidates.push(home.join(".local/share/Steam/steamapps/common/RailWorks"));
        }
    }

    #[cfg(target_os = "macos")]
    {
        if let Some(home) = dirs::home_dir() {
            candidates
                .push(home.join("Library/Application Support/Steam/steamapps/common/RailWorks"));
        }
    }

    #[cfg(windows)]
    {
        candidates.push(PathBuf::from(
            r"C:\Program Files (x86)\Steam\steamapps\common\RailWorks",
        ));
        candidates.push(PathBuf::from(
            r"C:\Program Files\Steam\steamapps\common\RailWorks",
        ));
    }

    candidates
}
