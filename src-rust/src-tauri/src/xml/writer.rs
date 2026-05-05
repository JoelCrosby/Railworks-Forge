use anyhow::Result;

/// Updates the text content of a named XML element in-place within a String.
/// For single, non-nested elements only. Scenario writes use a more targeted
/// approach — replace the specific element subtree rather than the full document.
pub fn update_element_text(xml: &str, element: &str, new_value: &str) -> Result<String> {
    let open_tag = format!("<{element}>");
    let close_tag = format!("</{element}>");

    let start = xml
        .find(&open_tag)
        .ok_or_else(|| anyhow::anyhow!("element <{element}> not found in XML"))?;
    let content_start = start + open_tag.len();
    let content_end = xml[content_start..]
        .find(&close_tag)
        .ok_or_else(|| anyhow::anyhow!("closing </{element}> not found"))?
        + content_start;

    let mut result = String::with_capacity(xml.len());
    result.push_str(&xml[..content_start]);
    result.push_str(new_value);
    result.push_str(&xml[content_end..]);
    Ok(result)
}
