use crate::{
    archive,
    models::{blueprint::Blueprint, Consist, Route},
};
use base64::{engine::general_purpose, Engine as _};
use std::{
    ffi::OsStr,
    path::{Component, Path, PathBuf},
};

const PNG_MIME: &str = "image/png";

/// Adds route thumbnail data URLs using the same lookup order as the C# app:
/// loose `RouteInformation/Image.png`, then `MainContent.ap`.
pub fn route_image_data_url(route: &Route) -> Option<String> {
    let loose = route
        .directory_path
        .join("RouteInformation")
        .join("Image.png");
    read_case_insensitive(&loose)
        .or_else(|| {
            let archive_path = resolve_case_insensitive_path(&route.main_content_archive_path())?;
            archive::read_entry(&archive_path, "RouteInformation/Image.png").ok()
        })
        .map(|bytes| data_url(PNG_MIME, &bytes))
}

/// Adds consist image data URLs in place. The lead vehicle is preferred, with
/// the final vehicle as the same fallback used by the original Avalonia UI.
pub fn populate_consist_images(consists: &mut [Consist], assets_root: &Path) {
    for consist in consists {
        consist.image_data_url = consist_image_data_url(consist, assets_root);
    }
}

pub fn consist_image_data_url(consist: &Consist, assets_root: &Path) -> Option<String> {
    blueprint_image_data_url(&consist.blueprint, assets_root).or_else(|| {
        consist
            .vehicles
            .last()
            .and_then(|vehicle| blueprint_image_data_url(&vehicle.blueprint, assets_root))
    })
}

fn blueprint_image_data_url(blueprint: &Blueprint, assets_root: &Path) -> Option<String> {
    if blueprint.provider.is_empty()
        || blueprint.product.is_empty()
        || blueprint.blueprint_id.is_empty()
    {
        return None;
    }

    let product_path = assets_root
        .join(&blueprint.provider)
        .join(&blueprint.product);
    let blueprint_path = product_path.join(normalise_blueprint_path(&blueprint.blueprint_id));
    let blueprint_dir = blueprint_path.parent()?;
    let loose = blueprint_dir.join("LocoInformation").join("Image.png");

    read_case_insensitive(&loose)
        .or_else(|| read_blueprint_image_from_archives(&product_path, blueprint_dir, &product_path))
        .map(|bytes| data_url(PNG_MIME, &bytes))
}

fn read_blueprint_image_from_archives(
    product_path: &Path,
    blueprint_dir: &Path,
    product_root: &Path,
) -> Option<Vec<u8>> {
    let product_path = resolve_case_insensitive_path(product_path)?;
    let entry_dir = blueprint_dir
        .strip_prefix(product_root)
        .ok()
        .map(path_to_archive_entry)?;
    let entry_name = format!("{entry_dir}/LocoInformation/Image.png");

    for archive_path in product_archives(&product_path) {
        if let Ok(bytes) = archive::read_entry(&archive_path, &entry_name) {
            return Some(bytes);
        }
    }

    None
}

fn product_archives(product_path: &Path) -> Vec<PathBuf> {
    let Ok(children) = std::fs::read_dir(product_path) else {
        return Vec::new();
    };

    children
        .filter_map(Result::ok)
        .map(|entry| entry.path())
        .filter(|path| {
            path.is_file()
                && path
                    .extension()
                    .and_then(OsStr::to_str)
                    .map(|ext| ext.eq_ignore_ascii_case("ap"))
                    .unwrap_or(false)
        })
        .collect()
}

fn read_case_insensitive(path: &Path) -> Option<Vec<u8>> {
    let path = resolve_case_insensitive_path(path)?;
    std::fs::read(path).ok()
}

fn resolve_case_insensitive_path(path: &Path) -> Option<PathBuf> {
    if path.exists() {
        return Some(path.to_path_buf());
    }

    let mut resolved = PathBuf::new();
    for component in path.components() {
        match component {
            Component::Normal(name) => {
                let base = if resolved.as_os_str().is_empty() {
                    Path::new(".")
                } else {
                    resolved.as_path()
                };
                let match_path = std::fs::read_dir(base)
                    .ok()?
                    .filter_map(Result::ok)
                    .find(|entry| os_str_eq_ignore_ascii_case(&entry.file_name(), name))
                    .map(|entry| entry.path())?;
                resolved = match_path;
            }
            _ => resolved.push(component.as_os_str()),
        }
    }

    resolved.exists().then_some(resolved)
}

fn os_str_eq_ignore_ascii_case(a: &OsStr, b: &OsStr) -> bool {
    let Some(a) = a.to_str() else {
        return false;
    };
    let Some(b) = b.to_str() else {
        return false;
    };
    a.eq_ignore_ascii_case(b)
}

fn normalise_blueprint_path(path: &str) -> PathBuf {
    PathBuf::from(path.replace('\\', "/"))
}

fn path_to_archive_entry(path: &Path) -> String {
    path.components()
        .filter_map(|component| match component {
            Component::Normal(part) => part.to_str().map(str::to_owned),
            _ => None,
        })
        .collect::<Vec<_>>()
        .join("/")
}

fn data_url(mime: &str, bytes: &[u8]) -> String {
    format!(
        "data:{mime};base64,{}",
        general_purpose::STANDARD.encode(bytes)
    )
}
