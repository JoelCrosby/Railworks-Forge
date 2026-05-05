# Railworks Forge — Rust/Tauri Rewrite

## Overview

Full rewrite of the C#/Avalonia desktop app in Rust + Tauri 2 + Svelte. The primary motivation is performance: `Scenario.bin` files expand to 500MB+ when deserialised, and the existing DOM-based XML parser (AngleSharp) loads the entire document into memory. The Rust rewrite uses a streaming pull-parser (`quick-xml`) that never holds more than one element subtree in memory.

---

## Technology Choices

| Role | Choice | Rationale |
|---|---|---|
| Language | Rust (stable) | Memory safety + zero-cost abstractions |
| GUI framework | Tauri 2.x | Native webview shell; Rust backend with JS/TS frontend |
| Frontend framework | SvelteKit | Lightest reactive framework, minimal bundle |
| Async runtime | Tokio | Industry standard; Tauri already uses it |
| XML parsing | `quick-xml` | Zero-copy, pull-based SAX-style, extremely fast |
| ZIP / .ap archives | `zip` crate | .ap files are standard ZIP |
| Concurrency | `dashmap` + `rayon` | Lock-free maps; data-parallelism for route/image loading |
| Serialisation | `serde` + `serde_json` | Config files, SDBCache JSON cache |
| Error handling | `anyhow` + `thiserror` | Application errors / library boundary errors |
| Logging | `tracing` + `tracing-subscriber` | Structured async-aware logging |
| Retry | `tokio-retry` | Replaces Polly |
| i18n | `fluent-rs` (Phase 5) | Mozilla Project Fluent |
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
│   │   │   └── assets.rs           # check_assets, get_asset_tree
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
│   │   │   ├── consist.rs          # Consist, LocoClass, ConsistAcquisitionState, PreloadConsist
│   │   │   ├── blueprint.rs        # Blueprint, AcquisitionState
│   │   │   └── vehicle.rs          # VehicleBlueprint, BlueprintType
│   │   ├── xml/                    # XML parsing layer
│   │   │   ├── mod.rs
│   │   │   ├── parser.rs           # quick-xml pull-parser helpers + async file reader
│   │   │   ├── selectors.rs        # Element query helpers (mirrors AngleSharp extensions)
│   │   │   └── writer.rs           # In-place XML element mutation
│   │   ├── serz/                   # Serz binary <-> XML bridge
│   │   │   ├── mod.rs
│   │   │   └── process.rs          # Spawns serz64.exe (native on Windows, Wine on Linux/macOS)
│   │   ├── cache/                  # Multi-level cache
│   │   │   ├── mod.rs
│   │   │   ├── xml_cache.rs        # Disk cache for .bin -> .xml conversions (MD5-keyed, mtime-invalidated)
│   │   │   └── image_cache.rs      # In-memory LRU bitmap cache (256 entries)
│   │   ├── archive/                # ZIP / .ap archive access
│   │   │   └── mod.rs
│   │   └── platform/               # OS-specific logic
│   │       ├── mod.rs
│   │       └── paths.rs            # Game dir detection, config/cache dirs, to_windows_path
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

Five cache layers mirroring the existing C# app:

| Cache | Storage | Key | Invalidation |
|---|---|---|---|
| XML file cache | Disk (`xml-cache/`) | MD5 of source path | mtime on source .bin |
| Scenario DB | Disk (`SDBCache.json`) | N/A | mtime on SDBCache.bin |
| Route assets | Disk (`route-assets/{id}.csv`) | Route ID | mtime on route dir |
| Blueprint | In-memory `DashMap` | BlueprintId | Session |
| Image | In-memory LRU (256 entries) | Archive path + entry name | LRU eviction |

---

## Migration Phases

### Phase 1 — Foundation ✅ Complete

- [x] Tauri 2 + SvelteKit project scaffold (`src-rust/`)
- [x] `platform/paths.rs` — game directory detection (Windows registry + Steam library + settings.json fallback)
- [x] `archive/mod.rs` — ZIP / .ap archive reading (entry lookup, read, list, prefix filter)
- [x] `serz/process.rs` — Serz CLI bridge (native on Windows, Wine on Linux/macOS); result cached
- [x] `cache/xml_cache.rs` — disk cache with MD5 keys and mtime invalidation
- [x] `cache/image_cache.rs` — in-memory LRU cache with thread-safe access
- [x] All domain models: `Route`, `Scenario`, `Consist`, `Blueprint`, `VehicleBlueprint`
- [x] `xml/parser.rs` — `quick-xml` pull-parser helpers, async file reader
- [x] `xml/selectors.rs` — element query helpers (mirrors AngleSharp CSS selector extensions)
- [x] `xml/writer.rs` — in-place element text mutation
- [x] `services/route_service.rs` — route discovery + `RouteProperties.xml` parsing (packed + unpacked)
- [x] `services/scenario_service.rs` — scenario loading, packed/unpacked merge, deduplication
- [x] `services/scenario_db.rs` — streaming `SDBCache.bin.xml` parser + JSON cache
- [x] `services/asset_service.rs` — acquisition state checks, asset tree enumeration
- [x] All Tauri command stubs registered: routes, scenarios, consists, tracks, assets
- [x] Compiles clean (`cargo check` — 0 errors)

### Phase 2 — Core XML Parsing

- [ ] Streaming `quick-xml` state-machine parser for `Scenario.bin.xml`
  - Emits `Consist` and `VehicleBlueprint` from the event stream
  - Benchmark against C# baseline (target: 3-5x faster on large files)
- [ ] `get_scenario_detail` command — triggers Serz conversion, parses consists
- [ ] Scenario detail page in Svelte
- [ ] Route detail page in Svelte (scenario list)

### Phase 3 — Consist Editing

- [ ] Command pattern: `ReplaceConsist`, `ReplaceConsistVehicles`, `AddConsistVehicle`, `DeleteConsist`, `DeleteConsistVehicle`
- [ ] `ConsistCommandRunner` — batched command execution + XML write-back
- [ ] `PersistenceService` — scenario backup creation, write-back via Serz
- [ ] `VehicleTemplates` + `VehicleGenerator` — preload consist templates
- [ ] Consist detail page in Svelte (vehicle list, add/delete/replace)
- [ ] Replace consist dialog

### Phase 4 — Asset Management

- [ ] `AssetDatabase` — provider/product directory tree with preload + RailVehicles flags
- [ ] Asset browser page in Svelte
- [ ] `TrackService` — streaming `Tracks.bin.xml` parser + replacement writer
- [ ] Track replacement dialog

### Phase 5 — Polish

- [ ] i18n via Fluent (English + German, matching existing translations)
- [ ] `tracing` with rolling file sink (replaces Serilog)
- [ ] Settings page (game path, theme)
- [ ] Dark/light theme
- [ ] Performance profiling — confirm streaming parser benchmarks

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
- LRU eviction on image cache (existing C# cache grows unbounded)
- Cancellation support via Tauri async command lifecycle
- `MaxDegreeOfParallelism=8` hardcode → configurable `Semaphore` bound
