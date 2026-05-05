use crate::{
    models::{Consist, Scenario},
    services::{
        consist_commands::{ConsistCommand, SavedConsist, VehicleEntry},
        persistence,
        scenario_service,
    },
};
use serde::{Deserialize, Serialize};

// ── Request types ────────────────────────────────────────────────────────────

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ReplaceConsistRequest {
    pub scenario: Scenario,
    pub target_consist_id: String,
    pub entries: Vec<VehicleEntry>,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AddVehicleRequest {
    pub scenario: Scenario,
    pub consist_id: String,
    pub entry: VehicleEntry,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DeleteVehicleRequest {
    pub scenario: Scenario,
    pub consist_id: String,
    pub vehicle_index: usize,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DeleteConsistRequest {
    pub scenario: Scenario,
    pub consist_id: String,
}

// ── Read commands ────────────────────────────────────────────────────────────

/// Returns the full consist (already populated by `get_scenario_detail`).
#[tauri::command]
pub async fn get_consist_detail(consist: Consist) -> Result<Consist, String> {
    Ok(consist)
}

// ── Write commands ───────────────────────────────────────────────────────────

/// Replaces all vehicles in a consist with the provided entries.
#[tauri::command]
pub async fn replace_consist(request: ReplaceConsistRequest) -> Result<Scenario, String> {
    let bin_path = scenario_service::resolve_scenario_bin(&request.scenario)
        .await
        .map_err(|e| e.to_string())?;

    let command = ConsistCommand::ReplaceVehicles {
        consist_id: request.target_consist_id,
        entries: request.entries,
    };

    persistence::apply_edits(request.scenario, bin_path, vec![command])
        .await
        .map_err(|e| e.to_string())
}

/// Appends a single vehicle to an existing consist.
#[tauri::command]
pub async fn add_vehicle(request: AddVehicleRequest) -> Result<Scenario, String> {
    let bin_path = scenario_service::resolve_scenario_bin(&request.scenario)
        .await
        .map_err(|e| e.to_string())?;

    // Collect existing vehicles plus the new one, then replace the consist.
    let existing = request
        .scenario
        .consists
        .iter()
        .find(|c| c.id == request.consist_id)
        .map(|c| {
            c.vehicles
                .iter()
                .map(|v| VehicleEntry {
                    provider: v.blueprint.provider.clone(),
                    product: v.blueprint.product.clone(),
                    blueprint_id: v.blueprint.blueprint_id.clone(),
                    flipped: v.flipped,
                    blueprint_type: v.blueprint_type.clone(),
                })
                .collect::<Vec<_>>()
        })
        .unwrap_or_default();

    let mut entries = existing;
    entries.push(request.entry);

    let command = ConsistCommand::ReplaceVehicles {
        consist_id: request.consist_id,
        entries,
    };

    persistence::apply_edits(request.scenario, bin_path, vec![command])
        .await
        .map_err(|e| e.to_string())
}

/// Removes a vehicle from a consist by its 0-based index.
#[tauri::command]
pub async fn delete_vehicle(request: DeleteVehicleRequest) -> Result<Scenario, String> {
    let bin_path = scenario_service::resolve_scenario_bin(&request.scenario)
        .await
        .map_err(|e| e.to_string())?;

    let command = ConsistCommand::DeleteVehicle {
        consist_id: request.consist_id,
        vehicle_index: request.vehicle_index,
    };

    persistence::apply_edits(request.scenario, bin_path, vec![command])
        .await
        .map_err(|e| e.to_string())
}

/// Removes an entire consist from a scenario.
#[tauri::command]
pub async fn delete_consist(request: DeleteConsistRequest) -> Result<Scenario, String> {
    let bin_path = scenario_service::resolve_scenario_bin(&request.scenario)
        .await
        .map_err(|e| e.to_string())?;

    let command = ConsistCommand::DeleteConsist {
        consist_id: request.consist_id,
    };

    persistence::apply_edits(request.scenario, bin_path, vec![command])
        .await
        .map_err(|e| e.to_string())
}

// ── Saved consist templates ──────────────────────────────────────────────────

/// Persists a consist entry list under a user-defined name.
#[tauri::command]
pub async fn save_consist(consist: SavedConsist) -> Result<(), String> {
    persistence::save_consist_template(consist).map_err(|e| e.to_string())
}

/// Returns all saved consist templates.
#[tauri::command]
pub async fn get_saved_consists() -> Result<Vec<SavedConsist>, String> {
    persistence::load_consist_templates().map_err(|e| e.to_string())
}

/// Removes a saved consist template by name.
#[tauri::command]
pub async fn delete_saved_consist(name: String) -> Result<(), String> {
    persistence::delete_consist_template(&name).map_err(|e| e.to_string())
}
