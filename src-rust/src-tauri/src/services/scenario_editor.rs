use crate::services::{consist_commands::ConsistCommand, vehicle_generator};
use anyhow::{Context, Result};
use quick_xml::{events::Event, Reader, Writer};
use std::{
    collections::HashMap,
    fs::File,
    io::{BufReader, BufWriter, Write as IoWrite},
    path::Path,
};

/// Applies consist edit commands to a scenario XML file in a single streaming pass,
/// writing the result to `output_path`.
///
/// For `ReplaceVehicles` commands the replacement vehicle XML is pre-generated before
/// the streaming pass begins so only one pass through the (potentially 500 MB) file
/// is required.
pub fn apply_edits(input_path: &Path, output_path: &Path, commands: &[ConsistCommand]) -> Result<()> {
    // Pre-compute replacement vehicle XMLs for every ReplaceVehicles command.
    let mut replacements: HashMap<String, Vec<String>> = HashMap::new();
    for cmd in commands {
        if let ConsistCommand::ReplaceVehicles { consist_id, entries } = cmd {
            let xmls: Vec<String> = entries.iter().map(vehicle_generator::generate_vehicle_xml).collect();
            replacements.insert(consist_id.clone(), xmls);
        }
    }

    let in_file = File::open(input_path)
        .with_context(|| format!("opening {}", input_path.display()))?;
    let out_file = File::create(output_path)
        .with_context(|| format!("creating {}", output_path.display()))?;

    let mut reader = Reader::from_reader(BufReader::new(in_file));
    // Preserve whitespace so the output is structurally identical to the input.
    reader.config_mut().trim_text(false);

    let mut writer = Writer::new(BufWriter::new(out_file));
    let mut state = EditorState::new(commands, &replacements);
    let mut buf = Vec::new();

    loop {
        let event = reader.read_event_into(&mut buf)?;

        // Phase 1 — inspect event, update state, collect any pre-content to emit.
        let (pre_bytes, should_write) = match &event {
            Event::Eof => break,
            Event::Start(e) => {
                let write = state.on_start(e);
                (Vec::new(), write)
            }
            Event::End(e) => {
                let pre = state.pre_end_bytes(e);
                let write = state.on_end(e);
                (pre, write)
            }
            _ => (Vec::new(), state.should_write()),
        };

        // Phase 2 — emit.
        if !pre_bytes.is_empty() {
            writer
                .get_mut()
                .write_all(&pre_bytes)
                .context("writing pre-end content")?;
        }
        if should_write {
            writer.write_event(event).context("writing event")?;
        }

        buf.clear();
    }

    writer.get_mut().flush().context("flushing output")?;
    Ok(())
}

// ── State machine ────────────────────────────────────────────────────────────

struct EditorState<'a> {
    commands: &'a [ConsistCommand],
    replacements: &'a HashMap<String, Vec<String>>,

    depth: usize,

    // The consist currently being traversed (if it has any relevant commands).
    current_consist_id: Option<String>,
    consist_depth: Option<usize>,

    // When Some(d): we are inside a consist at depth d that should be deleted.
    skip_consist_depth: Option<usize>,

    // RailVehicles section within the current consist.
    in_rail_vehicles: bool,
    rail_vehicles_depth: Option<usize>,
    vehicle_index: usize,
    // Depth of the cOwnedEntity currently being traversed (non-skip).
    current_vehicle_depth: Option<usize>,

    // When Some(d): we are inside a cOwnedEntity at depth d that should be skipped.
    skip_vehicle_depth: Option<usize>,

    // True when all vehicles in the current consist are being replaced.
    replacing: bool,
}

impl<'a> EditorState<'a> {
    fn new(commands: &'a [ConsistCommand], replacements: &'a HashMap<String, Vec<String>>) -> Self {
        Self {
            commands,
            replacements,
            depth: 0,
            current_consist_id: None,
            consist_depth: None,
            skip_consist_depth: None,
            in_rail_vehicles: false,
            rail_vehicles_depth: None,
            vehicle_index: 0,
            current_vehicle_depth: None,
            skip_vehicle_depth: None,
            replacing: false,
        }
    }

    fn should_write(&self) -> bool {
        self.skip_consist_depth.is_none() && self.skip_vehicle_depth.is_none()
    }

    /// Called for every `Start` event. Increments depth, updates state, returns
    /// `false` when the event should be dropped.
    fn on_start(&mut self, e: &quick_xml::events::BytesStart<'_>) -> bool {
        self.depth += 1;
        let name = name_bytes_str(e.name().as_ref());

        // While skipping a consist or vehicle, count depth but write nothing.
        if self.skip_consist_depth.is_some() || self.skip_vehicle_depth.is_some() {
            return false;
        }

        match name.as_str() {
            "cConsist" if self.consist_depth.is_none() => {
                let id = attr_str(e, b"d:id");

                if self.commands.iter().any(|c| {
                    matches!(c, ConsistCommand::DeleteConsist { consist_id } if consist_id == &id)
                }) {
                    self.skip_consist_depth = Some(self.depth);
                    return false;
                }

                let has_edits = self.commands.iter().any(|c| match c {
                    ConsistCommand::DeleteVehicle { consist_id, .. }
                    | ConsistCommand::ReplaceVehicles { consist_id, .. } => consist_id == &id,
                    _ => false,
                });

                if has_edits {
                    self.replacing = self
                        .commands
                        .iter()
                        .any(|c| matches!(c, ConsistCommand::ReplaceVehicles { consist_id, .. } if consist_id == &id));
                    self.current_consist_id = Some(id);
                    self.consist_depth = Some(self.depth);
                }
            }

            "RailVehicles"
                if self.consist_depth.is_some()
                    && !self.in_rail_vehicles
                    && self.skip_vehicle_depth.is_none() =>
            {
                self.in_rail_vehicles = true;
                self.rail_vehicles_depth = Some(self.depth);
                self.vehicle_index = 0;
            }

            "cOwnedEntity"
                if self.in_rail_vehicles
                    && self.current_vehicle_depth.is_none()
                    && self.skip_vehicle_depth.is_none() =>
            {
                // Only process direct children of <RailVehicles>.
                if self.depth != self.rail_vehicles_depth.map(|d| d + 1).unwrap_or(usize::MAX) {
                    return true;
                }

                if self.replacing {
                    // Replace mode: skip every existing vehicle.
                    self.skip_vehicle_depth = Some(self.depth);
                    return false;
                }

                let idx = self.vehicle_index;
                let cid = self.current_consist_id.as_deref().unwrap_or("");
                let should_delete = self.commands.iter().any(|c| {
                    matches!(c, ConsistCommand::DeleteVehicle { consist_id, vehicle_index }
                        if consist_id == cid && *vehicle_index == idx)
                });

                if should_delete {
                    self.skip_vehicle_depth = Some(self.depth);
                    return false;
                }

                self.current_vehicle_depth = Some(self.depth);
            }

            _ => {}
        }

        true
    }

    /// Returns raw bytes that should be emitted BEFORE the current `End` event
    /// (used to insert replacement vehicles before `</RailVehicles>`).
    fn pre_end_bytes(&self, e: &quick_xml::events::BytesEnd<'_>) -> Vec<u8> {
        if self.skip_consist_depth.is_some() || self.skip_vehicle_depth.is_some() {
            return Vec::new();
        }

        let name = name_bytes_str(e.name().as_ref());

        if name == "RailVehicles"
            && self.replacing
            && self.rail_vehicles_depth == Some(self.depth)
        {
            if let Some(id) = &self.current_consist_id {
                if let Some(xmls) = self.replacements.get(id) {
                    let mut out = String::new();
                    for xml in xmls {
                        out.push('\n');
                        out.push_str(xml);
                    }
                    out.push('\n');
                    return out.into_bytes();
                }
            }
        }

        Vec::new()
    }

    /// Called for every `End` event. Updates state and returns `false` when the
    /// event should be dropped.
    fn on_end(&mut self, e: &quick_xml::events::BytesEnd<'_>) -> bool {
        let name = name_bytes_str(e.name().as_ref());

        // Handle skip exit first, before any decrement.
        if let Some(skip_d) = self.skip_consist_depth {
            if name == "cConsist" && skip_d == self.depth {
                self.skip_consist_depth = None;
            }
            if self.depth > 0 {
                self.depth -= 1;
            }
            return false;
        }

        if let Some(skip_d) = self.skip_vehicle_depth {
            if name == "cOwnedEntity" && skip_d == self.depth {
                self.skip_vehicle_depth = None;
                // In delete mode (not replace), vehicle_index stays the same so subsequent
                // delete-by-index commands still resolve correctly against the shrunk list.
            }
            if self.depth > 0 {
                self.depth -= 1;
            }
            return false;
        }

        // Normal close handling.
        match name.as_str() {
            "cOwnedEntity" if self.current_vehicle_depth == Some(self.depth) => {
                self.current_vehicle_depth = None;
                self.vehicle_index += 1;
            }
            "RailVehicles" if self.rail_vehicles_depth == Some(self.depth) => {
                self.in_rail_vehicles = false;
                self.rail_vehicles_depth = None;
                self.replacing = false;
            }
            "cConsist" if self.consist_depth == Some(self.depth) => {
                self.current_consist_id = None;
                self.consist_depth = None;
                self.vehicle_index = 0;
            }
            _ => {}
        }

        if self.depth > 0 {
            self.depth -= 1;
        }

        true
    }
}

// ── Helpers ──────────────────────────────────────────────────────────────────

fn name_bytes_str(bytes: &[u8]) -> String {
    std::str::from_utf8(bytes).unwrap_or("").to_owned()
}

fn attr_str(e: &quick_xml::events::BytesStart<'_>, key: &[u8]) -> String {
    e.attributes()
        .flatten()
        .find(|a| a.key.as_ref() == key)
        .and_then(|a| String::from_utf8(a.value.to_vec()).ok())
        .unwrap_or_default()
}
