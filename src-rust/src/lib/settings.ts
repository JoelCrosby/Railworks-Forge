import { invoke } from '@tauri-apps/api/core';
import { writable } from 'svelte/store';
import type { Locale } from './i18n';
import { normalizeLocale } from './i18n';

export type Theme = 'dark' | 'light' | 'system';

export interface AppSettings {
	gamePath?: string | null;
	theme: Theme;
	locale: Locale;
}

const defaults: AppSettings = {
	gamePath: null,
	theme: 'dark',
	locale: 'en-US'
};

export const settings = writable<AppSettings>(defaults);

function normalizeSettings(value: Partial<AppSettings> | null | undefined): AppSettings {
	const theme = value?.theme === 'light' || value?.theme === 'system' ? value.theme : 'dark';
	return {
		gamePath: value?.gamePath ?? null,
		theme,
		locale: normalizeLocale(value?.locale)
	};
}

export async function loadSettings(): Promise<AppSettings> {
	const loaded = normalizeSettings(await invoke<AppSettings>('get_settings'));
	settings.set(loaded);
	applyTheme(loaded.theme);
	return loaded;
}

export async function saveSettings(next: AppSettings): Promise<AppSettings> {
	const saved = normalizeSettings(await invoke<AppSettings>('save_settings', { settings: next }));
	settings.set(saved);
	applyTheme(saved.theme);
	return saved;
}

export function applyTheme(theme: Theme): void {
	if (typeof document === 'undefined') return;
	const systemDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? true;
	const resolvedTheme = theme === 'system' ? (systemDark ? 'dark' : 'light') : theme;
	document.documentElement.classList.toggle('dark', resolvedTheme === 'dark');
	document.documentElement.style.colorScheme = resolvedTheme;
	document.documentElement.removeAttribute('data-theme');
}
