use crate::models::{Consist, Scenario};
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ReplaceConsistRequest {
    pub scenario: Scenario,
    pub target_consist_id: String,
    pub preload_consist_service_name: String,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AddVehicleRequest {
    pub scenario: Scenario,
    pub consist_id: String,
    pub provider: String,
    pub product: String,
    pub blueprint_id: String,
    pub flipped: bool,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DeleteVehicleRequest {
    pub scenario: Scenario,
    pub consist_id: String,
    pub vehicle_index: usize,
}

/// Returns the full consist detail including all vehicles.
/// Triggers Serz conversion of Scenario.bin if not already cached.
#[tauri::command]
pub async fn get_consist_detail(consist: Consist) -> Result<Consist, String> {
    // Phase 2: streaming XML parser for Scenario.bin.xml
    Ok(consist)
}

/// Persists a consist after an in-frontend edit, writing back to Scenario.bin via Serz.
#[tauri::command]
pub async fn save_consist(scenario: Scenario) -> Result<(), String> {
    // Phase 3: implement write-back
    tracing::info!("save_consist called for scenario {}", scenario.id);
    Ok(())
}

/// Replaces an entire service consist with a preload consist template.
#[tauri::command]
pub async fn replace_consist(request: ReplaceConsistRequest) -> Result<Scenario, String> {
    tracing::info!(
        "replace_consist: {} -> {}",
        request.target_consist_id,
        request.preload_consist_service_name
    );
    // Phase 3: implement command execution
    Ok(request.scenario)
}

/// Adds a vehicle to an existing consist.
#[tauri::command]
pub async fn add_vehicle(request: AddVehicleRequest) -> Result<Scenario, String> {
    tracing::info!(
        "add_vehicle to consist {} in scenario {}",
        request.consist_id,
        request.scenario.id
    );
    // Phase 3: implement command execution
    Ok(request.scenario)
}

/// Removes a vehicle from a consist by index.
#[tauri::command]
pub async fn delete_vehicle(request: DeleteVehicleRequest) -> Result<Scenario, String> {
    tracing::info!(
        "delete_vehicle [{}] from consist {} in scenario {}",
        request.vehicle_index,
        request.consist_id,
        request.scenario.id
    );
    // Phase 3: implement command execution
    Ok(request.scenario)
}
