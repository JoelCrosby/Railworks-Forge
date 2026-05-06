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
	<div class="db-status {dbStatus.status}">
		{#if dbStatus.status === 'loading'}
			<span class="dot"></span> {t(locale, 'status-loading-player-data')}
		{:else}
			<span class="dot"></span> {t(locale, 'status-player-data-unavailable', { message: dbStatus.message })}
			<button aria-label="Dismiss" onclick={() => (dbStatus = null)}>x</button>
		{/if}
	</div>
{/if}

<style>
	.db-status {
		position: fixed;
		bottom: 1rem;
		right: 1rem;
		background: var(--surface);
		border: 1px solid var(--border);
		border-radius: 6px;
		padding: 0.4rem 0.75rem;
		font-size: 0.78rem;
		color: var(--muted-strong);
		display: flex;
		align-items: center;
		gap: 0.5rem;
		z-index: 100;
		max-width: 28rem;
	}

	.db-status.failed {
		border-color: var(--danger-border);
		color: var(--danger-text);
	}

	.dot {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: var(--accent);
		flex-shrink: 0;
		animation: pulse 1.2s ease-in-out infinite;
	}

	.failed .dot {
		background: var(--danger-text);
		animation: none;
	}

	@keyframes pulse {
		0%,
		100% {
			opacity: 1;
		}
		50% {
			opacity: 0.3;
		}
	}

	button {
		background: none;
		border: none;
		color: inherit;
		cursor: pointer;
		padding: 0 0.1rem;
		font-size: 1rem;
		line-height: 1;
		opacity: 0.6;
		margin-left: auto;
	}

	button:hover {
		opacity: 1;
	}
</style>
