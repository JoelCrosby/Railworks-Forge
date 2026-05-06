use crate::{
    archive,
    models::{PackagingType, Route},
    services::image_service,
    xml::{parser::read_xml_file, selectors},
};
use anyhow::Result;
use std::path::Path;
use tokio::{fs, task::JoinSet};

/// Discovers all routes under the game's Content/Routes directory.
///
/// Each subdirectory is checked concurrently: all route discovery tasks are
/// spawned into a JoinSet so I/O (XML reads + ZIP extractions) overlaps rather
/// than serialising. With a typical 50–200 route install this is the dominant
/// speedup over the previous sequential loop.
pub async fn get_routes(routes_dir: &Path) -> Result<Vec<Route>> {
    let _profile = crate::services::profiling::ProfileSpan::new("get_routes");
    let mut entries = fs::read_dir(routes_dir).await?;
    let mut tasks: JoinSet<Option<Route>> = JoinSet::new();

    while let Some(entry) = entries.next_entry().await? {
        let path = entry.path();
        if !path.is_dir() {
            continue;
        }
        tasks.spawn(async move {
            match discover_route(&path).await {
                Ok(r) => r,
                Err(err) => {
                    tracing::warn!("skipping route at {}: {err:#}", path.display());
                    None
                }
            }
        });
    }

    let mut routes = Vec::with_capacity(tasks.len());
    while let Some(result) = tasks.join_next().await {
        if let Ok(Some(route)) = result {
            routes.push(route);
        }
    }

    routes.sort_by(|a, b| a.name.cmp(&b.name));
    Ok(routes)
}

/// Loads a single route by directory ID.
pub async fn get_route(routes_dir: &Path, route_id: &str) -> Result<Option<Route>> {
    let _profile = crate::services::profiling::ProfileSpan::new("get_route");
    let route_dir = routes_dir.join(route_id);
    if !fs::try_exists(&route_dir).await.unwrap_or(false) {
        return Ok(None);
    }

    discover_route(&route_dir).await
}

async fn discover_route(dir: &Path) -> Result<Option<Route>> {
    let unpacked_props = dir.join("RouteProperties.xml");
    let packed_archive = dir.join("MainContent.ap");

    // Use async existence checks so we don't block the executor.
    if fs::try_exists(&unpacked_props).await.unwrap_or(false) {
        let route = parse_route_from_file(dir, &unpacked_props, PackagingType::Unpacked).await?;
        return Ok(Some(route));
    }

    if fs::try_exists(&packed_archive).await.unwrap_or(false) {
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

    let mut route = Route {
        id,
        name,
        description,
        directory_path: dir.to_path_buf(),
        packaging_type: packaging,
        image_data_url: None,
    };
    route.image_data_url = image_service::route_image_data_url(&route);

    Ok(route)
}
