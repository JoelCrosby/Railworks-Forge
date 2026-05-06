# Railworks Forge — Rust/Tauri Rewrite

## Overview

Full rewrite of the C#/Avalonia desktop app in Rust + Tauri 2 + Svelte. The primary motivation is performance: `Scenario.bin` files expand to 500MB+ when deserialised, and the existing DOM-based XML parser (AngleSharp) loads the entire document into memory. The Rust rewrite uses a streaming pull-parser (`quick-xml`) that never holds more than one element subtree in memory.

---

## Technology Choices

| Role | Choice | Rationale |
|---|---|---|
| Language | Rust (stable) | Memory safety + zero-cost abstractions |
| GUI framework | Tauri 2.x | Native webview shell; Rust backend with JS/TS frontend |
| Frontend framework | SvelteKit in SPA mode | Svelte UI with file-based routing; static output embedded by Tauri |
| Async runtime | Tokio | Industry standard; Tauri already uses it |
| XML parsing | `quick-xml` | Zero-copy, pull-based SAX-style, extremely fast |
| ZIP / .ap archives | `zip` crate | .ap files are standard ZIP |
| Concurrency | `dashmap` + `rayon` | Lock-free maps; data-parallelism for route/asset loading |
| Serialisation | `serde` + `serde_json` | Config files, SDBCache JSON cache |
| Error handling | `anyhow` + `thiserror` | Application errors / library boundary errors |
| Logging | `tracing` + `tracing-subscriber` + `tracing-appender` | Structured async-aware logging with rolling file output |
| Retry | `tokio-retry` | Replaces Polly |
| i18n | Fluent message catalogs (`@fluent/bundle`) | English + German UI strings in the Svelte client |
| Windows registry | `winreg` (cfg-gated) | Railworks install path detection |

---

## Project Structure

```
src-rust/
├── src/                            # Svelte frontend (SvelteKit)
│   └── ...
├── src-tauri/
│   ├── src/
│   │   ├── main.rs
│   │   ├── lib.rs                  # Tauri builder + handler registration
│   │   ├── commands/               # Tauri IPC command handlers
│   │   │   ├── mod.rs
│   │   │   ├── routes.rs           # get_routes, get_game_path, set_game_path
│   │   │   ├── scenarios.rs        # get_scenarios, get_scenario_detail
│   │   │   ├── consists.rs         # get_consist_detail, save_consist, replace_consist, add/delete_vehicle
│   │   │   ├── tracks.rs           # get_tracks, replace_tracks
│   │   │   ├── assets.rs           # check_assets, get_asset_tree
│   │   │   └── settings.rs         # get_settings, save_settings, clear_xml_cache
│   │   ├── services/               # Business logic
│   │   │   ├── mod.rs
│   │   │   ├── route_service.rs    # Route discovery + RouteProperties.xml parsing
│   │   │   ├── scenario_service.rs # Scenario loading (packed + unpacked)
│   │   │   ├── scenario_db.rs      # SDBCache.bin player progress (streaming parser + JSON cache)
│   │   │   └── asset_service.rs    # Acquisition state checks, asset tree enumeration
│   │   ├── models/                 # Domain types
│   │   │   ├── mod.rs
│   │   │   ├── route.rs            # Route, PackagingType
│   │   │   ├── scenario.rs         # Scenario, ScenarioClass, ScenarioPlayerInfo
│   │   │   ├── consist.rs          # Consist, LocoClass, ConsistAcquisitionState
│   │   │   ├── blueprint.rs        # Blueprint, AcquisitionState
│   │   │   └── vehicle.rs          # VehicleBlueprint, BlueprintType
│   │   ├── xml/                    # XML parsing layer
│   │   │   ├── mod.rs
│   │   │   ├── parser.rs           # quick-xml pull-parser helpers + async file reader
│   │   │   └── selectors.rs        # Element query helpers (mirrors AngleSharp extensions)
│   │   ├── serz/                   # Serz binary <-> XML bridge
│   │   │   ├── mod.rs
│   │   │   └── process.rs          # Spawns serz64.exe (native on Windows, Wine on Linux/macOS)
│   │   ├── cache/                  # Disk caches
│   │   │   ├── mod.rs
│   │   │   ├── xml_cache.rs        # Disk cache for .bin -> .xml conversions (MD5-keyed, mtime-invalidated)
│   │   ├── archive/                # ZIP / .ap archive access
│   │   │   └── mod.rs
│   │   └── platform/               # OS-specific logic
│   │       ├── mod.rs
│   │       ├── paths.rs            # Game dir detection, config/cache/log dirs, to_windows_path
│   │       └── settings.rs         # Persisted game path, theme, locale
│   ├── Cargo.toml
│   └── tauri.conf.json
├── package.json
├── vite.config.ts
└── MIGRATION.md                    # This file
```

---

## XML Parsing Strategy

The most important design decision. The C# app uses AngleSharp (full DOM) which loads entire documents into memory — a problem at 500MB+.

### Two-tier approach

**Tier 1 — Streaming projection** (large files: `Scenario.bin.xml`, `Tracks.bin.xml`, `SDBCache.bin.xml`)

`quick-xml` pull-parser with a state machine that emits domain objects directly from the event stream. Never holds more than one element subtree in memory.

```rust
// State machine: Idle -> InConsist -> InVehicle -> ...
// Emits a Consist when </cConsist> is reached
pub fn parse_scenario_consists(path: &Path) -> impl Iterator<Item = Result<Consist>>
```

**Tier 2 — In-memory string** (small files < 1MB: `ScenarioProperties.xml`, `RouteProperties.xml`, blueprint XMLs)

Read into a `String`, then query with the helpers in `xml/selectors.rs`. Simple and fast enough at this scale.

### XML Cache

```
~/.cache/railworks-forge/xml-cache/{md5_of_source_path}-{filename}.xml
```

Cache is valid when `mtime(xml) >= mtime(bin)`. Serz conversion is skipped on a hit.

---

## Tauri IPC Architecture

All heavy work stays in Rust. The Svelte frontend is a pure presentation layer.

```
Frontend (Svelte)        Tauri IPC              Rust backend
─────────────────        ─────────              ────────────
get_routes()        →    #[tauri::command]  →   route_service::get_routes()
get_scenarios(r)    →    #[tauri::command]  →   scenario_service::get_scenarios()
save_consist(req)   →    #[tauri::command]  →   persistence::save_consist()

Progress events     ←    Channel<T>         ←   channel.send(ProgressEvent)
```

Long-running operations (Serz conversion, route discovery) stream progress to the frontend via Tauri's typed `Channel<T>` API rather than blocking.

---

## Caching Strategy

Active cache layers:

| Cache | Storage | Key | Invalidation |
|---|---|---|---|
| XML file cache | Disk (`xml-cache/`) | MD5 of source path | mtime on source .bin |
| Scenario DB | Disk (`SDBCache.json`) | N/A | mtime on SDBCache.bin |

---

## Migration Phases

### Phase 1 — Foundation ✅ Complete

- [x] Tauri 2 + SvelteKit project scaffold (`src-rust/`)
- [x] `platform/paths.rs` — game directory detection (Windows registry + Steam library + settings.json fallback)
- [x] `archive/mod.rs` — ZIP / .ap archive reading (entry lookup, read, list, prefix filter)
- [x] `serz/process.rs` — Serz CLI bridge (native on Windows, Wine on Linux/macOS); result cached
- [x] `cache/xml_cache.rs` — disk cache with MD5 keys and mtime invalidation
- [x] All domain models: `Route`, `Scenario`, `Consist`, `Blueprint`, `VehicleBlueprint`
- [x] `xml/parser.rs` — `quick-xml` pull-parser helpers, async file reader
- [x] `xml/selectors.rs` — element query helpers (mirrors AngleSharp CSS selector extensions)
- [x] `services/route_service.rs` — route discovery + `RouteProperties.xml` parsing (packed + unpacked)
- [x] `services/scenario_service.rs` — scenario loading, packed/unpacked merge, deduplication
- [x] `services/scenario_db.rs` — streaming `SDBCache.bin.xml` parser + JSON cache
- [x] `services/asset_service.rs` — acquisition state checks, asset tree enumeration
- [x] All Tauri command stubs registered: routes, scenarios, consists, tracks, assets
- [x] Compiles clean (`cargo check` — 0 errors)

### Phase 2 — Core XML Parsing ✅ Complete

- [x] Streaming `quick-xml` state-machine parser for `Scenario.bin.xml` (`services/scenario_parser.rs`)
  - Depth-tracked state machine; emits `Consist` + `VehicleBlueprint` from event stream
  - Reads directly from file via `BufReader` — never loads full document into memory
- [x] `get_scenario_detail` command — Serz conversion (cached), packed extraction, consist parsing
- [x] Route detail page in Svelte (`routes/[routeId]/+page.svelte`) — scenario list with search
- [x] Scenario detail page in Svelte (`routes/[routeId]/scenarios/[scenarioId]/+page.svelte`) — consist + vehicle list
- [x] Routes list page (`routes/+page.svelte`) — replaces placeholder, calls `get_routes` with Channel progress
- [x] SPA mode enabled via `routes/+layout.ts` (`ssr = false`)

### Phase 3 — Consist Editing ✅ Complete

- [x] Command pattern: `DeleteVehicle`, `DeleteConsist`, `ReplaceVehicles` (`services/consist_commands.rs`)
- [x] `ScenarioEditor` — single-pass streaming XML editor applying consist commands (`services/scenario_editor.rs`)
- [x] `PersistenceService` — scenario backup creation, XML write-back via Serz, saved consist templates (`services/persistence.rs`)
- [x] `VehicleTemplates` + `VehicleGenerator` — embedded Engine/Wagon/Tender templates; generates vehicle XML with blueprint IDs + flipped flag (`services/vehicle_templates.rs`, `services/vehicle_generator.rs`)
- [x] `commands/consists.rs` — `replace_consist`, `add_vehicle`, `delete_vehicle`, `delete_consist`, `save_consist`, `get_saved_consists`, `delete_saved_consist`
- [x] Consist detail page (`routes/[routeId]/scenarios/[scenarioId]/consists/[consistId]/+page.svelte`) — vehicle table with per-vehicle delete, consist delete, replace consist dialog
- [x] Replace consist dialog — inline vehicle list editor, add vehicle form, save/load named templates
- [x] Scenario detail page updated with "Edit" link per consist card

### Phase 4 — Asset Management

- [x] `AssetDatabase` — provider/product directory tree with preload + RailVehicles flags
- [x] Asset browser page in Svelte
- [x] `TrackService` — streaming `Tracks.bin.xml` parser + replacement writer
- [x] Track replacement dialog

### Phase 5 — Polish ✅ Complete

- [x] i18n via Fluent message catalogs (English + German)
- [x] `tracing` with rolling file sink (replaces Serilog)
- [x] Settings page (game path, theme, language, XML cache clear)
- [x] Dark/light/system theme
- [x] Performance profiling spans for route/scenario/track parsing and edit operations
- [x] Removed stale scaffolding: unused XML writer, image cache, preload consist placeholders, and backend search helpers

---

## Key Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Serz binary format changes with game updates | All Serz interaction isolated behind `serz/process.rs`; easy to swap |
| Streaming XML parser complexity for deeply nested `Scenario.bin.xml` | Write integration tests against real game files before Phase 3 |
| Svelte virtual table for large consist/scenario lists | `tanstack-table` with virtual scrolling handles thousands of rows |
| Wine path translation edge cases on Linux/macOS | `to_windows_path()` matches existing C# `ToWindowsPath()` logic |

---

## What is Preserved from the C# App

- Multi-level caching strategy (structure and invalidation logic)
- Command pattern for consist modifications
- `AcquisitionState` (Found / Partial / Missing) check logic
- MD5-based cache path flattening
- Backup-before-modify pattern
- Packed/unpacked route and scenario merge + deduplication

## What is Improved

- DOM-based XML → streaming pull-parser for all large files
- Typed explicit parsers instead of CSS selector queries (eliminates runtime query errors)
- Stale image-cache scaffolding removed until route thumbnails/asset previews are implemented
- Cancellation support via Tauri async command lifecycle
- `MaxDegreeOfParallelism=8` hardcode → configurable `Semaphore` bound
