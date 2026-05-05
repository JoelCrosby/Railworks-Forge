use crate::models::blueprint::{AcquisitionState, Blueprint};
use anyhow::Result;
use std::path::{Path, PathBuf};

/// Checks whether a blueprint's assets are present on disk or in a packed archive.
pub fn check_acquisition(blueprint: &Blueprint, assets_root: &Path) -> AcquisitionState {
    let asset_path = blueprint.asset_path(&assets_root.to_path_buf());

    // Check unpacked first
    if asset_path.exists() {
        return AcquisitionState::Found;
    }

    // Check packed inside a product .ap archive
    let archive_path = packed_archive_path(&blueprint.provider, &blueprint.product, assets_root);
    if archive_path.exists() {
        let entry = blueprint.blueprint_id.replace('\\', "/");
        if crate::archive::entry_exists(&archive_path, &entry) {
            return AcquisitionState::Found;
        }
        // Archive exists but entry not found — Partial (provider/product available but not this asset)
        return AcquisitionState::Partial;
    }

    AcquisitionState::Missing
}

fn packed_archive_path(provider: &str, product: &str, assets_root: &Path) -> PathBuf {
    assets_root
        .join(provider)
        .join(product)
        .with_extension("ap")
}

/// Scans the Assets directory and returns a flat list of all provider/product pairs.
pub fn get_asset_tree(assets_root: &Path) -> Result<Vec<AssetNode>> {
    let mut nodes = Vec::new();

    for provider_entry in std::fs::read_dir(assets_root)? {
        let provider_path = provider_entry?.path();
        if !provider_path.is_dir() {
            continue;
        }
        let provider = provider_path
            .file_name()
            .and_then(|n| n.to_str())
            .unwrap_or("")
            .to_string();

        for product_entry in std::fs::read_dir(&provider_path)? {
            let product_path = product_entry?.path();
            let product = product_path
                .file_name()
                .and_then(|n| n.to_str())
                .unwrap_or("")
                .to_string();

            let has_rail_vehicles = product_path.join("RailVehicles").exists()
                || crate::archive::entry_exists(
                    &product_path.with_extension("ap"),
                    "RailVehicles/",
                );

            nodes.push(AssetNode {
                provider: provider.clone(),
                product,
                has_rail_vehicles,
            });
        }
    }

    nodes.sort_by(|a, b| {
        a.provider
            .cmp(&b.provider)
            .then(a.product.cmp(&b.product))
    });
    Ok(nodes)
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AssetNode {
    pub provider: String,
    pub product: String,
    pub has_rail_vehicles: bool,
}
