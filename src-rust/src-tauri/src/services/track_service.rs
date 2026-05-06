use crate::{
    commands::tracks::{TrackBlueprint, TrackReplacement},
    models::Route,
    platform::app_cache_dir,
    serz,
};
use anyhow::{Context, Result};
use quick_xml::{events::Event, Reader};
use std::{
    collections::HashSet,
    io::BufReader,
    path::{Path, PathBuf},
};

// ── Public API ───────────────────────────────────────────────────────────────

/// Converts Tracks.bin → XML (cached), then streaming-parses unique track blueprints.
pub async fn get_tracks(route: &Route) -> Result<Vec<TrackBlueprint>> {
    let bin_path = route.tracks_binary_path();
    anyhow::ensure!(
        bin_path.exists(),
        "Tracks.bin not found at {}",
        bin_path.display()
    );

    let xml_path = serz::convert_to_xml(&bin_path, false).await?;
    tokio::task::spawn_blocking(move || parse_tracks(&xml_path)).await?
}

/// Converts Tracks.bin → XML, applies blueprint replacements, converts back, updates RouteProperties.xml.
pub async fn replace_tracks(route: &Route, replacements: &[TrackReplacement]) -> Result<()> {
    if replacements.iter().all(|r| r.to.is_none()) {
        return Ok(());
    }

    let bin_path = route.tracks_binary_path();
    anyhow::ensure!(
        bin_path.exists(),
        "Tracks.bin not found at {}",
        bin_path.display()
    );

    let xml_path = serz::convert_to_xml(&bin_path, false).await?;

    // Backup the original binary before any write.
    backup_tracks_bin(&bin_path).await?;

    // Apply replacements to a temp file, then atomically replace the cached XML.
    let temp_path = xml_path.with_extension("edit.xml");
    {
        let xml_c = xml_path.clone();
        let temp_c = temp_path.clone();
        let repls = replacements.to_vec();
        tokio::task::spawn_blocking(move || apply_track_replacements(&xml_c, &temp_c, &repls))
            .await??;
    }
    tokio::fs::rename(&temp_path, &xml_path).await?;

    // Convert modified XML → Tracks.bin.
    serz::convert_to_bin(&xml_path, &bin_path).await?;

    // Update RouteProperties.xml blueprint set collections.
    let props_path = route.route_properties_path();
    if props_path.exists() {
        let props_xml = tokio::fs::read_to_string(&props_path).await?;
        let updated = update_route_properties(&props_xml, replacements)?;
        tokio::fs::write(&props_path, updated).await?;
    }

    Ok(())
}

// ── Streaming track parser ───────────────────────────────────────────────────

/// Streaming pull-parser for Tracks.bin.xml.
///
/// Extracts unique Provider/Product/BlueprintID triples from
/// `Network-cSectionGenericProperties > BlueprintID > iBlueprintLibrary-cAbsoluteBlueprintID`.
pub fn parse_tracks(xml_path: &Path) -> Result<Vec<TrackBlueprint>> {
    let file = std::fs::File::open(xml_path)
        .with_context(|| format!("opening tracks xml: {}", xml_path.display()))?;
    let mut reader = Reader::from_reader(BufReader::new(file));
    reader.config_mut().trim_text(true);

    let mut blueprints: Vec<TrackBlueprint> = Vec::new();
    let mut seen: HashSet<(String, String, String)> = HashSet::new();
    let mut buf = Vec::new();

    let mut depth: usize = 0;
    let mut in_section_props = false;
    let mut in_outer_bp = false;
    let mut in_abs_bp = false;
    let mut in_bp_set = false;

    let mut cur_blueprint_id = String::new();
    let mut cur_provider = String::new();
    let mut cur_product = String::new();
    let mut capture: Option<&'static str> = None;

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(ref e)) => {
                depth += 1;
                let name = name_str(e.name().as_ref());
                match name.as_str() {
                    "Network-cSectionGenericProperties" if !in_section_props => {
                        in_section_props = true;
                    }
                    "BlueprintID" if in_section_props && !in_outer_bp && !in_abs_bp => {
                        in_outer_bp = true;
                    }
                    "iBlueprintLibrary-cAbsoluteBlueprintID" if in_outer_bp && !in_abs_bp => {
                        in_abs_bp = true;
                        cur_blueprint_id.clear();
                        cur_provider.clear();
                        cur_product.clear();
                    }
                    "BlueprintID" if in_abs_bp && !in_bp_set => {
                        capture = Some("blueprint_id");
                    }
                    "iBlueprintLibrary-cBlueprintSetID" if in_abs_bp => {
                        in_bp_set = true;
                    }
                    "Provider" if in_bp_set => {
                        capture = Some("provider");
                    }
                    "Product" if in_bp_set => {
                        capture = Some("product");
                    }
                    _ => {}
                }
            }

            Ok(Event::Text(ref e)) => {
                if let Some(field) = capture.take() {
                    let text = e.unescape().unwrap_or_default();
                    match field {
                        "blueprint_id" => cur_blueprint_id = text.into_owned(),
                        "provider" => cur_provider = text.into_owned(),
                        "product" => cur_product = text.into_owned(),
                        _ => {}
                    }
                }
            }

            Ok(Event::End(ref e)) => {
                let name = name_str(e.name().as_ref());
                match name.as_str() {
                    "iBlueprintLibrary-cAbsoluteBlueprintID" if in_abs_bp => {
                        if !cur_provider.is_empty() || !cur_blueprint_id.is_empty() {
                            let key = (
                                cur_provider.clone(),
                                cur_product.clone(),
                                cur_blueprint_id.clone(),
                            );
                            if seen.insert(key) {
                                blueprints.push(TrackBlueprint {
                                    provider: cur_provider.clone(),
                                    product: cur_product.clone(),
                                    blueprint_id: cur_blueprint_id.clone(),
                                });
                            }
                        }
                        in_abs_bp = false;
                        in_bp_set = false;
                    }
                    "iBlueprintLibrary-cBlueprintSetID" if in_bp_set => {
                        in_bp_set = false;
                    }
                    "BlueprintID" if in_outer_bp && !in_abs_bp => {
                        in_outer_bp = false;
                    }
                    "Network-cSectionGenericProperties" if in_section_props => {
                        in_section_props = false;
                        in_outer_bp = false;
                    }
                    _ => {}
                }
                if depth > 0 {
                    depth -= 1;
                }
            }

            Ok(Event::Eof) => break,
            Err(e) => {
                tracing::warn!("tracks xml parse error at depth {depth}: {e}");
                break;
            }
            _ => {}
        }
        buf.clear();
    }

    blueprints.sort_by(|a, b| a.provider.cmp(&b.provider).then(a.product.cmp(&b.product)));
    Ok(blueprints)
}

// ── Streaming track replacement editor ──────────────────────────────────────

/// Single-pass streaming replacement for Tracks.bin.xml.
///
/// The file is read into memory because:
/// 1. We need byte positions to splice replacements (no seek-back in a forward stream).
/// 2. Tracks.bin.xml is typically 5–50 MB — well within available RAM.
///
/// For each `iBlueprintLibrary-cAbsoluteBlueprintID` inside
/// `Network-cSectionGenericProperties > BlueprintID`, if the (provider, product, blueprintId)
/// triple matches a replacement entry the element is rewritten with the new values.
pub fn apply_track_replacements(
    input_path: &Path,
    output_path: &Path,
    replacements: &[TrackReplacement],
) -> Result<()> {
    // Build a lookup from old → new blueprint.
    type Key = (String, String, String);
    let map: std::collections::HashMap<Key, &TrackBlueprint> = replacements
        .iter()
        .filter_map(|r| {
            r.to.as_ref().map(|to| {
                let k = (
                    r.from.provider.clone(),
                    r.from.product.clone(),
                    r.from.blueprint_id.clone(),
                );
                (k, to)
            })
        })
        .collect();

    if map.is_empty() {
        // Nothing to replace — copy file verbatim.
        std::fs::copy(input_path, output_path)?;
        return Ok(());
    }

    let content =
        std::fs::read(input_path).with_context(|| format!("reading {}", input_path.display()))?;
    let xml_str =
        std::str::from_utf8(&content).with_context(|| "Tracks.bin.xml is not valid UTF-8")?;

    // Collect (start_byte, end_byte, new_xml) for every element that needs replacement.
    // Positions are byte offsets into `xml_str`.
    let mut patches: Vec<(usize, usize, String)> = Vec::new();

    let mut reader = Reader::from_str(xml_str);
    reader.config_mut().trim_text(false);
    let mut buf = Vec::new();

    let mut depth: usize = 0;
    let mut in_section_props = false;
    let mut in_outer_bp = false;
    let mut in_abs_bp = false;
    let mut in_bp_set = false;

    // Byte offset of the `<iBlueprintLibrary-cAbsoluteBlueprintID` opening tag.
    let mut abs_bp_start: Option<usize> = None;

    let mut cur_blueprint_id = String::new();
    let mut cur_provider = String::new();
    let mut cur_product = String::new();
    let mut capture: Option<&'static str> = None;

    loop {
        // Record byte position BEFORE reading the next event.
        let pos_before = reader.buffer_position() as usize;
        let event = reader.read_event_into(&mut buf)?;
        let pos_after = reader.buffer_position() as usize;

        match &event {
            Event::Start(e) => {
                depth += 1;
                let name = name_str(e.name().as_ref());
                match name.as_str() {
                    "Network-cSectionGenericProperties" if !in_section_props => {
                        in_section_props = true;
                    }
                    "BlueprintID" if in_section_props && !in_outer_bp && !in_abs_bp => {
                        in_outer_bp = true;
                    }
                    "iBlueprintLibrary-cAbsoluteBlueprintID" if in_outer_bp && !in_abs_bp => {
                        in_abs_bp = true;
                        abs_bp_start = Some(pos_before);
                        cur_blueprint_id.clear();
                        cur_provider.clear();
                        cur_product.clear();
                    }
                    "BlueprintID" if in_abs_bp && !in_bp_set => {
                        capture = Some("blueprint_id");
                    }
                    "iBlueprintLibrary-cBlueprintSetID" if in_abs_bp => {
                        in_bp_set = true;
                    }
                    "Provider" if in_bp_set => {
                        capture = Some("provider");
                    }
                    "Product" if in_bp_set => {
                        capture = Some("product");
                    }
                    _ => {}
                }
            }

            Event::Text(e) => {
                if let Some(field) = capture.take() {
                    let text = e.unescape().unwrap_or_default();
                    match field {
                        "blueprint_id" => cur_blueprint_id = text.into_owned(),
                        "provider" => cur_provider = text.into_owned(),
                        "product" => cur_product = text.into_owned(),
                        _ => {}
                    }
                }
            }

            Event::End(e) => {
                let name = name_str(e.name().as_ref());
                match name.as_str() {
                    "iBlueprintLibrary-cAbsoluteBlueprintID" if in_abs_bp => {
                        let key: Key = (
                            cur_provider.clone(),
                            cur_product.clone(),
                            cur_blueprint_id.clone(),
                        );
                        if let Some(repl) = map.get(&key) {
                            let start = abs_bp_start.unwrap_or(pos_before);
                            patches.push((start, pos_after, generate_abs_bp_xml(repl)));
                        }
                        in_abs_bp = false;
                        in_bp_set = false;
                        abs_bp_start = None;
                    }
                    "iBlueprintLibrary-cBlueprintSetID" if in_bp_set => {
                        in_bp_set = false;
                    }
                    "BlueprintID" if in_outer_bp && !in_abs_bp => {
                        in_outer_bp = false;
                    }
                    "Network-cSectionGenericProperties" if in_section_props => {
                        in_section_props = false;
                        in_outer_bp = false;
                    }
                    _ => {}
                }
                if depth > 0 {
                    depth -= 1;
                }
            }

            Event::Eof => break,
            _ => {}
        }
        buf.clear();
    }

    if patches.is_empty() {
        std::fs::copy(input_path, output_path)?;
        return Ok(());
    }

    // Apply patches in order (non-overlapping, sorted by position).
    patches.sort_by_key(|(start, _, _)| *start);

    let mut result = Vec::with_capacity(content.len());
    let mut cursor = 0usize;
    for (start, end, new_xml) in &patches {
        result.extend_from_slice(&content[cursor..*start]);
        result.extend_from_slice(new_xml.as_bytes());
        cursor = *end;
    }
    result.extend_from_slice(&content[cursor..]);

    std::fs::write(output_path, &result)
        .with_context(|| format!("writing {}", output_path.display()))?;
    Ok(())
}

// ── RouteProperties.xml blueprint set update ─────────────────────────────────

/// Ensures every replacement's new provider/product is listed in `RBlueprintSetPreLoad`
/// and `RequiredSet` within the route properties XML.
pub fn update_route_properties(xml: &str, replacements: &[TrackReplacement]) -> Result<String> {
    let mut result = xml.to_string();

    for replacement in replacements {
        let to = match &replacement.to {
            Some(t) => t,
            None => continue,
        };

        for collection in &["RBlueprintSetPreLoad", "RequiredSet"] {
            result = ensure_blueprint_in_collection(&result, collection, to)?;
        }
    }

    Ok(result)
}

/// Adds a `<iBlueprintLibrary-cBlueprintSetID>` entry to `collection` if the
/// provider/product pair is not already present.
fn ensure_blueprint_in_collection(
    xml: &str,
    collection: &str,
    blueprint: &TrackBlueprint,
) -> Result<String> {
    // Check if already present: look for Provider inside the collection element.
    let open_col = format!("<{collection}>");
    let close_col = format!("</{collection}>");

    let col_start = match xml.find(&open_col) {
        Some(pos) => pos,
        None => return Ok(xml.to_string()), // collection element absent — skip
    };

    let col_end = match xml[col_start..].find(&close_col) {
        Some(off) => col_start + off + close_col.len(),
        None => return Ok(xml.to_string()),
    };

    let col_content = &xml[col_start..col_end];

    // Naïve but sufficient presence check: look for the Provider value inside the slice.
    if col_content.contains(&format!("<Provider>{}</Provider>", blueprint.provider))
        && col_content.contains(&format!("<Product>{}</Product>", blueprint.product))
    {
        return Ok(xml.to_string()); // already present
    }

    // Insert before the closing tag.
    let insert_pos = col_start + col_content.rfind(&close_col).unwrap();
    let entry = format!(
        "\n\t\t<iBlueprintLibrary-cBlueprintSetID>\n\t\t\t<Provider>{}</Provider>\n\t\t\t<Product>{}</Product>\n\t\t</iBlueprintLibrary-cBlueprintSetID>",
        blueprint.provider, blueprint.product,
    );

    let mut result = xml.to_string();
    result.insert_str(insert_pos, &entry);
    Ok(result)
}

// ── Backup ────────────────────────────────────────────────────────────────────

async fn backup_tracks_bin(bin_path: &Path) -> Result<PathBuf> {
    let route_id = bin_path
        .ancestors()
        .nth(2) // Networks -> route_dir
        .and_then(|p| p.file_name())
        .and_then(|n| n.to_str())
        .unwrap_or("unknown");

    let backup_dir = app_cache_dir()
        .unwrap_or_else(|_| PathBuf::from("."))
        .join("backups")
        .join("tracks")
        .join(route_id);

    tokio::fs::create_dir_all(&backup_dir).await?;

    let ts = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);

    let dest = backup_dir.join(format!("Tracks.{ts}.bin.bak"));
    tokio::fs::copy(bin_path, &dest).await?;
    tracing::info!("backed up {} → {}", bin_path.display(), dest.display());
    Ok(dest)
}

// ── Helpers ────────────────────────────────────────────────────────────────────

fn name_str(bytes: &[u8]) -> String {
    std::str::from_utf8(bytes).unwrap_or("").to_owned()
}

fn generate_abs_bp_xml(bp: &TrackBlueprint) -> String {
    format!(
        "<iBlueprintLibrary-cAbsoluteBlueprintID>\
\n\t\t\t\t\t<BlueprintID>{}</BlueprintID>\
\n\t\t\t\t\t<iBlueprintLibrary-cBlueprintSetID>\
\n\t\t\t\t\t\t<Provider>{}</Provider>\
\n\t\t\t\t\t\t<Product>{}</Product>\
\n\t\t\t\t\t</iBlueprintLibrary-cBlueprintSetID>\
\n\t\t\t\t</iBlueprintLibrary-cAbsoluteBlueprintID>",
        bp.blueprint_id, bp.provider, bp.product
    )
}
