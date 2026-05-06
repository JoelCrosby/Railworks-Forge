use crate::{models::Route, services::track_service};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct TrackBlueprint {
    pub provider: String,
    pub product: String,
    pub blueprint_id: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct TrackReplacement {
    pub from: TrackBlueprint,
    /// None means "no replacement selected for this blueprint".
    pub to: Option<TrackBlueprint>,
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
    track_service::get_tracks(&route)
        .await
        .map_err(|e| e.to_string())
}

/// Replaces track blueprint references in Tracks.bin and updates RouteProperties.xml.
#[tauri::command]
pub async fn replace_tracks(request: ReplaceTracksRequest) -> Result<(), String> {
    track_service::replace_tracks(&request.route, &request.replacements)
        .await
        .map_err(|e| e.to_string())
}
