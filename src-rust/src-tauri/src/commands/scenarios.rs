use crate::{
    models::{Route, Scenario},
    platform::find_game_directory,
    services::{scenario_db, scenario_service},
};
use dashmap::DashMap;
use std::sync::Arc;
use tokio::sync::OnceCell;

// Lazily initialised scenario player database, shared across commands.
static SCENARIO_DB: OnceCell<Arc<DashMap<String, crate::models::ScenarioPlayerInfo>>> =
    OnceCell::const_new();

async fn get_scenario_db() -> Arc<DashMap<String, crate::models::ScenarioPlayerInfo>> {
    SCENARIO_DB
        .get_or_init(|| async {
            let game_dir = match find_game_directory() {
                Ok(d) => d,
                Err(err) => {
                    tracing::warn!("could not find game directory for scenario db: {err}");
                    return Arc::new(DashMap::new());
                }
            };
            let sdb_path = game_dir.join("Content").join("SDBCache.bin");
            match scenario_db::load(&sdb_path, false).await {
                Ok(db) => db,
                Err(err) => {
                    tracing::warn!("could not load scenario db: {err}");
                    Arc::new(DashMap::new())
                }
            }
        })
        .await
        .clone()
}

/// Returns all scenarios for a given route (by route directory path).
#[tauri::command]
pub async fn get_scenarios(route: Route) -> Result<Vec<Scenario>, String> {
    let db = get_scenario_db().await;
    scenario_service::get_scenarios(&route, &db)
        .await
        .map_err(|e| e.to_string())
}

/// Returns a scenario with its consists populated (triggers Serz conversion).
#[tauri::command]
pub async fn get_scenario_detail(scenario: Scenario) -> Result<Scenario, String> {
    // Consist loading will be implemented in Phase 2 once the streaming parser is complete.
    // For now, return the scenario as-is.
    Ok(scenario)
}
