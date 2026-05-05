use crate::{
    archive,
    models::{PackagingType, Route, Scenario, ScenarioClass, ScenarioPlayerInfo},
    xml::{parser::read_xml_file, selectors},
};
use anyhow::Result;
use std::path::{Path, PathBuf};
use tokio::fs;

/// Loads all scenarios for a given route, merging packed and unpacked variants.
pub async fn get_scenarios(
    route: &Route,
    player_db: &dashmap::DashMap<String, ScenarioPlayerInfo>,
) -> Result<Vec<Scenario>> {
    let mut scenarios = Vec::new();

    let scenarios_dir = route.scenarios_directory();
    if scenarios_dir.exists() {
        let mut entries = fs::read_dir(&scenarios_dir).await?;
        while let Some(entry) = entries.next_entry().await? {
            let path = entry.path();
            if !path.is_dir() {
                continue;
            }

            match parse_scenario_from_dir(&path, route, player_db).await {
                Ok(Some(s)) => scenarios.push(s),
                Ok(None) => {}
                Err(err) => {
                    tracing::warn!("skipping scenario at {}: {err:#}", path.display());
                }
            }
        }
    }

    // Also check for scenarios packed inside the route's MainContent.ap
    let archive_path = route.main_content_archive_path();
    if archive_path.exists() {
        let packed = load_packed_scenarios(route, &archive_path, player_db).await;
        match packed {
            Ok(mut packed_scenarios) => scenarios.append(&mut packed_scenarios),
            Err(err) => {
                tracing::warn!("skipping packed scenarios for {}: {err:#}", route.id);
            }
        }
    }

    // Deduplicate by id (unpacked takes priority over packed)
    scenarios.dedup_by(|a, b| {
        if a.id == b.id {
            // Keep whichever is Unpacked
            if a.packaging_type == PackagingType::Unpacked {
                true // remove b
            } else {
                false
            }
        } else {
            false
        }
    });

    scenarios.sort_by(|a, b| a.name.cmp(&b.name));
    Ok(scenarios)
}

async fn parse_scenario_from_dir(
    dir: &Path,
    route: &Route,
    player_db: &dashmap::DashMap<String, ScenarioPlayerInfo>,
) -> Result<Option<Scenario>> {
    let props_path = dir.join("ScenarioProperties.xml");
    if !props_path.exists() {
        return Ok(None);
    }

    let xml = read_xml_file(&props_path).await?;
    let scenario = build_scenario(dir, &xml, route, PackagingType::Unpacked, player_db)?;
    Ok(Some(scenario))
}

async fn load_packed_scenarios(
    route: &Route,
    archive_path: &Path,
    player_db: &dashmap::DashMap<String, ScenarioPlayerInfo>,
) -> Result<Vec<Scenario>> {
    let entries = tokio::task::spawn_blocking({
        let archive_path = archive_path.to_path_buf();
        move || archive::entries_with_prefix(&archive_path, "Scenarios/")
    })
    .await??;

    let scenario_dirs: std::collections::HashSet<String> = entries
        .iter()
        .filter_map(|e| {
            let parts: Vec<&str> = e.splitn(3, '/').collect();
            if parts.len() >= 2 {
                Some(parts[1].to_string())
            } else {
                None
            }
        })
        .collect();

    let mut scenarios = Vec::new();
    for scenario_id in scenario_dirs {
        let entry = format!("Scenarios/{scenario_id}/ScenarioProperties.xml");
        let xml = match tokio::task::spawn_blocking({
            let archive_path = archive_path.to_path_buf();
            let entry = entry.clone();
            move || archive::read_entry_as_string(&archive_path, &entry)
        })
        .await?
        {
            Ok(xml) => xml,
            Err(_) => continue,
        };

        let dir = route.scenarios_directory().join(&scenario_id);
        match build_scenario(&dir, &xml, route, PackagingType::Packed, player_db) {
            Ok(s) => scenarios.push(s),
            Err(err) => tracing::warn!("skipping packed scenario {scenario_id}: {err:#}"),
        }
    }

    Ok(scenarios)
}

fn build_scenario(
    dir: &Path,
    xml: &str,
    route: &Route,
    packaging: PackagingType,
    player_db: &dashmap::DashMap<String, ScenarioPlayerInfo>,
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

    let season = selectors::select_text(xml, "Season")
        .unwrap_or_else(|| "Summer".to_string());

    let scenario_class = selectors::select_text(xml, "ScenarioClass")
        .map(|s| ScenarioClass::from_str(&s))
        .unwrap_or(ScenarioClass::Empty);

    let player_info = player_db
        .get(&id)
        .map(|v| v.clone())
        .unwrap_or_else(|| ScenarioPlayerInfo::empty(&id));

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
        route_id: route.id.clone(),
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
    // scenario.directory_path = {route_dir}/Scenarios/{scenario_id}
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
