use crate::models::vehicle::BlueprintType;

const ENGINE_TEMPLATE: &str = include_str!("templates/Engine.xml");
const WAGON_TEMPLATE: &str = include_str!("templates/Wagon.xml");
const TENDER_TEMPLATE: &str = include_str!("templates/Tender.xml");

pub fn get_template(blueprint_type: &BlueprintType) -> &'static str {
    match blueprint_type {
        BlueprintType::Engine => ENGINE_TEMPLATE,
        BlueprintType::Tender => TENDER_TEMPLATE,
        _ => WAGON_TEMPLATE,
    }
}
