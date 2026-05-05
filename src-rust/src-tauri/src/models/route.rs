use serde::{Deserialize, Serialize};
use std::path::PathBuf;

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "camelCase")]
pub enum PackagingType {
    Packed,
    Unpacked,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Route {
    pub id: String,
    pub name: String,
    pub description: Option<String>,
    pub directory_path: PathBuf,
    pub packaging_type: PackagingType,
}

impl Route {
    pub fn route_properties_path(&self) -> PathBuf {
        self.directory_path.join("RouteProperties.xml")
    }

    pub fn main_content_archive_path(&self) -> PathBuf {
        self.directory_path.join("MainContent.ap")
    }

    pub fn tracks_binary_path(&self) -> PathBuf {
        self.directory_path.join("Networks").join("Tracks.bin")
    }

    pub fn scenarios_directory(&self) -> PathBuf {
        self.directory_path.join("Scenarios")
    }
}
