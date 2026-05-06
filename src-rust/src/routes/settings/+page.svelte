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

<div class="mx-auto max-w-[760px] p-6">
	<nav class="mb-4">
		<button
			class="cursor-pointer border-0 bg-transparent p-0 text-sm text-[var(--accent)] hover:underline"
			onclick={() => goto('/')}>← {t(locale, 'nav-routes')}</button
		>
	</nav>

	<header class="mb-6">
		<h1 class="text-[1.35rem] font-bold">{t(locale, 'settings-title')}</h1>
	</header>

	{#if error}
		<div class="mb-4 rounded-md border border-[var(--danger-border)] bg-[var(--danger-surface)] px-4 py-3 text-sm text-[var(--danger-text)]"><strong>{t(locale, 'error-label')}:</strong> {error}</div>
	{/if}
	{#if success}
		<div class="mb-4 rounded-md border border-[var(--success-border)] bg-[var(--success-surface)] px-4 py-3 text-sm text-[var(--success-text)]">{success}</div>
	{/if}

	{#if loading}
		<div class="text-center text-[var(--muted)]">{t(locale, 'action-loading')}</div>
	{:else}
		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-[var(--border)] bg-[var(--surface)] p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-game-path')}</h2>
			<p class="text-[0.82rem] text-[var(--muted)]">{t(locale, 'settings-game-path-hint')}</p>
			<input
				class="rounded-md border border-[var(--border-strong)] bg-[var(--bg)] px-2.5 py-2 text-[var(--text)] outline-none focus:border-[var(--accent)]"
				bind:value={form.gamePath}
				placeholder="/path/to/RailWorks"
			/>
			{#if form.gamePath}
				<p class="text-[0.82rem] text-[var(--muted)]">{t(locale, 'settings-current-path', { path: form.gamePath })}</p>
			{/if}
		</section>

		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-[var(--border)] bg-[var(--surface)] p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-theme')}</h2>
			<div class="grid grid-cols-3 gap-1.5">
				<button
					class={`cursor-pointer rounded-md border px-3.5 py-2 ${form.theme === 'dark' ? 'border-[var(--accent-border)] bg-[var(--accent-surface)] text-[var(--accent-text)]' : 'border-[var(--border-strong)] bg-[var(--surface-raised)] text-[var(--text)] hover:border-[var(--accent-border)] hover:bg-[var(--accent-surface)] hover:text-[var(--accent-text)]'}`}
					onclick={() => setTheme('dark')}
				>
					{t(locale, 'settings-theme-dark')}
				</button>
				<button
					class={`cursor-pointer rounded-md border px-3.5 py-2 ${form.theme === 'light' ? 'border-[var(--accent-border)] bg-[var(--accent-surface)] text-[var(--accent-text)]' : 'border-[var(--border-strong)] bg-[var(--surface-raised)] text-[var(--text)] hover:border-[var(--accent-border)] hover:bg-[var(--accent-surface)] hover:text-[var(--accent-text)]'}`}
					onclick={() => setTheme('light')}
				>
					{t(locale, 'settings-theme-light')}
				</button>
				<button
					class={`cursor-pointer rounded-md border px-3.5 py-2 ${form.theme === 'system' ? 'border-[var(--accent-border)] bg-[var(--accent-surface)] text-[var(--accent-text)]' : 'border-[var(--border-strong)] bg-[var(--surface-raised)] text-[var(--text)] hover:border-[var(--accent-border)] hover:bg-[var(--accent-surface)] hover:text-[var(--accent-text)]'}`}
					onclick={() => setTheme('system')}
				>
					{t(locale, 'settings-theme-system')}
				</button>
			</div>
		</section>

		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-[var(--border)] bg-[var(--surface)] p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-language')}</h2>
			<select
				class="rounded-md border border-[var(--border-strong)] bg-[var(--bg)] px-2.5 py-2 text-[var(--text)] outline-none focus:border-[var(--accent)]"
				bind:value={form.locale}
			>
				<option value="en-US">{t(locale, 'settings-language-english')}</option>
				<option value="de-DE">{t(locale, 'settings-language-german')}</option>
			</select>
		</section>

		<section class="mb-4 flex flex-col gap-2.5 rounded-lg border border-[var(--border)] bg-[var(--surface)] p-4">
			<h2 class="text-[0.95rem] font-semibold">{t(locale, 'settings-cache')}</h2>
			<button
				class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-3.5 py-2 text-[var(--text)] hover:border-[var(--accent-border)] hover:bg-[var(--accent-surface)] hover:text-[var(--accent-text)] disabled:cursor-not-allowed disabled:opacity-55"
				onclick={clearCache}
				disabled={clearing}
			>
				{clearing ? t(locale, 'action-loading') : t(locale, 'settings-clear-cache')}
			</button>
		</section>

		<footer class="flex justify-end gap-2">
			<button
				class="cursor-pointer rounded-md border border-[var(--primary-border)] bg-[var(--primary)] px-3.5 py-2 text-white hover:bg-[var(--primary-hover)] disabled:cursor-not-allowed disabled:opacity-55"
				onclick={save}
				disabled={saving}
			>
				{saving ? t(locale, 'action-saving') : t(locale, 'action-save')}
			</button>
			<button
				class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-3.5 py-2 text-[var(--text)] hover:border-[var(--accent-border)] hover:bg-[var(--accent-surface)] hover:text-[var(--accent-text)]"
				onclick={() => goto('/')}>{t(locale, 'action-cancel')}</button
			>
		</footer>
	{/if}
</div>
