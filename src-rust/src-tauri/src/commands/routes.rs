use crate::{
    models::Route,
    platform::{app_config_dir, find_game_directory},
    services::route_service,
};
use tauri::ipc::Channel;

#[derive(Debug, serde::Serialize, Clone)]
#[serde(rename_all = "camelCase")]
pub struct ProgressEvent {
    pub current: usize,
    pub total: usize,
    pub message: String,
}

/// Returns the currently configured game directory path, or an error if unset.
#[tauri::command]
pub async fn get_game_path() -> Result<String, String> {
    find_game_directory()
        .map(|p| p.to_string_lossy().into_owned())
        .map_err(|e| e.to_string())
}

/// Saves a manually specified game path to settings.json.
#[tauri::command]
pub async fn set_game_path(path: String) -> Result<(), String> {
    save_game_path(&path).await.map_err(|e| e.to_string())
}

/// Discovers all routes in the game's Content/Routes directory.
/// Emits progress events via the supplied channel.
#[tauri::command]
pub async fn get_routes(on_progress: Channel<ProgressEvent>) -> Result<Vec<Route>, String> {
    let game_dir = find_game_directory().map_err(|e| e.to_string())?;
    let routes_dir = game_dir.join("Content").join("Routes");

    let _ = on_progress.send(ProgressEvent {
        current: 0,
        total: 0,
        message: "Scanning routes…".into(),
    });

    route_service::get_routes(&routes_dir)
        .await
        .map_err(|e| e.to_string())
}

/// Loads a single route by its route directory ID.
#[tauri::command]
pub async fn get_route(route_id: String) -> Result<Option<Route>, String> {
    let game_dir = find_game_directory().map_err(|e| e.to_string())?;
    let routes_dir = game_dir.join("Content").join("Routes");

    route_service::get_route(&routes_dir, &route_id)
        .await
        .map_err(|e| e.to_string())
}

async fn save_game_path(path: &str) -> anyhow::Result<()> {
    let config_dir = app_config_dir()?;
    tokio::fs::create_dir_all(&config_dir).await?;
    let settings_path = config_dir.join("settings.json");

    let mut settings: serde_json::Value = if settings_path.exists() {
        let content = tokio::fs::read_to_string(&settings_path).await?;
        serde_json::from_str(&content).unwrap_or(serde_json::json!({}))
    } else {
        serde_json::json!({})
    };

    settings["gamePath"] = serde_json::Value::String(path.to_string());
    tokio::fs::write(&settings_path, serde_json::to_string_pretty(&settings)?).await?;
    Ok(())
}
