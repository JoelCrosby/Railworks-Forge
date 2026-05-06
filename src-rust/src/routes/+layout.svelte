<script lang="ts">
	import '../app.css';
	import { listen } from '@tauri-apps/api/event';
	import favicon from '$lib/assets/favicon.svg';
	import { t } from '$lib/i18n';
	import { applyTheme, loadSettings, settings } from '$lib/settings';

	let { children } = $props();

	type DbStatus =
		| { status: 'loading' }
		| { status: 'ready' }
		| { status: 'failed'; message: string };

	let dbStatus = $state<DbStatus | null>(null);
	let locale = $derived($settings.locale);

	$effect(() => {
		loadSettings().catch(() => applyTheme('dark'));

		const themeWatcher = window.matchMedia?.('(prefers-color-scheme: dark)');
		const onThemeChange = () => applyTheme($settings.theme);
		themeWatcher?.addEventListener('change', onThemeChange);

		const unlisten = listen<DbStatus>('scenario-db-status', (event) => {
			dbStatus = event.payload;
			if (event.payload.status === 'ready') {
				setTimeout(() => {
					dbStatus = null;
				}, 2000);
			}
		});
		return () => {
			unlisten.then((fn) => fn());
			themeWatcher?.removeEventListener('change', onThemeChange);
		};
	});
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

{@render children()}

{#if dbStatus !== null && dbStatus.status !== 'ready'}
	<div
		class={`fixed right-4 bottom-4 z-100 flex max-w-md items-center gap-2 rounded-md border bg-surface px-3 py-1.5 text-xs text-muted-strong ${dbStatus.status === 'failed' ? 'border-danger-border text-danger-text' : 'border-border'}`}
	>
		{#if dbStatus.status === 'loading'}
			<span class="size-1.5 shrink-0 animate-pulse rounded-full bg-accent"></span> {t(locale, 'status-loading-player-data')}
		{:else}
			<span class="size-1.5 shrink-0 rounded-full bg-danger-text"></span> {t(locale, 'status-player-data-unavailable', { message: dbStatus.message })}
			<button
				class="ml-auto cursor-pointer border-0 bg-transparent px-0.5 text-base leading-none text-inherit opacity-60 hover:opacity-100"
				aria-label="Dismiss"
				onclick={() => (dbStatus = null)}>x</button
			>
		{/if}
	</div>
{/if}
