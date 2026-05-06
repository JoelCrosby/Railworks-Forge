<script lang="ts">
	import { invoke } from '@tauri-apps/api/core';
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { applyTheme, loadSettings, saveSettings, settings, type AppSettings, type Theme } from '$lib/settings';

	let form = $state<AppSettings>({ gamePath: null, theme: 'dark', locale: 'en-US' });
	let loading = $state(true);
	let saving = $state(false);
	let clearing = $state(false);
	let error = $state<string | null>(null);
	let success = $state<string | null>(null);
	let locale = $derived($settings.locale);

	async function load() {
		loading = true;
		error = null;
		try {
			form = await loadSettings();
		} catch (e) {
			error = String(e);
		} finally {
			loading = false;
		}
	}

	async function save() {
		saving = true;
		error = null;
		success = null;
		try {
			form = await saveSettings({
				...form,
				gamePath: form.gamePath?.trim() || null
			});
			success = t(form.locale, 'settings-saved');
		} catch (e) {
			error = String(e);
		} finally {
			saving = false;
		}
	}

	async function clearCache() {
		clearing = true;
		error = null;
		success = null;
		try {
			await invoke('clear_xml_cache');
			success = t(locale, 'settings-cache-cleared');
		} catch (e) {
			error = String(e);
		} finally {
			clearing = false;
		}
	}

	function setTheme(theme: Theme) {
		form.theme = theme;
		applyTheme(theme);
	}

	$effect(() => {
		load();
	});
</script>

<div class="page">
	<nav>
		<button class="back" onclick={() => goto('/')}>← {t(locale, 'nav-routes')}</button>
	</nav>

	<header>
		<h1>{t(locale, 'settings-title')}</h1>
	</header>

	{#if error}
		<div class="banner error"><strong>{t(locale, 'error-label')}:</strong> {error}</div>
	{/if}
	{#if success}
		<div class="banner success">{success}</div>
	{/if}

	{#if loading}
		<div class="status">{t(locale, 'action-loading')}</div>
	{:else}
		<section>
			<h2>{t(locale, 'settings-game-path')}</h2>
			<p>{t(locale, 'settings-game-path-hint')}</p>
			<input class="path-input" bind:value={form.gamePath} placeholder="/path/to/RailWorks" />
			{#if form.gamePath}
				<p class="current">{t(locale, 'settings-current-path', { path: form.gamePath })}</p>
			{/if}
		</section>

		<section>
			<h2>{t(locale, 'settings-theme')}</h2>
			<div class="segmented">
				<button class:active={form.theme === 'dark'} onclick={() => setTheme('dark')}>
					{t(locale, 'settings-theme-dark')}
				</button>
				<button class:active={form.theme === 'light'} onclick={() => setTheme('light')}>
					{t(locale, 'settings-theme-light')}
				</button>
				<button class:active={form.theme === 'system'} onclick={() => setTheme('system')}>
					{t(locale, 'settings-theme-system')}
				</button>
			</div>
		</section>

		<section>
			<h2>{t(locale, 'settings-language')}</h2>
			<select bind:value={form.locale}>
				<option value="en-US">{t(locale, 'settings-language-english')}</option>
				<option value="de-DE">{t(locale, 'settings-language-german')}</option>
			</select>
		</section>

		<section>
			<h2>{t(locale, 'settings-cache')}</h2>
			<button onclick={clearCache} disabled={clearing}>
				{clearing ? t(locale, 'action-loading') : t(locale, 'settings-clear-cache')}
			</button>
		</section>

		<footer>
			<button class="btn-primary" onclick={save} disabled={saving}>
				{saving ? t(locale, 'action-saving') : t(locale, 'action-save')}
			</button>
			<button onclick={() => goto('/')}>{t(locale, 'action-cancel')}</button>
		</footer>
	{/if}
</div>

<style>
	.page {
		max-width: 760px;
		margin: 0 auto;
		padding: 1.5rem;
	}

	nav {
		margin-bottom: 1rem;
	}

	.back {
		background: none;
		border: none;
		color: var(--accent);
		font-size: 0.875rem;
		cursor: pointer;
		padding: 0;
	}

	header {
		margin-bottom: 1.5rem;
	}

	h1 {
		font-size: 1.35rem;
	}

	section {
		background: var(--surface);
		border: 1px solid var(--border);
		border-radius: 8px;
		padding: 1rem;
		margin-bottom: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.65rem;
	}

	h2 {
		font-size: 0.95rem;
	}

	p,
	.current {
		color: var(--muted);
		font-size: 0.82rem;
	}

	input,
	select {
		background: var(--bg);
		border: 1px solid var(--border-strong);
		border-radius: 6px;
		color: var(--text);
		padding: 0.45rem 0.65rem;
	}

	input:focus,
	select:focus {
		border-color: var(--accent);
		outline: none;
	}

	.segmented {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		gap: 0.35rem;
	}

	button {
		background: var(--surface-raised);
		color: var(--text);
		border: 1px solid var(--border-strong);
		border-radius: 6px;
		padding: 0.45rem 0.85rem;
		cursor: pointer;
	}

	button:hover:not(:disabled),
	button.active {
		background: var(--accent-surface);
		border-color: var(--accent-border);
		color: var(--accent-text);
	}

	button:disabled {
		opacity: 0.55;
		cursor: not-allowed;
	}

	.btn-primary {
		background: var(--primary);
		border-color: var(--primary-border);
		color: #fff;
	}

	.btn-primary:hover:not(:disabled) {
		background: var(--primary-hover);
		color: #fff;
	}

	.banner {
		border-radius: 6px;
		padding: 0.75rem 1rem;
		font-size: 0.875rem;
		margin-bottom: 1rem;
	}

	.banner.success {
		background: var(--success-surface);
		border: 1px solid var(--success-border);
		color: var(--success-text);
	}

	.banner.error {
		background: var(--danger-surface);
		border: 1px solid var(--danger-border);
		color: var(--danger-text);
	}

	.status {
		color: var(--muted);
		text-align: center;
	}

	footer {
		display: flex;
		justify-content: flex-end;
		gap: 0.5rem;
	}
</style>
