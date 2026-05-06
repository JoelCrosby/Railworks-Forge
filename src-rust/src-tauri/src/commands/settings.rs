use crate::{
    cache::xml_cache,
    platform::settings::{self, AppSettings},
};

#[tauri::command]
pub async fn get_settings() -> Result<AppSettings, String> {
    settings::load().await.map_err(|e| e.to_string())
}

#[tauri::command]
pub async fn save_settings(settings: AppSettings) -> Result<AppSettings, String> {
    settings::save(&settings).await.map_err(|e| e.to_string())?;
    Ok(settings)
}

#[tauri::command]
pub async fn clear_xml_cache() -> Result<(), String> {
    xml_cache::clear().map_err(|e| e.to_string())
}
