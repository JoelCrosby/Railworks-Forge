use crate::{
    archive,
    models::{PackagingType, Route, Scenario, ScenarioClass, ScenarioPlayerInfo},
    xml::{parser::read_xml_file, selectors},
};
use anyhow::Result;
use std::{
    collections::HashMap,
    path::{Path, PathBuf},
};
use tokio::{fs, task::JoinSet};

/// Loads all scenarios for a given route, merging packed and unpacked variants.
///
/// Unpacked scenarios are discovered concurrently (one task per directory).
/// Packed scenarios are extracted and parsed concurrently from the route archive.
pub async fn get_scenarios(
    route: &Route,
    player_db: &dashmap::DashMap<String, ScenarioPlayerInfo>,
) -> Result<Vec<Scenario>> {
    let (unpacked, packed) = tokio::join!(
        load_unpacked_scenarios(route, player_db),
        load_packed_scenarios(route, player_db),
    );

    let mut by_id: HashMap<String, Scenario> = HashMap::new();

    // Unpacked takes priority over packed for the same scenario ID.
    for scenario in packed.unwrap_or_default() {
        by_id.insert(scenario.id.clone(), scenario);
    }
    for scenario in unpacked.unwrap_or_default() {
        by_id.insert(scenario.id.clone(), scenario);
    }

    let mut scenarios: Vec<Scenario> = by_id.into_values().collect();
    scenarios.sort_by(|a, b| a.name.cmp(&b.name));
    Ok(scenarios)
}

async fn load_unpacked_scenarios(
    route: &Route,
    player_db: &dashmap::DashMap<String, ScenarioPlayerInfo>,
) -> Result<Vec<Scenario>> {
    let scenarios_dir = route.scenarios_directory();
    if !fs::try_exists(&scenarios_dir).await.unwrap_or(false) {
        return Ok(Vec::new());
    }

    let mut entries = fs::read_dir(&scenarios_dir).await?;
    let mut tasks: JoinSet<Option<Scenario>> = JoinSet::new();

    while let Some(entry) = entries.next_entry().await? {
        let path = entry.path();
        if !path.is_dir() {
            continue;
        }
        let route_id = route.id.clone();
        let player_info = player_db
            .get(path.file_name().and_then(|n| n.to_str()).unwrap_or(""))
            .map(|v| v.clone());

        tasks.spawn(async move {
            match parse_scenario_from_dir(&path, &route_id, player_info).await {
                Ok(s) => s,
                Err(err) => {
                    tracing::warn!("skipping scenario at {}: {err:#}", path.display());
                    None
                }
            }
        });
    }

    let mut scenarios = Vec::with_capacity(tasks.len());
    while let Some(result) = tasks.join_next().await {
        if let Ok(Some(s)) = result {
            scenarios.push(s);
        }
    }
    Ok(scenarios)
}

async fn parse_scenario_from_dir(
    dir: &Path,
    route_id: &str,
    player_info: Option<ScenarioPlayerInfo>,
) -> Result<Option<Scenario>> {
    let props_path = dir.join("ScenarioProperties.xml");
    if !fs::try_exists(&props_path).await.unwrap_or(false) {
        return Ok(None);
    }

    let xml = read_xml_file(&props_path).await?;
    let player_info = player_info.unwrap_or_else(|| {
        ScenarioPlayerInfo::empty(dir.file_name().and_then(|n| n.to_str()).unwrap_or(""))
    });
    let scenario = build_scenario(dir, &xml, route_id, PackagingType::Unpacked, player_info)?;
    Ok(Some(scenario))
}

async fn load_packed_scenarios(
    route: &Route,
    player_db: &dashmap::DashMap<String, ScenarioPlayerInfo>,
) -> Result<Vec<Scenario>> {
    let archive_path = route.main_content_archive_path();
    if !fs::try_exists(&archive_path).await.unwrap_or(false) {
        return Ok(Vec::new());
    }

    // List all scenario IDs from the archive in one blocking call.
    let scenario_ids = tokio::task::spawn_blocking({
        let archive_path = archive_path.clone();
        move || -> Result<Vec<String>> {
            let entries = archive::entries_with_prefix(&archive_path, "Scenarios/")?;
            let ids: std::collections::HashSet<String> = entries
                .iter()
                .filter_map(|e| {
                    let parts: Vec<&str> = e.splitn(3, '/').collect();
                    if parts.len() >= 2 && !parts[1].is_empty() {
                        Some(parts[1].to_string())
                    } else {
                        None
                    }
                })
                .collect();
            Ok(ids.into_iter().collect())
        }
    })
    .await??;

    // Spawn a concurrent task per scenario to read its ScenarioProperties.xml.
    let mut tasks: JoinSet<Option<Scenario>> = JoinSet::new();
    let scenarios_dir = route.scenarios_directory();

    for scenario_id in scenario_ids {
        let archive_path = archive_path.clone();
        let dir = scenarios_dir.join(&scenario_id);
        let route_id = route.id.clone();
        let player_info = player_db.get(&scenario_id).map(|v| v.clone());

        tasks.spawn(async move {
            let entry = format!("Scenarios/{scenario_id}/ScenarioProperties.xml");
            let xml = match tokio::task::spawn_blocking(move || {
                archive::read_entry_as_string(&archive_path, &entry)
            })
            .await
            {
                Ok(Ok(xml)) => xml,
                _ => return None,
            };

            let player_info =
                player_info.unwrap_or_else(|| ScenarioPlayerInfo::empty(&scenario_id));
            build_scenario(&dir, &xml, &route_id, PackagingType::Packed, player_info)
                .map(Some)
                .unwrap_or_else(|err| {
                    tracing::warn!("skipping packed scenario {scenario_id}: {err:#}");
                    None
                })
        });
    }

    let mut scenarios = Vec::with_capacity(tasks.len());
    while let Some(result) = tasks.join_next().await {
        if let Ok(Some(s)) = result {
            scenarios.push(s);
        }
    }
    Ok(scenarios)
}

fn build_scenario(
    dir: &Path,
    xml: &str,
    route_id: &str,
    packaging: PackagingType,
    player_info: ScenarioPlayerInfo,
) -> Result<Scenario> {
    let id = dir
        .file_name()
        .and_then(|n| n.to_str())
        .unwrap_or("unknown")
        .to_string();

    let name = selectors::select_localised(xml, "DisplayName")
        .or_else(|| selectors::select_text(xml, "DisplayName"))
        .unwrap_or_else(|| id.clone());

    let description = selectors::select_localised(xml, "Description")
        .or_else(|| selectors::select_text(xml, "Description"));

    let briefing = selectors::select_localised(xml, "Briefing")
        .or_else(|| selectors::select_text(xml, "Briefing"));

    let start_location = selectors::select_text(xml, "StartLocation");

    let locomotive = selectors::select_text(xml, "LocoName")
        .or_else(|| selectors::select_text(xml, "sDriverFrontEndDetails"))
        .unwrap_or_default();

    let duration = selectors::select_integer(xml, "Duration").unwrap_or(0);
    let rating = selectors::select_integer(xml, "Rating").unwrap_or(0);

    let season = selectors::select_text(xml, "Season").unwrap_or_else(|| "Summer".to_string());

    let scenario_class = selectors::select_text(xml, "ScenarioClass")
        .map(|s| ScenarioClass::from_str(&s))
        .unwrap_or(ScenarioClass::Empty);

    Ok(Scenario {
        id,
        name,
        description,
        briefing,
        start_location,
        locomotive,
        duration,
        rating,
        season,
        scenario_class,
        packaging_type: packaging,
        directory_path: dir.to_path_buf(),
        route_id: route_id.to_string(),
        player_info,
        consists: Vec::new(),
    })
}

/// Returns the path to `Scenario.bin`, extracting it from the route archive first
/// when the scenario is packed.
pub async fn resolve_scenario_bin(scenario: &Scenario) -> anyhow::Result<PathBuf> {
    let bin_path = scenario.binary_path();
    if bin_path.exists() {
        return Ok(bin_path);
    }

    // Packed: the .bin lives inside the route's MainContent.ap.
    let route_dir = scenario
        .directory_path
        .parent()
        .and_then(|p| p.parent())
        .ok_or_else(|| {
            anyhow::anyhow!(
                "cannot determine route directory from: {}",
                scenario.directory_path.display()
            )
        })?;

    let archive_path = route_dir.join("MainContent.ap");
    anyhow::ensure!(
        archive_path.exists(),
        "no Scenario.bin or MainContent.ap found for scenario {}",
        scenario.id
    );

    let entry_name = format!("Scenarios/{}/Scenario.bin", scenario.id);
    let destination = bin_path.clone();

    if let Some(parent) = destination.parent() {
        tokio::fs::create_dir_all(parent).await?;
    }

    tokio::task::spawn_blocking(move || {
        let bytes = archive::read_entry(&archive_path, &entry_name)?;
        std::fs::write(&destination, bytes)?;
        Ok::<(), anyhow::Error>(())
    })
    .await??;

    Ok(bin_path)
}
