mod archive;
mod cache;
mod commands;
mod models;
mod platform;
mod services;
mod serz;
mod xml;

use commands::{assets, consists, routes, scenarios, tracks};

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tracing_subscriber::fmt()
        .with_env_filter(
            tracing_subscriber::EnvFilter::try_from_default_env()
                .unwrap_or_else(|_| tracing_subscriber::EnvFilter::new("info")),
        )
        .init();

    tauri::Builder::default()
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_dialog::init())
        .setup(|app| {
            let handle = app.handle().clone();
            tauri::async_runtime::spawn(async move {
                scenarios::prime_scenario_db(handle).await;
            });
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            routes::get_routes,
            routes::get_route,
            routes::get_game_path,
            routes::set_game_path,
            scenarios::get_scenarios,
            scenarios::get_scenario_detail,
            consists::get_consist_detail,
            consists::replace_consist,
            consists::add_vehicle,
            consists::delete_vehicle,
            consists::delete_consist,
            consists::save_consist,
            consists::get_saved_consists,
            consists::delete_saved_consist,
            tracks::get_tracks,
            tracks::replace_tracks,
            assets::check_assets,
            assets::get_asset_tree,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
