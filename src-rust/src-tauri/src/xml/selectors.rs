/// Helpers that mirror the AngleSharp extension methods used throughout the C# codebase.
/// These operate on raw XML strings and use the pull-parser from parser.rs to avoid
/// building a full DOM.
use super::parser::find_text_content;

pub fn select_text(xml: &str, element: &str) -> Option<String> {
    find_text_content(xml, element)
}

pub fn select_integer(xml: &str, element: &str) -> Option<i32> {
    find_text_content(xml, element)?.parse().ok()
}

/// Extracts the localised string content — Railworks XML wraps localised strings
/// in <Localisation-cUserLocalisedString> with an <English> child.
pub fn select_localised(xml: &str, element: &str) -> Option<String> {
    // Find the element, then look for <English> inside it.
    // This is a simplified version — a full implementation would walk the subtree.
    let start = xml.find(&format!("<{element}>"))?;
    let slice = &xml[start..];
    find_text_content(slice, "English").or_else(|| find_text_content(slice, element))
}
