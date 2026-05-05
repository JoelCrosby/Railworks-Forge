use super::{consist::Consist, route::PackagingType};
use serde::{Deserialize, Serialize};
use std::path::PathBuf;

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "camelCase")]
pub enum ScenarioClass {
    Passenger,
    Freight,
    Shunting,
    Mixed,
    Empty,
}

impl ScenarioClass {
    pub fn from_str(s: &str) -> Self {
        match s {
            "eScenarioClass_Passenger" => Self::Passenger,
            "eScenarioClass_Freight" => Self::Freight,
            "eScenarioClass_Shunting" => Self::Shunting,
            "eScenarioClass_Mixed" => Self::Mixed,
            _ => Self::Empty,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ScenarioPlayerInfo {
    pub scenario_id: String,
    pub score: i32,
    pub completion: String,
    pub medals_awarded: i32,
}

impl ScenarioPlayerInfo {
    pub fn empty(scenario_id: impl Into<String>) -> Self {
        Self {
            scenario_id: scenario_id.into(),
            score: 0,
            completion: String::new(),
            medals_awarded: 0,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct Scenario {
    pub id: String,
    pub name: String,
    pub description: Option<String>,
    pub briefing: Option<String>,
    pub start_location: Option<String>,
    pub locomotive: String,
    pub duration: i32,
    pub rating: i32,
    pub season: String,
    pub scenario_class: ScenarioClass,
    pub packaging_type: PackagingType,
    pub directory_path: PathBuf,
    pub route_id: String,
    pub player_info: ScenarioPlayerInfo,
    #[serde(skip_serializing_if = "Vec::is_empty", default)]
    pub consists: Vec<Consist>,
}

impl Scenario {
    pub fn properties_path(&self) -> PathBuf {
        self.directory_path.join("ScenarioProperties.xml")
    }

    pub fn binary_path(&self) -> PathBuf {
        self.directory_path.join("Scenario.bin")
    }

    pub fn scenario_archive_path(&self) -> PathBuf {
        self.directory_path.join("MainContent.ap")
    }

    pub fn search_index(&self) -> String {
        format!(
            "{} {} {}",
            self.name.to_lowercase(),
            self.locomotive.to_lowercase(),
            self.season.to_lowercase()
        )
    }
}
