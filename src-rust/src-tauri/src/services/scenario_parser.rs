use crate::models::{
    blueprint::{AcquisitionState, Blueprint},
    consist::{Consist, ConsistAcquisitionState, LocoClass},
    vehicle::{BlueprintType, VehicleBlueprint},
};
use anyhow::{Context, Result};
use quick_xml::{events::Event, Reader};
use std::{io::BufReader, path::Path};

/// Streaming state-machine parser for Scenario.bin.xml.
///
/// The document can exceed 500 MB; this parser uses quick-xml's pull API so it
/// never holds more than one consist subtree in memory at a time.
pub fn parse_consists(path: &Path) -> Result<Vec<Consist>> {
    let _profile = crate::services::profiling::ProfileSpan::new("parse_scenario_consists");
    let file = std::fs::File::open(path)
        .with_context(|| format!("opening scenario xml: {}", path.display()))?;
    let mut reader = Reader::from_reader(BufReader::new(file));
    reader.config_mut().trim_text(true);

    let mut consists: Vec<Consist> = Vec::new();
    let mut buf = Vec::new();

    // Current nesting depth (incremented on Start, decremented after End is handled).
    let mut depth: usize = 0;
    // Depth at which <cConsist> was opened; None when we are outside any consist.
    let mut consist_at: Option<usize> = None;
    // Depth at which <cOwnedEntity> was opened; None when outside any vehicle.
    let mut vehicle_at: Option<usize> = None;

    // ── Consist-level accumulators ─────────────────────────────────────────
    let mut cb_id = String::new();
    let mut cb_service_name = String::new();
    let mut cb_service_id = String::new();
    let mut cb_player_driver = false;
    let mut cb_vehicles: Vec<VehicleBlueprint> = Vec::new();
    let mut cb_loco_classes: Vec<String> = Vec::new();
    let mut cb_vehicle_idx: usize = 0;

    // ── Vehicle-level accumulators ─────────────────────────────────────────
    let mut vb_name = String::new();
    let mut vb_unique_number = String::new();
    let mut vb_loco_class = String::new();
    let mut vb_flipped = false;
    let mut vb_blueprint_id = String::new();
    let mut vb_provider = String::new();
    let mut vb_product = String::new();
    let mut vb_component_type = String::new();

    // ── Context flags ──────────────────────────────────────────────────────
    // Within consist (outside any vehicle):
    let mut in_driver = false;
    let mut in_service_name = false;
    let mut in_localised = false; // inside Localisation-cUserLocalisedString
    let mut in_rail_vehicles = false;

    // Within vehicle:
    // Outer <BlueprintID> wrapper (contains iBlueprintLibrary-cAbsoluteBlueprintID)
    let mut in_veh_bp_outer = false;
    // <iBlueprintLibrary-cAbsoluteBlueprintID> (contains inner BlueprintID text + cBlueprintSetID)
    let mut in_veh_abs_bp = false;
    // <iBlueprintLibrary-cBlueprintSetID> (contains Provider + Product)
    let mut in_veh_bp_set = false;
    // <Component> — first Start child determines vehicle type
    let mut in_veh_component = false;
    let mut component_first_child = false;

    // Name of the field whose text we want on the next Text event.
    let mut capture: Option<&'static str> = None;

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(ref e)) => {
                depth += 1;
                let name = name_str(e.name().as_ref());

                match name.as_str() {
                    // ── Consist boundary ──────────────────────────────────
                    "cConsist" if consist_at.is_none() => {
                        consist_at = Some(depth);
                        cb_id = attr(e, b"d:id");
                        cb_service_name.clear();
                        cb_service_id.clear();
                        cb_player_driver = false;
                        cb_vehicles.clear();
                        cb_loco_classes.clear();
                        cb_vehicle_idx = 0;
                        in_driver = false;
                        in_service_name = false;
                        in_localised = false;
                        in_rail_vehicles = false;
                    }

                    // ── Driver section (service name, player flag) ─────────
                    "Driver" if consist_at.is_some() && vehicle_at.is_none() => {
                        in_driver = true;
                    }
                    "PlayerDriver" if in_driver => {
                        capture = Some("player_driver");
                    }
                    "ServiceName" if in_driver => {
                        in_service_name = true;
                    }
                    "Localisation-cUserLocalisedString" if in_service_name => {
                        in_localised = true;
                    }
                    "English" if in_localised => {
                        capture = Some("service_name");
                    }
                    // Key appears inside Localisation-cUserLocalisedString as the GUID
                    "Key" if in_service_name => {
                        capture = Some("service_id");
                    }

                    // ── Vehicle list ───────────────────────────────────────
                    "RailVehicles" if consist_at.is_some() && !in_driver => {
                        in_rail_vehicles = true;
                    }

                    // ── Vehicle boundary ───────────────────────────────────
                    "cOwnedEntity" if in_rail_vehicles && vehicle_at.is_none() => {
                        vehicle_at = Some(depth);
                        vb_name.clear();
                        vb_unique_number.clear();
                        vb_loco_class.clear();
                        vb_flipped = false;
                        vb_blueprint_id.clear();
                        vb_provider.clear();
                        vb_product.clear();
                        vb_component_type.clear();
                        in_veh_bp_outer = false;
                        in_veh_abs_bp = false;
                        in_veh_bp_set = false;
                        in_veh_component = false;
                        component_first_child = false;
                    }

                    // ── Direct vehicle fields (depth == vehicle_at + 1) ───
                    "Name" if vehicle_at.is_some() && !in_veh_bp_outer && !in_veh_component => {
                        capture = Some("veh_name");
                    }
                    "UniqueNumber"
                        if vehicle_at.is_some() && !in_veh_bp_outer && !in_veh_component =>
                    {
                        capture = Some("veh_unique");
                    }
                    "LocoClass"
                        if vehicle_at.is_some() && !in_veh_bp_outer && !in_veh_component =>
                    {
                        capture = Some("veh_loco_class");
                    }
                    "Flipped" if vehicle_at.is_some() && !in_veh_bp_outer && !in_veh_component => {
                        capture = Some("veh_flipped");
                    }

                    // ── Blueprint nesting ──────────────────────────────────
                    // Outer <BlueprintID> wrapper of the vehicle entity
                    "BlueprintID" if vehicle_at.is_some() && !in_veh_bp_outer && !in_veh_abs_bp => {
                        in_veh_bp_outer = true;
                    }
                    "iBlueprintLibrary-cAbsoluteBlueprintID" if in_veh_bp_outer => {
                        in_veh_abs_bp = true;
                    }
                    // Inner <BlueprintID> text (the actual blueprint file path)
                    "BlueprintID" if in_veh_abs_bp && !in_veh_bp_set => {
                        capture = Some("veh_blueprint_id");
                    }
                    "iBlueprintLibrary-cBlueprintSetID" if in_veh_abs_bp => {
                        in_veh_bp_set = true;
                    }
                    "Provider" if in_veh_bp_set => {
                        capture = Some("veh_provider");
                    }
                    "Product" if in_veh_bp_set => {
                        capture = Some("veh_product");
                    }

                    // ── Component type detection ───────────────────────────
                    "Component" if vehicle_at.is_some() && !in_veh_bp_outer => {
                        in_veh_component = true;
                        component_first_child = true;
                    }
                    // First child element name inside <Component> is the vehicle type
                    _ if in_veh_component && component_first_child => {
                        vb_component_type = name.clone();
                        component_first_child = false;
                    }

                    _ => {}
                }
            }

            Ok(Event::Text(ref e)) => {
                if let Some(field) = capture.take() {
                    let text = e.unescape().unwrap_or_default();
                    match field {
                        "player_driver" => cb_player_driver = text.trim() == "1",
                        "service_name" => cb_service_name = text.into_owned(),
                        "service_id" => {
                            // Only store if we don't already have a value (Key appears
                            // both inside and sometimes outside the localised wrapper).
                            if cb_service_id.is_empty() {
                                cb_service_id = text.into_owned();
                            }
                        }
                        "veh_name" => vb_name = text.into_owned(),
                        "veh_unique" => vb_unique_number = text.into_owned(),
                        "veh_loco_class" => vb_loco_class = text.into_owned(),
                        "veh_flipped" => vb_flipped = text.trim() == "1",
                        "veh_blueprint_id" => vb_blueprint_id = text.into_owned(),
                        "veh_provider" => vb_provider = text.into_owned(),
                        "veh_product" => vb_product = text.into_owned(),
                        _ => {}
                    }
                }
            }

            Ok(Event::End(ref e)) => {
                let name = name_str(e.name().as_ref());

                // ── Vehicle finalization ───────────────────────────────────
                if name == "cOwnedEntity" && vehicle_at == Some(depth) {
                    let blueprint = Blueprint::new(&vb_provider, &vb_product, &vb_blueprint_id);
                    let blueprint_type = BlueprintType::from_str(&vb_component_type);
                    cb_vehicles.push(VehicleBlueprint {
                        blueprint,
                        name: vb_name.clone(),
                        unique_number: vb_unique_number.clone(),
                        blueprint_type,
                        flipped: vb_flipped,
                        index: cb_vehicle_idx,
                    });
                    cb_loco_classes.push(vb_loco_class.clone());
                    cb_vehicle_idx += 1;
                    vehicle_at = None;
                    in_veh_bp_outer = false;
                    in_veh_abs_bp = false;
                    in_veh_bp_set = false;
                    in_veh_component = false;
                }
                // ── Consist finalization ───────────────────────────────────
                else if name == "cConsist" && consist_at == Some(depth) {
                    if let Some(consist) = build_consist(
                        &cb_id,
                        &cb_service_name,
                        &cb_service_id,
                        cb_player_driver,
                        &cb_vehicles,
                        &cb_loco_classes,
                    ) {
                        consists.push(consist);
                    }
                    consist_at = None;
                    in_driver = false;
                    in_service_name = false;
                    in_localised = false;
                    in_rail_vehicles = false;
                }
                // ── Context flag clearing ──────────────────────────────────
                else {
                    match name.as_str() {
                        "Driver" if in_driver => {
                            in_driver = false;
                            in_service_name = false;
                            in_localised = false;
                        }
                        "ServiceName" if in_service_name => {
                            in_service_name = false;
                            in_localised = false;
                        }
                        "Localisation-cUserLocalisedString" if in_localised => {
                            in_localised = false;
                        }
                        "RailVehicles" if in_rail_vehicles => {
                            in_rail_vehicles = false;
                        }
                        // Blueprint nesting — clear from innermost out
                        "iBlueprintLibrary-cBlueprintSetID" if in_veh_bp_set => {
                            in_veh_bp_set = false;
                        }
                        "iBlueprintLibrary-cAbsoluteBlueprintID" if in_veh_abs_bp => {
                            in_veh_abs_bp = false;
                            in_veh_bp_set = false;
                        }
                        // Outer BlueprintID close — only when not inside abs blueprint
                        "BlueprintID" if in_veh_bp_outer && !in_veh_abs_bp => {
                            in_veh_bp_outer = false;
                        }
                        "Component" if in_veh_component => {
                            in_veh_component = false;
                        }
                        _ => {}
                    }
                }

                if depth > 0 {
                    depth -= 1;
                }
            }

            Ok(Event::Eof) => break,

            Err(err) => {
                tracing::warn!("scenario xml parse error at depth {depth}: {err}");
                break;
            }

            _ => {}
        }
        buf.clear();
    }

    Ok(consists)
}

fn build_consist(
    id: &str,
    service_name: &str,
    service_id: &str,
    player_driver: bool,
    vehicles: &[VehicleBlueprint],
    loco_classes: &[String],
) -> Option<Consist> {
    let lead_idx = lead_vehicle_idx(vehicles)?;
    let lead = &vehicles[lead_idx];

    let loco_class_str = loco_classes.get(lead_idx).map(String::as_str).unwrap_or("");
    let loco_class = LocoClass::from_str(loco_class_str);
    let acquisition_state = acquisition_state(vehicles);

    let loco_author =
        (!lead.blueprint.provider.is_empty()).then(|| lead.blueprint.provider.clone());

    Some(Consist {
        id: id.to_string(),
        locomotive_name: lead.name.clone(),
        service_name: service_name.to_string(),
        service_id: service_id.to_string(),
        loco_author,
        loco_class,
        player_driver,
        blueprint: lead.blueprint.clone(),
        vehicles: vehicles.to_vec(),
        acquisition_state,
        image_data_url: None,
    })
}

fn lead_vehicle_idx(vehicles: &[VehicleBlueprint]) -> Option<usize> {
    if vehicles.is_empty() {
        return None;
    }
    if vehicles[0].blueprint_type == BlueprintType::Engine {
        return Some(0);
    }
    if vehicles[vehicles.len() - 1].blueprint_type == BlueprintType::Engine {
        return Some(vehicles.len() - 1);
    }
    Some(0)
}

fn acquisition_state(vehicles: &[VehicleBlueprint]) -> ConsistAcquisitionState {
    let found = vehicles
        .iter()
        .filter(|v| v.blueprint.acquisition_state == AcquisitionState::Found)
        .count();
    if found == vehicles.len() {
        ConsistAcquisitionState::Found
    } else if found > 0 {
        ConsistAcquisitionState::Partial
    } else {
        ConsistAcquisitionState::Missing
    }
}

fn name_str(bytes: &[u8]) -> String {
    std::str::from_utf8(bytes).unwrap_or("").to_owned()
}

fn attr(e: &quick_xml::events::BytesStart<'_>, key: &[u8]) -> String {
    e.attributes()
        .flatten()
        .find(|a| a.key.as_ref() == key)
        .and_then(|a| String::from_utf8(a.value.to_vec()).ok())
        .unwrap_or_default()
}
