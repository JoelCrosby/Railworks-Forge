use crate::models::blueprint::{AcquisitionState, Blueprint};
use anyhow::{Context, Result};
use std::path::{Path, PathBuf};

/// Checks whether a blueprint's assets are present on disk or in a packed archive.
pub fn check_acquisition(blueprint: &Blueprint, assets_root: &Path) -> AcquisitionState {
    if blueprint.provider.is_empty() || blueprint.product.is_empty() {
        return AcquisitionState::Missing;
    }

    let asset_path = blueprint.asset_path(&assets_root.to_path_buf());
    let bin_path = asset_path.with_extension("bin");

    // Check unpacked first
    if asset_path.exists() || bin_path.exists() {
        return AcquisitionState::Found;
    }

    let product_path = assets_root
        .join(&blueprint.provider)
        .join(&blueprint.product);
    if !product_path.exists() {
        return AcquisitionState::Missing;
    }

    // Check packed assets inside product .ap archives.
    let xml_entry = blueprint.blueprint_id.replace('\\', "/");
    let bin_entry = xml_entry.strip_suffix(".xml").map(|p| format!("{p}.bin"));
    for archive_path in product_archives_recursive(&product_path) {
        if crate::archive::entry_exists(&archive_path, &xml_entry)
            || bin_entry
                .as_deref()
                .map(|entry| crate::archive::entry_exists(&archive_path, entry))
                .unwrap_or(false)
        {
            return AcquisitionState::Found;
        }
    }

    // Provider/product exists, but this specific blueprint does not.
    AcquisitionState::Partial
}

/// Scans the Assets directory and returns a flat list of all provider/product pairs.
pub fn get_asset_tree(assets_root: &Path) -> Result<Vec<AssetNode>> {
    let database = AssetDatabase::build(assets_root)?;
    Ok(database
        .provider_directories
        .into_iter()
        .flat_map(|provider| {
            let provider_name = provider.name;
            provider.products.into_iter().map(move |product| AssetNode {
                provider: provider_name.clone(),
                product: product.name,
                has_rail_vehicles: product.contains_rail_vehicles,
                has_preload_data: product.contains_preload_data,
            })
        })
        .collect())
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AssetDatabase {
    pub provider_directories: Vec<ProviderDirectory>,
}

impl AssetDatabase {
    pub fn build(assets_root: &Path) -> Result<Self> {
        let mut provider_directories = Vec::new();

        for provider_entry in std::fs::read_dir(assets_root)
            .with_context(|| format!("reading assets directory: {}", assets_root.display()))?
        {
            let provider_path = provider_entry?.path();
            if !provider_path.is_dir() {
                continue;
            }

            let provider = provider_path
                .file_name()
                .and_then(|n| n.to_str())
                .unwrap_or("")
                .to_string();

            provider_directories.push(ProviderDirectory {
                name: provider,
                path: provider_path.to_string_lossy().into_owned(),
                products: product_directories(&provider_path)?,
            });
        }

        provider_directories.sort_by(|a, b| a.name.cmp(&b.name));
        Ok(Self {
            provider_directories,
        })
    }
}

fn product_directories(provider_path: &Path) -> Result<Vec<ProductDirectory>> {
    let mut products = Vec::new();

    for product_entry in std::fs::read_dir(provider_path)
        .with_context(|| format!("reading provider directory: {}", provider_path.display()))?
    {
        let product_path = product_entry?.path();
        if !product_path.is_dir() {
            continue;
        }

        let product = product_path
            .file_name()
            .and_then(|n| n.to_str())
            .unwrap_or("")
            .to_string();
        let flags = product_flags(&product_path)?;

        products.push(ProductDirectory {
            name: product,
            path: product_path.to_string_lossy().into_owned(),
            contains_rail_vehicles: flags.contains_rail_vehicles,
            contains_preload_data: flags.contains_preload_data,
        });
    }

    products.sort_by(|a, b| a.name.cmp(&b.name));
    Ok(products)
}

fn product_flags(product_path: &Path) -> Result<ProductFlags> {
    let mut contains_rail_vehicles = child_dir_exists(product_path, "RailVehicles");
    let mut contains_preload_data = child_dir_exists(product_path, "PreloadData");

    for archive in product_archives(product_path) {
        if !contains_rail_vehicles {
            contains_rail_vehicles =
                crate::archive::entry_with_prefix_exists(&archive, "RailVehicles/");
        }
        if !contains_preload_data {
            contains_preload_data =
                crate::archive::entry_with_prefix_exists(&archive, "PreloadData/");
        }
        if contains_rail_vehicles && contains_preload_data {
            break;
        }
    }

    Ok(ProductFlags {
        contains_rail_vehicles,
        contains_preload_data,
    })
}

fn child_dir_exists(path: &Path, child_name: &str) -> bool {
    let Ok(children) = std::fs::read_dir(path) else {
        return false;
    };

    children.filter_map(Result::ok).any(|entry| {
        entry.path().is_dir()
            && entry
                .file_name()
                .to_str()
                .map(|name| name.eq_ignore_ascii_case(child_name))
                .unwrap_or(false)
    })
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
                    .and_then(|ext| ext.to_str())
                    .map(|ext| ext.eq_ignore_ascii_case("ap"))
                    .unwrap_or(false)
        })
        .collect()
}

fn product_archives_recursive(product_path: &Path) -> Vec<PathBuf> {
    let mut archives = Vec::new();
    let mut pending = vec![product_path.to_path_buf()];

    while let Some(dir) = pending.pop() {
        let Ok(children) = std::fs::read_dir(&dir) else {
            continue;
        };

        for child in children.filter_map(Result::ok) {
            let path = child.path();
            if path.is_dir() {
                pending.push(path);
            } else if path
                .extension()
                .and_then(|ext| ext.to_str())
                .map(|ext| ext.eq_ignore_ascii_case("ap"))
                .unwrap_or(false)
            {
                archives.push(path);
            }
        }
    }

    archives
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AssetNode {
    pub provider: String,
    pub product: String,
    pub has_rail_vehicles: bool,
    pub has_preload_data: bool,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ProviderDirectory {
    pub name: String,
    pub path: String,
    pub products: Vec<ProductDirectory>,
}

#[derive(Debug, serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ProductDirectory {
    pub name: String,
    pub path: String,
    pub contains_rail_vehicles: bool,
    pub contains_preload_data: bool,
}

struct ProductFlags {
    contains_rail_vehicles: bool,
    contains_preload_data: bool,
}
