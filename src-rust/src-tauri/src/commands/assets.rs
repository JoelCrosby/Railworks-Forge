use crate::{
    models::Scenario,
    platform::find_game_directory,
    services::asset_service::{self, AssetNode},
};

#[derive(Debug, serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AssetCheckResult {
    pub consist_id: String,
    pub vehicle_name: String,
    pub state: String,
}

/// Checks acquisition state for all vehicles in a scenario's consists.
#[tauri::command]
pub async fn check_assets(scenario: Scenario) -> Result<Vec<AssetCheckResult>, String> {
    let game_dir = find_game_directory().map_err(|e| e.to_string())?;
    let assets_root = game_dir.join("Assets");

    let mut results = Vec::new();
    for consist in &scenario.consists {
        for vehicle in &consist.vehicles {
            let state = asset_service::check_acquisition(&vehicle.blueprint, &assets_root);
            results.push(AssetCheckResult {
                consist_id: consist.id.clone(),
                vehicle_name: vehicle.name.clone(),
                state: format!("{state:?}"),
            });
        }
    }

    Ok(results)
}

/// Returns the provider/product asset tree for the asset browser.
#[tauri::command]
pub async fn get_asset_tree() -> Result<Vec<AssetNode>, String> {
    let game_dir = find_game_directory().map_err(|e| e.to_string())?;
    let assets_root = game_dir.join("Assets");
    asset_service::get_asset_tree(&assets_root).map_err(|e| e.to_string())
}
