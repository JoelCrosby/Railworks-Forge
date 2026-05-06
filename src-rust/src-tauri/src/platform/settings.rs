use super::app_config_dir;
use anyhow::Result;
use serde::{Deserialize, Serialize};
use std::path::PathBuf;

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AppSettings {
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub game_path: Option<String>,
    #[serde(default = "default_theme")]
    pub theme: String,
    #[serde(default = "default_locale")]
    pub locale: String,
}

impl Default for AppSettings {
    fn default() -> Self {
        Self {
            game_path: None,
            theme: default_theme(),
            locale: default_locale(),
        }
    }
}

pub fn settings_path() -> Result<PathBuf> {
    Ok(app_config_dir()?.join("settings.json"))
}

pub async fn load() -> Result<AppSettings> {
    let path = settings_path()?;
    if !path.exists() {
        return Ok(AppSettings::default());
    }

    let content = tokio::fs::read_to_string(path).await?;
    Ok(serde_json::from_str(&content).unwrap_or_default())
}

pub async fn save(settings: &AppSettings) -> Result<()> {
    let path = settings_path()?;
    if let Some(parent) = path.parent() {
        tokio::fs::create_dir_all(parent).await?;
    }
    let json = serde_json::to_string_pretty(settings)?;
    tokio::fs::write(path, json).await?;
    Ok(())
}

pub async fn set_game_path(path: &str) -> Result<()> {
    let mut settings = load().await?;
    settings.game_path = Some(path.to_string());
    save(&settings).await
}

fn default_theme() -> String {
    "dark".to_string()
}

fn default_locale() -> String {
    "en-US".to_string()
}
