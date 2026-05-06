use crate::services::{consist_commands::VehicleEntry, vehicle_templates};
use std::sync::atomic::{AtomicU64, Ordering};

static ENTITY_COUNTER: AtomicU64 = AtomicU64::new(100_000_000);

fn next_entity_id() -> String {
    // Mirrors the C# approach: random 9-digit integer for d:id attributes.
    let id = ENTITY_COUNTER.fetch_add(1, Ordering::Relaxed);
    format!("{}", (id % 900_000_000) + 100_000_000)
}

/// Generates a complete `<cOwnedEntity>` XML string for insertion into a scenario.
///
/// Uses the embedded vehicle type template (Engine/Wagon/Tender) and fills in the
/// blueprint provider, product, blueprint path, and flipped flag.
/// The namespace declaration is stripped since the enclosing document already declares it.
pub fn generate_vehicle_xml(entry: &VehicleEntry) -> String {
    let blueprint_type = entry.resolved_type();
    let template = vehicle_templates::get_template(&blueprint_type);

    // Strip the xmlns:d declaration — the parent document already has it.
    let mut xml = template.replace(r#" xmlns:d="http://www.kuju.com/TnT/2003/Delta""#, "");

    // Assign IDs to all empty d:id="" placeholders.
    while xml.contains(r#"d:id="""#) {
        xml = xml.replacen(r#"d:id="""#, &format!(r#"d:id="{}""#, next_entity_id()), 1);
    }

    // Update blueprint fields inside <BlueprintID>.
    xml = set_first_element_text(&xml, "Provider", &entry.provider);
    xml = set_first_element_text(&xml, "Product", &entry.product);
    xml = set_tagged_element_text(
        &xml,
        r#"<BlueprintID d:type="cDeltaString">"#,
        "</BlueprintID>",
        &entry.blueprint_id,
    );

    // Update <Flipped> inside the component section.
    let flipped_val = if entry.flipped { "1" } else { "0" };
    xml = set_first_element_text(&xml, "Flipped", flipped_val);

    xml
}

/// Replaces the text content of the FIRST `<{tag_name}...>...</{tag_name}>` occurrence.
/// Handles tags with attributes (e.g. `<Provider d:type="cDeltaString">`).
fn set_first_element_text(xml: &str, tag_name: &str, new_value: &str) -> String {
    let open_prefix = format!("<{tag_name}");
    let close_tag = format!("</{tag_name}>");

    let Some(open_start) = xml.find(&open_prefix) else {
        return xml.to_owned();
    };
    let Some(rel_end) = xml[open_start..].find('>') else {
        return xml.to_owned();
    };
    let content_start = open_start + rel_end + 1;

    let Some(rel_close) = xml[content_start..].find(&close_tag) else {
        return xml.to_owned();
    };
    let content_end = content_start + rel_close;

    let mut result = String::with_capacity(xml.len());
    result.push_str(&xml[..content_start]);
    result.push_str(new_value);
    result.push_str(&xml[content_end..]);
    result
}

/// Replaces the text content between an exact opening tag string and a closing tag string.
/// Used for the inner `<BlueprintID d:type="cDeltaString">` to distinguish from the outer
/// `<BlueprintID>` wrapper element.
fn set_tagged_element_text(xml: &str, open_tag: &str, close_tag: &str, new_value: &str) -> String {
    let Some(open_pos) = xml.find(open_tag) else {
        return xml.to_owned();
    };
    let content_start = open_pos + open_tag.len();

    let Some(rel_close) = xml[content_start..].find(close_tag) else {
        return xml.to_owned();
    };
    let content_end = content_start + rel_close;

    let mut result = String::with_capacity(xml.len());
    result.push_str(&xml[..content_start]);
    result.push_str(new_value);
    result.push_str(&xml[content_end..]);
    result
}
