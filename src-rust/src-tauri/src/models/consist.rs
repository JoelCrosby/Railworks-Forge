use super::{blueprint::Blueprint, vehicle::VehicleBlueprint};
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "camelCase")]
pub enum LocoClass {
    Steam,
    Diesel,
    Electric,
    Unknown,
}

impl LocoClass {
    pub fn from_str(s: &str) -> Self {
        let lower = s.to_lowercase();
        if lower.contains("steam") {
            Self::Steam
        } else if lower.contains("diesel") {
            Self::Diesel
        } else if lower.contains("electric") {
            Self::Electric
        } else {
            Self::Unknown
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "camelCase")]
pub enum ConsistAcquisitionState {
    Found,
    Partial,
    Missing,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Consist {
    pub id: String,
    pub locomotive_name: String,
    pub service_name: String,
    pub service_id: String,
    pub loco_author: Option<String>,
    pub loco_class: LocoClass,
    pub player_driver: bool,
    pub blueprint: Blueprint,
    pub vehicles: Vec<VehicleBlueprint>,
    pub acquisition_state: ConsistAcquisitionState,
}
