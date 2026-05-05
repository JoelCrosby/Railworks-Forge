use crate::{
    models::Scenario,
    platform::{app_config_dir},
    serz,
    services::{
        consist_commands::{ConsistCommand, SavedConsist},
        scenario_editor,
        scenario_parser,
    },
};
use anyhow::Result;
use std::path::{Path, PathBuf};

/// Backs up `Scenario.bin` to the per-scenario backup directory before any modification.
pub async fn backup_scenario_bin(bin_path: &Path) -> Result<PathBuf> {
    let scenario_id = bin_path
        .parent()
        .and_then(|p| p.file_name())
        .and_then(|n| n.to_str())
        .unwrap_or("unknown");

    let backup_dir = app_config_dir()
        .unwrap_or_else(|_| PathBuf::from("."))
        .join("backups")
        .join("scenarios")
        .join(scenario_id);

    tokio::fs::create_dir_all(&backup_dir).await?;

    let ts = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);

    let backup_path = backup_dir.join(format!("Scenario.{ts}.bin.bak"));
    tokio::fs::copy(bin_path, &backup_path).await?;
    tracing::info!("backed up {} → {}", bin_path.display(), backup_path.display());
    Ok(backup_path)
}

/// Applies consist commands to a scenario, writing back the modified `Scenario.bin`.
///
/// Flow:
/// 1. Resolve + serz-convert `Scenario.bin` → `.xml` (cached)
/// 2. Backup the `.bin`
/// 3. Apply edits via streaming XML editor
/// 4. Run serz to convert modified `.xml` → `.bin`
/// 5. Re-parse consists and return updated `Scenario`
pub async fn apply_edits(
    scenario: Scenario,
    bin_path: PathBuf,
    commands: Vec<ConsistCommand>,
) -> Result<Scenario> {
    // Convert to XML (or use cache hit).
    let xml_path = serz::convert_to_xml(&bin_path, false).await?;

    // Create a backup before any destructive write.
    backup_scenario_bin(&bin_path).await?;

    // Stream edits into a temp file, then atomically replace the cached XML.
    let temp_xml_path = xml_path.with_extension("edit.xml");
    {
        let xml_path_c = xml_path.clone();
        let temp_c = temp_xml_path.clone();
        let cmds_c = commands.clone();
        tokio::task::spawn_blocking(move || scenario_editor::apply_edits(&xml_path_c, &temp_c, &cmds_c))
            .await??;
    }
    tokio::fs::rename(&temp_xml_path, &xml_path).await?;

    // Convert the modified XML back to .bin (force=true since we just changed it).
    serz::convert_to_bin(&xml_path).await?;

    // Re-parse consists from the updated XML.
    let xml_path_c = xml_path.clone();
    let consists = tokio::task::spawn_blocking(move || scenario_parser::parse_consists(&xml_path_c))
        .await??;

    tracing::info!(
        "scenario {}: {} consists after edit",
        scenario.id,
        consists.len()
    );

    Ok(Scenario { consists, ..scenario })
}

// ── Saved consist templates ──────────────────────────────────────────────────

/// Persists a consist template to `~/.config/railworks-forge/consists.json`.
pub fn save_consist_template(consist: SavedConsist) -> Result<()> {
    let mut templates = load_consist_templates()?;
    templates.retain(|t: &SavedConsist| t.name != consist.name);
    templates.push(consist);
    write_templates(&templates)
}

/// Removes a saved consist template by name.
pub fn delete_consist_template(name: &str) -> Result<()> {
    let mut templates = load_consist_templates()?;
    templates.retain(|t| t.name != name);
    write_templates(&templates)
}

/// Returns all saved consist templates.
pub fn load_consist_templates() -> Result<Vec<SavedConsist>> {
    let path = templates_path()?;
    if !path.exists() {
        return Ok(Vec::new());
    }
    let json = std::fs::read_to_string(&path)?;
    Ok(serde_json::from_str(&json).unwrap_or_default())
}

fn write_templates(templates: &[SavedConsist]) -> Result<()> {
    let path = templates_path()?;
    if let Some(parent) = path.parent() {
        std::fs::create_dir_all(parent)?;
    }
    let json = serde_json::to_string_pretty(templates)?;
    std::fs::write(&path, json)?;
    Ok(())
}

fn templates_path() -> Result<PathBuf> {
    Ok(app_config_dir()?.join("consists.json"))
}
