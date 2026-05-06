use anyhow::{Context, Result};
use quick_xml::{events::Event, Reader};
use std::path::Path;
use tokio::io::AsyncReadExt;

/// Reads the full text content of an element by name from an XML string.
/// Returns the first match. Uses a pull-parser to avoid loading a full DOM.
pub fn find_text_content(xml: &str, element_name: &str) -> Option<String> {
    let mut reader = Reader::from_str(xml);
    reader.config_mut().trim_text(true);

    let mut in_target = false;
    let mut buf = Vec::new();

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(e)) if e.name().as_ref() == element_name.as_bytes() => {
                in_target = true;
            }
            Ok(Event::Text(e)) if in_target => {
                return e.unescape().ok().map(|s| s.into_owned());
            }
            Ok(Event::End(_)) if in_target => {
                in_target = false;
            }
            Ok(Event::Eof) | Err(_) => break,
            _ => {}
        }
        buf.clear();
    }
    None
}

/// Reads an entire XML file into memory as a String.
/// For large files, prefer the streaming parsers in the service layer.
pub async fn read_xml_file(path: &Path) -> Result<String> {
    let mut file = tokio::fs::File::open(path)
        .await
        .with_context(|| format!("opening XML file: {}", path.display()))?;
    let mut content = String::new();
    file.read_to_string(&mut content)
        .await
        .with_context(|| format!("reading XML file: {}", path.display()))?;
    Ok(content)
}
