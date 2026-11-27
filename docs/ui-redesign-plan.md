# UI redesign and component library plan

Mockups: the "Railworks Forge Redesign" design canvas (scenario detail, consist editor, route detail, routes library).

## Where the code is now

- **Some reusable controls already exist.** Seven templated controls in `Views/TemplatedControls/` (`TableHeader`, `TableSearchBox`, `BlueprintSummary`, `VehicleThumbnail`, …) have styled properties, no view-model types and a `Design.PreviewWith`. That is the pattern to copy.
- **Pages repeat the same markup.**
  - The unsaved-changes bar is copied in `ScenarioDetailPage.axaml` and `ConsistDetailPage.axaml`.
  - Header-card `Border`s use hard-coded padding and radius.
  - Settings defines its own `card`, `row` and `section` styles locally.
  - `NavigationBar.axaml` is mostly local styles (`Button.flat`, breadcrumbs) that other screens need too.
- **There are no shared design tokens.** `Colours.axaml` is mostly `Header*` brushes, `Typography.axaml` has three font sizes, and spacing and radii are written inline. The status bar uses `StaticResource SystemBaseHighColor` with white text, so it stays black in the light theme.
- **Some features in the mockups need Core work.** `IScenarioEditSession` has add, replace and delete, but no move, flip or list of changes.

## Rules for components

1. **Two kinds of view.**
   - **Components** (`Views/Components/`) are `TemplatedControl`s. They have no DataContext, no view-model types and no `echoes:Translate` inside; text, commands and items come in as styled properties. Each one has a `.axaml` style with a `Design.PreviewWith` and a `.cs` file.
   - **Views** (pages, the shell, dialogs) are `UserControl`s that bind one view model and only compose components.
2. Components expose `ICommand` properties for actions, `[Content]` or named slots where callers supply content, and pseudo-classes for visual state (`:selected`, `:flipped`, `:empty`).
3. Components use only `DynamicResource` token keys, never hex values or `System*` brushes.
4. All components are registered through one `Styles/Components.axaml`.
5. An `ArchitectureTests` rule enforces that `Views.Components` does not reference `RailworksForge.ViewModels` or `Echoes`.

## Phase 1: Tokens and base styles

- `Styles/Tokens.axaml`: surface, border, text, accent, danger, warning and success brushes for light and dark, plus spacing, radii and the type ramp. Folds in `Colours.axaml` and `Typography.axaml`.
- `Styles/Controls.axaml`: button classes `primary`, `danger`, `ghost`, `small`; `Border.card`; overflow `DropDownButton`; underline `TabControl`.
- Fix the light theme: button contrast and the status bar background.

## Phase 2: Component library

| Component | Replaces / used by | Main properties |
|---|---|---|
| `PageHeader` | Route and scenario header grids | `Image`, `Title`, `Badges` slot, `Description`, `Actions` slot |
| `Badge` | New | Content; classes `accent`, `success`, `warning` |
| `SectionHeader` | Grows out of `TableHeader` | `Title`, `Count`, `Tools` slot |
| `SearchBox` | Grows out of `TableSearchBox` | `Text`, `Placeholder`, `ShortcutHint`; Ctrl+F focuses it |
| `SelectionToolbar` | Actions only in context menus today | `SelectedCount`, `SelectionLabel`, `ClearCommand`, actions slot |
| `PendingChangesBar` | The bar copied on two pages | `Summary`, `Details`, `DiscardCommand`, `ApplyCommand`, `ApplyText` |
| `Thumbnail` | Grows out of `VehicleThumbnail` | `Source`, placeholder icon while loading |
| `ConsistStrip` + `VehicleCard` | Consist editor | `ItemsSource`, `SelectedItem`; card has `:flipped` and `:selected` |
| `RouteCard` | `RoutesGrid` item template | `Image`, `Title`, `Subtitle`; class `compact` for the recent row |
| `EmptyState` | Blank grids and dialog panes | `Icon`, `Title`, `Hint` |
| `SettingsSection` + `SettingsRow` | Local styles in Settings | `Title`, `Description`, content slot |
| `TaskIndicator` | Status bar background work | `Label`, `Progress`, `IsIndeterminate` |
| `Breadcrumb` | Inline markup in `NavigationBar` | crumbs (icon, brush, text, current, dirty), `NavigateCommand` |

`AcquisitionStateIcon`, `BlueprintSummary`, `LoadingSpinner` and `NameValueDisplay` move into `Views/Components/`.

## Phase 3: Shell

- `NavigationService`: back/forward history; Settings shows a single crumb; the scenario crumb shows when it has unsaved changes.
- `NavigationBar`: rebuilt from `Breadcrumb` and ghost buttons. Page-specific search and layout controls move onto their pages.
- Status bar: shows background tasks and page context (for example "12 services · 2 selected") instead of the game path.
- Key bindings: Alt+←/→ back and forward, Ctrl+F search; pages handle Enter, Delete, Escape.

## Phase 4: Pages

1. **Scenario detail**: `PageHeader` with Open folder and a More menu for file utilities; `SelectionToolbar` (Open, Replace consist, Save as preset, Delete); `PendingChangesBar` listing the changes.
2. **Route detail**: `PageHeader` with scenario and completed counts; Route tools menu with Replace track in its own section; completion and season filters; Rating, Score and Packaging columns removed.
3. **Routes library**: `RouteCard` grid that fills the width; "Recently opened" row (`RecentRoutes` in settings); sort by name or recently opened; search on the page. No publisher filter pills.
4. **Consist editor**: `ConsistStrip` above the vehicle table; right panel with tabs (Browse stock, Search all vehicles); stock loads when a folder is selected; `SelectionToolbar` with Move, Flip, Replace, Delete; vehicle names fall back to the blueprint file name.
5. **Settings and dialogs**: Settings rebuilt from `SettingsSection`/`SettingsRow`; `EmptyState` in dialogs.

## Phase 5: Core support

- `IScenarioEditSession.MoveVehicle` and `FlipVehicle`, with tests.
- The session records structured `ScenarioChange` entries so the pending-changes bar can say what changed. Core reports the structure; the UI formats it through Echoes.
- Undo is out of scope for now.

## Cross-cutting

- Every new string goes into the en, de, es and fr translation files.
- Component render tests with Avalonia.Headless; view-model tests with NSubstitute.

## Order

Phase 1 → Phase 2 → scenario detail → shell → route detail → routes → Phase 5 → consist editor → settings and dialogs. Each step leaves the app working.

## Implementation notes

Where the build differs from the plan above:

- **Back** steps up one level of the trail, or out of Settings to the page it was opened from. There is no forward history, because forward would have to replay the unsaved-edits prompts.
- **Status bar** shows the scenario database load as its background task. The vehicle index still reports progress in its own dialog.
- **Selection** uses the grids' existing Ctrl/Shift multi-select; there is no checkbox column.
- **Route detail** drops the Rating, Score and Packaging columns rather than adding a column picker.
- **Route cards** show the full route name; there is no publisher tag.
- **Moving a vehicle** also reorders the driver's `InitialRV` numbers, but only when that list matches the consist's vehicles exactly; otherwise it is left as it was.
- **Styling `LucideIcon` and `ConsistStrip`**: both keep a base control's template through `StyleKeyOverride`, so type selectors for them never match. Select `lucideAvalonia|Lucide` for icons, and `ListBox.consist-strip` (the class `ConsistStrip` adds to itself) for the strip.
- **Tests that create Avalonia objects** must run through `ComponentSession` in the `Avalonia UI` collection. The headless session owns `Dispatcher.UIThread`, and anything that reads it from another thread first breaks the session.
