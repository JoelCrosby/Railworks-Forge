use crate::models::vehicle::BlueprintType;
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct VehicleEntry {
    pub provider: String,
    pub product: String,
    pub blueprint_id: String,
    pub flipped: bool,
    #[serde(default)]
    pub blueprint_type: BlueprintType,
}

impl VehicleEntry {
    pub fn resolved_type(&self) -> BlueprintType {
        if self.blueprint_type != BlueprintType::Unknown {
            return self.blueprint_type.clone();
        }
        BlueprintType::from_str(&self.blueprint_id)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SavedConsist {
    pub name: String,
    pub entries: Vec<VehicleEntry>,
}

#[derive(Debug, Clone)]
pub enum ConsistCommand {
    DeleteVehicle {
        consist_id: String,
        vehicle_index: usize,
    },
    DeleteConsist {
        consist_id: String,
    },
    ReplaceVehicles {
        consist_id: String,
        entries: Vec<VehicleEntry>,
    },
}

impl ConsistCommand {
    pub fn consist_id(&self) -> &str {
        match self {
            Self::DeleteVehicle { consist_id, .. } => consist_id,
            Self::DeleteConsist { consist_id } => consist_id,
            Self::ReplaceVehicles { consist_id, .. } => consist_id,
        }
    }
}
