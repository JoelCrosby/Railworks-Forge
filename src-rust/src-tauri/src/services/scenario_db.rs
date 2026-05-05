use crate::{
    models::ScenarioPlayerInfo,
    platform::app_cache_dir,
    serz::convert_to_xml,
    xml::parser,
};
use anyhow::Result;
use dashmap::DashMap;
use quick_xml::{events::Event, Reader};
use std::path::Path;
use std::sync::Arc;

const CACHE_FILE: &str = "SDBCache.json";

/// Loads the scenario player database from SDBCache.bin.
/// Results are cached to JSON; subsequent calls return from cache unless `force` is true.
pub async fn load(sdb_bin_path: &Path, force: bool) -> Result<Arc<DashMap<String, ScenarioPlayerInfo>>> {
    let cache_path = app_cache_dir()?.join(CACHE_FILE);

    if !force && cache_path.exists() {
        if let Ok(db) = load_from_json_cache(&cache_path).await {
            tracing::debug!("scenario db loaded from json cache");
            return Ok(Arc::new(db));
        }
    }

    let xml_path = convert_to_xml(sdb_bin_path, force).await?;
    let xml = parser::read_xml_file(&xml_path).await?;
    let db = parse_scenario_db(&xml)?;

    persist_to_json_cache(&cache_path, &db).await?;
    Ok(Arc::new(db))
}

fn parse_scenario_db(xml: &str) -> Result<DashMap<String, ScenarioPlayerInfo>> {
    let db = DashMap::new();
    let mut reader = Reader::from_str(xml);
    reader.config_mut().trim_text(true);

    let mut buf = Vec::new();
    let mut current_id = String::new();
    let mut current_score = 0i32;
    let mut current_completion = String::new();
    let mut current_medals = 0i32;
    let mut current_field: Option<&'static str> = None;
    let mut in_scenario = false;

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(e)) => {
                let name = std::str::from_utf8(e.name().as_ref()).unwrap_or("").to_string();
                match name.as_str() {
                    "sSDScenario" => {
                        in_scenario = true;
                        current_id.clear();
                        current_score = 0;
                        current_completion.clear();
                        current_medals = 0;
                    }
                    "ScenarioId" if in_scenario => current_field = Some("id"),
                    "Score" if in_scenario => current_field = Some("score"),
                    "Completion" if in_scenario => current_field = Some("completion"),
                    "MedalsAwarded" if in_scenario => current_field = Some("medals"),
                    _ => {}
                }
            }
            Ok(Event::Text(e)) => {
                if let Some(field) = current_field {
                    let text = e.unescape().unwrap_or_default();
                    match field {
                        "id" => current_id = text.into_owned(),
                        "score" => current_score = text.parse().unwrap_or(0),
                        "completion" => current_completion = text.into_owned(),
                        "medals" => current_medals = text.parse().unwrap_or(0),
                        _ => {}
                    }
                    current_field = None;
                }
            }
            Ok(Event::End(e)) => {
                if std::str::from_utf8(e.name().as_ref()).unwrap_or("") == "sSDScenario" && in_scenario {
                    if !current_id.is_empty() {
                        db.insert(
                            current_id.clone(),
                            ScenarioPlayerInfo {
                                scenario_id: current_id.clone(),
                                score: current_score,
                                completion: current_completion.clone(),
                                medals_awarded: current_medals,
                            },
                        );
                    }
                    in_scenario = false;
                }
            }
            Ok(Event::Eof) => break,
            Err(err) => {
                tracing::warn!("xml parse error in scenario db: {err}");
                break;
            }
            _ => {}
        }
        buf.clear();
    }

    Ok(db)
}

async fn load_from_json_cache(path: &Path) -> Result<DashMap<String, ScenarioPlayerInfo>> {
    let content = tokio::fs::read_to_string(path).await?;
    let map: std::collections::HashMap<String, ScenarioPlayerInfo> =
        serde_json::from_str(&content)?;
    let db = DashMap::new();
    for (k, v) in map {
        db.insert(k, v);
    }
    Ok(db)
}

async fn persist_to_json_cache(
    path: &Path,
    db: &DashMap<String, ScenarioPlayerInfo>,
) -> Result<()> {
    if let Some(parent) = path.parent() {
        tokio::fs::create_dir_all(parent).await?;
    }
    let map: std::collections::HashMap<String, ScenarioPlayerInfo> =
        db.iter().map(|e| (e.key().clone(), e.value().clone())).collect();
    let json = serde_json::to_string_pretty(&map)?;
    tokio::fs::write(path, json).await?;
    Ok(())
}
