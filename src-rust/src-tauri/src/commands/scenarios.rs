use crate::{
    models::{Route, Scenario},
    platform::find_game_directory,
    serz,
    services::{scenario_db, scenario_parser, scenario_service},
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

/// Returns a scenario with its consists populated.
///
/// Triggers Serz conversion of `Scenario.bin` (cached), then runs the streaming
/// state-machine parser. For packed scenarios the .bin is first extracted from
/// the route's `MainContent.ap`.
#[tauri::command]
pub async fn get_scenario_detail(scenario: Scenario) -> Result<Scenario, String> {
    let bin_path = scenario_service::resolve_scenario_bin(&scenario)
        .await
        .map_err(|e| e.to_string())?;

    let xml_path = serz::convert_to_xml(&bin_path, false)
        .await
        .map_err(|e| format!("serz conversion: {e}"))?;

    let consists = tokio::task::spawn_blocking(move || scenario_parser::parse_consists(&xml_path))
        .await
        .map_err(|e| e.to_string())?
        .map_err(|e| e.to_string())?;

    tracing::info!(
        "scenario {}: parsed {} consists",
        scenario.id,
        consists.len()
    );

    Ok(Scenario { consists, ..scenario })
}

