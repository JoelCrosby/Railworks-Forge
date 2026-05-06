mod archive;
mod cache;
mod commands;
mod models;
mod platform;
mod services;
mod serz;
mod xml;

use commands::{assets, consists, routes, scenarios, settings, tracks};
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    init_logging();

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
            settings::get_settings,
            settings::save_settings,
            settings::clear_xml_cache,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

fn init_logging() {
    let env_filter = tracing_subscriber::EnvFilter::try_from_default_env().unwrap_or_else(|_| {
        tracing_subscriber::EnvFilter::new("info,railworks_forge::profile=info")
    });

    let stdout_layer = tracing_subscriber::fmt::layer()
        .compact()
        .with_target(false);

    let log_dir = platform::app_log_dir().ok();
    if let Some(dir) = log_dir {
        let _ = std::fs::create_dir_all(&dir);
        let file_appender = tracing_appender::rolling::daily(dir, "railworks-forge.log");
        let file_layer = tracing_subscriber::fmt::layer()
            .json()
            .with_writer(file_appender)
            .with_target(true);

        tracing_subscriber::registry()
            .with(env_filter)
            .with(stdout_layer)
            .with(file_layer)
            .init();
    } else {
        tracing_subscriber::registry()
            .with(env_filter)
            .with(stdout_layer)
            .init();
    }
}
