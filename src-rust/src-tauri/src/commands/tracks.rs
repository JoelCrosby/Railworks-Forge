use crate::models::Route;
use serde::{Deserialize, Serialize};

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct TrackBlueprint {
    pub provider: String,
    pub product: String,
    pub blueprint_id: String,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct TrackReplacement {
    pub from: TrackBlueprint,
    pub to: TrackBlueprint,
}

#[derive(Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ReplaceTracksRequest {
    pub route: Route,
    pub replacements: Vec<TrackReplacement>,
}

/// Returns all unique track blueprints referenced in a route's Tracks.bin.
#[tauri::command]
pub async fn get_tracks(route: Route) -> Result<Vec<TrackBlueprint>, String> {
    tracing::info!("get_tracks for route {}", route.id);
    // Phase 4: streaming parse of Tracks.bin.xml
    Ok(Vec::new())
}

/// Replaces track blueprint references in Tracks.bin and RouteProperties.xml.
#[tauri::command]
pub async fn replace_tracks(request: ReplaceTracksRequest) -> Result<(), String> {
    tracing::info!(
        "replace_tracks for route {} ({} replacements)",
        request.route.id,
        request.replacements.len()
    );
    // Phase 4: implement track service
    Ok(())
}
