use anyhow::{Context, Result};
use quick_xml::{events::Event, Reader};
use std::{io::BufRead, path::Path};
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

/// Reads the text content of an element inside a specific parent element.
pub fn find_nested_text<R: BufRead>(
    reader: &mut Reader<R>,
    parent: &str,
    child: &str,
) -> Option<String> {
    let mut buf = Vec::new();
    let mut depth = 0i32;
    let mut in_parent = false;
    let mut in_child = false;

    loop {
        match reader.read_event_into(&mut buf) {
            Ok(Event::Start(e)) => {
                let name_bytes = e.name().as_ref().to_vec();
                let name_str = String::from_utf8_lossy(&name_bytes).into_owned();
                if name_str == parent {
                    in_parent = true;
                    depth = 0;
                } else if in_parent && name_str == child {
                    in_child = true;
                } else if in_parent {
                    depth += 1;
                }
            }
            Ok(Event::Text(e)) if in_child => {
                return e.unescape().ok().map(|s| s.into_owned());
            }
            Ok(Event::End(e)) => {
                let name_bytes = e.name().as_ref().to_vec();
                let name_str = String::from_utf8_lossy(&name_bytes).into_owned();
                if name_str == child {
                    in_child = false;
                } else if name_str == parent && depth == 0 {
                    in_parent = false;
                } else if in_parent {
                    depth -= 1;
                }
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
