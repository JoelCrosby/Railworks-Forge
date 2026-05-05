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

impl Consist {
    pub fn lead_vehicle(&self) -> Option<&VehicleBlueprint> {
        self.vehicles.first()
    }

    pub fn search_index(&self) -> String {
        format!(
            "{} {} {}",
            self.service_name.to_lowercase(),
            self.locomotive_name.to_lowercase(),
            self.loco_author.as_deref().unwrap_or("").to_lowercase()
        )
    }
}

/// A consist entry within a preload blueprint — used as a template when adding vehicles.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ConsistEntry {
    pub blueprint: Blueprint,
    pub flipped: bool,
}

/// A preload consist template used for replacement and vehicle-add operations.
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PreloadConsist {
    pub service_name: String,
    pub entries: Vec<ConsistEntry>,
}
