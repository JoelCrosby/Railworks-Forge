use crate::{
    models::{Route, Scenario},
    platform::find_game_directory,
    services::{scenario_db, scenario_parser, scenario_service},
    serz,
};
use anyhow::Result;
use dashmap::DashMap;
use std::sync::Arc;
use tauri::Emitter;
use tokio::sync::OnceCell;

static SCENARIO_DB: OnceCell<Arc<DashMap<String, crate::models::ScenarioPlayerInfo>>> =
    OnceCell::const_new();

#[derive(serde::Serialize, Clone)]
#[serde(tag = "status", rename_all = "lowercase")]
enum ScenarioDbStatus {
    Loading,
    Ready,
    Failed { message: String },
}

async fn try_load_scenario_db() -> Result<Arc<DashMap<String, crate::models::ScenarioPlayerInfo>>> {
    let game_dir = find_game_directory()?;
    let sdb_path = game_dir.join("Content").join("SDBCache.bin");
    scenario_db::load(&sdb_path, false).await
}

fn current_scenario_db_or_empty() -> Arc<DashMap<String, crate::models::ScenarioPlayerInfo>> {
    SCENARIO_DB
        .get()
        .cloned()
        .unwrap_or_else(|| Arc::new(DashMap::new()))
}

/// Eagerly initialises the scenario player DB in the background, emitting
/// `scenario-db-status` events so the UI can show progress.
pub async fn prime_scenario_db<R: tauri::Runtime>(app: tauri::AppHandle<R>) {
    let _ = app.emit("scenario-db-status", ScenarioDbStatus::Loading);
    SCENARIO_DB
        .get_or_init(|| async {
            match try_load_scenario_db().await {
                Ok(db) => {
                    let _ = app.emit("scenario-db-status", ScenarioDbStatus::Ready);
                    db
                }
                Err(err) => {
                    tracing::warn!("could not load scenario db: {err:#}");
                    let _ = app.emit(
                        "scenario-db-status",
                        ScenarioDbStatus::Failed {
                            message: err.to_string(),
                        },
                    );
                    Arc::new(DashMap::new())
                }
            }
        })
        .await;
}

/// Returns all scenarios for a given route (by route directory path).
#[tauri::command]
pub async fn get_scenarios(route: Route) -> Result<Vec<Scenario>, String> {
    let db = current_scenario_db_or_empty();
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

    Ok(Scenario {
        consists,
        ..scenario
    })
}
