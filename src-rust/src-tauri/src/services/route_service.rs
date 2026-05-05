use crate::{
    archive,
    models::{PackagingType, Route},
    xml::{parser::read_xml_file, selectors},
};
use anyhow::Result;
use std::path::Path;
use tokio::fs;

/// Discovers all routes under the game's Content/Routes directory.
/// Each route is a directory containing either RouteProperties.xml directly
/// (Unpacked) or a MainContent.ap archive that contains it (Packed).
pub async fn get_routes(routes_dir: &Path) -> Result<Vec<Route>> {
    let mut routes = Vec::new();
    let mut entries = fs::read_dir(routes_dir).await?;

    while let Some(entry) = entries.next_entry().await? {
        let path = entry.path();
        if !path.is_dir() {
            continue;
        }

        match discover_route(&path).await {
            Ok(Some(route)) => routes.push(route),
            Ok(None) => {}
            Err(err) => {
                tracing::warn!("skipping route at {}: {err:#}", path.display());
            }
        }
    }

    routes.sort_by(|a, b| a.name.cmp(&b.name));
    Ok(routes)
}

async fn discover_route(dir: &Path) -> Result<Option<Route>> {
    let unpacked_props = dir.join("RouteProperties.xml");
    let packed_archive = dir.join("MainContent.ap");

    if unpacked_props.exists() {
        let route = parse_route_from_file(dir, &unpacked_props, PackagingType::Unpacked).await?;
        return Ok(Some(route));
    }

    if packed_archive.exists() {
        let route = parse_route_from_archive(dir, &packed_archive).await?;
        return Ok(Some(route));
    }

    Ok(None)
}

async fn parse_route_from_file(
    dir: &Path,
    props_path: &Path,
    packaging: PackagingType,
) -> Result<Route> {
    let xml = read_xml_file(props_path).await?;
    build_route(dir, &xml, packaging)
}

async fn parse_route_from_archive(dir: &Path, archive_path: &Path) -> Result<Route> {
    let xml = tokio::task::spawn_blocking({
        let archive_path = archive_path.to_path_buf();
        move || archive::read_entry_as_string(&archive_path, "RouteProperties.xml")
    })
    .await??;

    build_route(dir, &xml, PackagingType::Packed)
}

fn build_route(dir: &Path, xml: &str, packaging: PackagingType) -> Result<Route> {
    let id = dir
        .file_name()
        .and_then(|n| n.to_str())
        .unwrap_or("unknown")
        .to_string();

    let name = selectors::select_localised(xml, "DisplayName")
        .or_else(|| selectors::select_text(xml, "DisplayName"))
        .unwrap_or_else(|| id.clone());

    let description = selectors::select_localised(xml, "Description")
        .or_else(|| selectors::select_text(xml, "Description"));

    Ok(Route {
        id,
        name,
        description,
        directory_path: dir.to_path_buf(),
        packaging_type: packaging,
    })
}
