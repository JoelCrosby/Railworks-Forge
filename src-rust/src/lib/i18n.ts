import { FluentBundle, FluentResource } from '@fluent/bundle';
import type { FluentVariable } from '@fluent/bundle';

export type Locale = 'en-US' | 'de-DE';

const catalogs: Record<Locale, string> = {
	'en-US': `
app-title = Railworks Forge
nav-routes = Routes
nav-assets = Assets
nav-settings = Settings
action-refresh = Refresh
action-loading = Loading...
action-save = Save
action-saving = Saving...
action-cancel = Cancel
action-clear = Clear
action-back = Back
error-label = Error
status-loading-player-data = Loading player data...
status-player-data-unavailable = Player data unavailable: { $message }
home-scanning-routes = Scanning routes...
home-no-routes = No routes found. Check your game path in settings.
home-game-path-missing = Game path is not configured. Open settings to set it.
home-opening = opening
settings-title = Settings
settings-game-path = Game Path
settings-game-path-hint = Full path to your Train Simulator / Railworks installation directory
settings-current-path = Current: { $path }
settings-theme = Theme
settings-theme-dark = Dark
settings-theme-light = Light
settings-theme-system = System
settings-language = Language
settings-language-english = English
settings-language-german = German
settings-cache = Cache
settings-clear-cache = Clear XML cache
settings-cache-cleared = XML cache cleared.
settings-saved = Settings saved.
assets-title = Asset Browser
assets-search = Search provider or product...
assets-railvehicles-only = RailVehicles only
assets-scanning = Scanning assets...
assets-no-assets = No assets found.
assets-no-matches = No matches for current filter.
route-tracks = Tracks
route-search-scenarios = Search scenarios...
route-opening = Opening route...
route-loading-scenarios = Loading scenarios...
route-no-scenarios = No scenarios found for this route.
`,
	'de-DE': `
app-title = Railworks Forge
nav-routes = Strecken
nav-assets = Assets
nav-settings = Einstellungen
action-refresh = Aktualisieren
action-loading = Lädt...
action-save = Speichern
action-saving = Speichert...
action-cancel = Abbrechen
action-clear = Leeren
action-back = Zurück
error-label = Fehler
status-loading-player-data = Spielerdaten werden geladen...
status-player-data-unavailable = Spielerdaten nicht verfügbar: { $message }
home-scanning-routes = Strecken werden gesucht...
home-no-routes = Keine Strecken gefunden. Prüfe den Spielpfad in den Einstellungen.
home-game-path-missing = Der Spielpfad ist nicht eingerichtet. Öffne die Einstellungen.
home-opening = öffnet
settings-title = Einstellungen
settings-game-path = Spielpfad
settings-game-path-hint = Vollständiger Pfad zu deiner Train-Simulator-/Railworks-Installation
settings-current-path = Aktuell: { $path }
settings-theme = Design
settings-theme-dark = Dunkel
settings-theme-light = Hell
settings-theme-system = System
settings-language = Sprache
settings-language-english = Englisch
settings-language-german = Deutsch
settings-cache = Cache
settings-clear-cache = XML-Cache leeren
settings-cache-cleared = XML-Cache geleert.
settings-saved = Einstellungen gespeichert.
assets-title = Asset-Browser
assets-search = Anbieter oder Produkt suchen...
assets-railvehicles-only = Nur RailVehicles
assets-scanning = Assets werden gesucht...
assets-no-assets = Keine Assets gefunden.
assets-no-matches = Keine Treffer für den aktuellen Filter.
route-tracks = Gleise
route-search-scenarios = Szenarien suchen...
route-opening = Strecke wird geöffnet...
route-loading-scenarios = Szenarien werden geladen...
route-no-scenarios = Keine Szenarien für diese Strecke gefunden.
`
};

const bundles = Object.fromEntries(
	Object.entries(catalogs).map(([locale, source]) => {
		const bundle = new FluentBundle(locale);
		bundle.addResource(new FluentResource(source));
		return [locale, bundle];
	})
) as Record<Locale, FluentBundle>;

export function normalizeLocale(locale: string | null | undefined): Locale {
	return locale === 'de-DE' ? 'de-DE' : 'en-US';
}

export function t(locale: string | null | undefined, key: string, args?: Record<string, FluentVariable>): string {
	const normalized = normalizeLocale(locale);
	const bundle = bundles[normalized];
	const message = bundle.getMessage(key) ?? bundles['en-US'].getMessage(key);
	if (!message?.value) return key;
	return bundle.formatPattern(message.value, args);
}
