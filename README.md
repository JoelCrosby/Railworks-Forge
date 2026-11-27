[![.NET Core Desktop](https://github.com/JoelCrosby/Railworks-Forge/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/JoelCrosby/Railworks-Forge/actions/workflows/dotnet-desktop.yml)

# Railworks Forge

A desktop app for managing Train Simulator (RailWorks) routes, scenarios, consists and assets. It runs on Linux and Windows.

## Features

- Browse installed routes and their scenarios, with search and your completion, score and medals for each scenario.
- Edit scenario consists. You can replace a consist with a saved or Quick Drive consist, swap or delete vehicles, and delete services.
- Save consists as presets and replace track rules across a route.
- Check whether a route or scenario has every asset it needs, and search an index of installed vehicles.
- Install `.rwp` and `.rpk` packages.
- Convert between `.bin` and `.xml`, export scenario XML and extract packed scenarios. Scenario and track edits are backed up first.

## Requirements

- A RailWorks installation that contains the `Assets` and `Content` folders. On Windows it is found through the registry; otherwise, choose it in Settings.
- `serz64.exe` from that installation to convert XML back to BIN. On Linux it runs through Wine. BIN-to-XML conversion is built in and needs neither (see [docs/serz-performance.md](docs/serz-performance.md)).

## Building

The app uses .NET 11 and Avalonia.

```bash
dotnet build RailworksForge.slnx
dotnet test RailworksForge.slnx
dotnet run --project src/RailworksForge
```

CI builds and tests the solution on Windows in Debug and Release.

### Arch Linux

[build/PKGBUILD](build/PKGBUILD) builds the `railworks-forge-git` package from the GitHub repository. It installs the app to `/opt/railworks-forge` along with a desktop entry.

```bash
build/install.sh
```

The script runs `makepkg -si` on a copy of the PKGBUILD in a temporary directory and deletes it afterwards, so the clone, build trees and package file don't pile up in `build/`. Extra arguments are passed to `makepkg`.

## Project layout

| Path | Contents |
| --- | --- |
| `src/RailworksForge` | Avalonia UI: views, view models, dialogs |
| `src/RailworksForge.Core` | UI-independent logic, grouped by feature (routes, scenarios, consists, assets, packaging, serialization) |
| `src/RailworksForge.Translations` | UI strings in English, German, Spanish and French |
| `tests/` | xUnit tests for Core and the view models |
| `tools/SerzBenchmark` | Benchmarks the internal BIN converter against `serz64.exe` |

## Files

Settings are stored in `~/.config/RailworksForge`, which also holds the logs. Caches are kept in `~/.cache/RailworksForge`. On Windows both folders are under the user's home folder.
