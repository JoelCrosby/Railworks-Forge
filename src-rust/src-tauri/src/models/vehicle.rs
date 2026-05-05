use super::blueprint::Blueprint;
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq, Default)]
#[serde(rename_all = "camelCase")]
pub enum BlueprintType {
    Engine,
    Tender,
    Coach,
    Wagon,
    #[default]
    Unknown,
}

impl BlueprintType {
    pub fn from_str(s: &str) -> Self {
        match s.to_lowercase().as_str() {
            s if s.contains("engine") || s.contains("loco") => Self::Engine,
            s if s.contains("tender") => Self::Tender,
            s if s.contains("coach") || s.contains("passenger") => Self::Coach,
            s if s.contains("wagon") || s.contains("freight") => Self::Wagon,
            _ => Self::Unknown,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct VehicleBlueprint {
    pub blueprint: Blueprint,
    pub name: String,
    pub unique_number: String,
    pub blueprint_type: BlueprintType,
    pub flipped: bool,
    pub index: usize,
}
