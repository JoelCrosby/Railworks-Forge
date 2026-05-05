use serde::{Deserialize, Serialize};
use std::path::PathBuf;

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "camelCase")]
pub enum AcquisitionState {
    Found,
    Partial,
    Missing,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Blueprint {
    pub provider: String,
    pub product: String,
    pub blueprint_id: String,
    pub acquisition_state: AcquisitionState,
}

impl Blueprint {
    pub fn new(provider: impl Into<String>, product: impl Into<String>, blueprint_id: impl Into<String>) -> Self {
        Self {
            provider: provider.into(),
            product: product.into(),
            blueprint_id: blueprint_id.into(),
            acquisition_state: AcquisitionState::Missing,
        }
    }

    pub fn asset_path(&self, assets_root: &PathBuf) -> PathBuf {
        assets_root
            .join(&self.provider)
            .join(&self.product)
            .join(&self.blueprint_id)
    }
}
