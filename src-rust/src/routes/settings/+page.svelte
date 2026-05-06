<script lang="ts">
	import { invoke } from '@tauri-apps/api/core';
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { applyTheme, loadSettings, saveSettings, settings, type AppSettings, type Theme } from '$lib/settings';
	import Breadcrumb from '$lib/components/Breadcrumb.svelte';

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

<div class="mx-auto max-w-190 p-6">
	<Breadcrumb items={[
		{ label: t(locale, 'nav-routes'), onclick: () => goto('/') },
		{ label: t(locale, 'nav-settings') }
	]} />

	<header class="mb-6">
		<h1 class="text-[1.35rem] font-bold">{t(locale, 'settings-title')}</h1>
	</header>

	{#if error}
		<div class="mb-4 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"><strong>{t(locale, 'error-label')}:</strong> {error}</div>
	{/if}
	{#if success}
		<div class="mb-4 rounded-md border border-success-border bg-success-surface px-4 py-3 text-sm text-success-text">{success}</div>
	{/if}

	{#if loading}
		<div class="text-center text-muted">{t(locale, 'action-loading')}</div>
	{:else}
		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-border bg-surface p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-game-path')}</h2>
			<p class="text-[0.82rem] text-muted">{t(locale, 'settings-game-path-hint')}</p>
			<input
				class="rounded-md border border-border-strong bg-bg px-2.5 py-2 text-text outline-none focus:border-accent"
				bind:value={form.gamePath}
				placeholder="/path/to/RailWorks"
			/>
			{#if form.gamePath}
				<p class="text-[0.82rem] text-muted">{t(locale, 'settings-current-path', { path: form.gamePath })}</p>
			{/if}
		</section>

		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-border bg-surface p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-theme')}</h2>
			<div class="grid grid-cols-3 gap-1.5">
				<button
					class={`cursor-pointer rounded-md border px-3.5 py-2 ${form.theme === 'dark' ? 'border-accent-border bg-accent-surface text-accent-text' : 'border-border-strong bg-surface-raised text-text hover:border-accent-border hover:bg-accent-surface hover:text-accent-text'}`}
					onclick={() => setTheme('dark')}
				>
					{t(locale, 'settings-theme-dark')}
				</button>
				<button
					class={`cursor-pointer rounded-md border px-3.5 py-2 ${form.theme === 'light' ? 'border-accent-border bg-accent-surface text-accent-text' : 'border-border-strong bg-surface-raised text-text hover:border-accent-border hover:bg-accent-surface hover:text-accent-text'}`}
					onclick={() => setTheme('light')}
				>
					{t(locale, 'settings-theme-light')}
				</button>
				<button
					class={`cursor-pointer rounded-md border px-3.5 py-2 ${form.theme === 'system' ? 'border-accent-border bg-accent-surface text-accent-text' : 'border-border-strong bg-surface-raised text-text hover:border-accent-border hover:bg-accent-surface hover:text-accent-text'}`}
					onclick={() => setTheme('system')}
				>
					{t(locale, 'settings-theme-system')}
				</button>
			</div>
		</section>

		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-border bg-surface p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-language')}</h2>
			<select
				class="rounded-md border border-border-strong bg-bg px-2.5 py-2 text-text outline-none focus:border-accent"
				bind:value={form.locale}
			>
				<option value="en-US">{t(locale, 'settings-language-english')}</option>
				<option value="de-DE">{t(locale, 'settings-language-german')}</option>
			</select>
		</section>

		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-border bg-surface p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-cache')}</h2>
			<button
				class="cursor-pointer rounded-md border border-border-strong bg-surface-raised px-3.5 py-2 text-text hover:border-accent-border hover:bg-accent-surface hover:text-accent-text disabled:cursor-not-allowed disabled:opacity-55"
				onclick={clearCache}
				disabled={clearing}
			>
				{clearing ? t(locale, 'action-loading') : t(locale, 'settings-clear-cache')}
			</button>
		</section>

		<footer class="flex justify-end gap-2">
			<button
				class="cursor-pointer rounded-md border border-primary-border bg-primary px-3.5 py-2 text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-55"
				onclick={save}
				disabled={saving}
			>
				{saving ? t(locale, 'action-saving') : t(locale, 'action-save')}
			</button>
			<button
				class="cursor-pointer rounded-md border border-border-strong bg-surface-raised px-3.5 py-2 text-text hover:border-accent-border hover:bg-accent-surface hover:text-accent-text"
				onclick={() => goto('/')}>{t(locale, 'action-cancel')}</button
			>
		</footer>
	{/if}
</div>
