<script lang="ts">
	import { invoke } from '@tauri-apps/api/core';
	import { Channel } from '@tauri-apps/api/core';
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { settings } from '$lib/settings';

	interface Route {
		id: string;
		name: string;
		description: string | null;
		directoryPath: string;
		packagingType: 'packed' | 'unpacked';
	}

	interface ProgressEvent {
		current: number;
		total: number;
		message: string;
	}

	let routes = $state<Route[]>([]);
	let loading = $state(false);
	let error = $state<string | null>(null);
	let progress = $state<string | null>(null);
	let openingRouteId = $state<string | null>(null);

	// Game path state
	let gamePathMissing = $state(false);
	let locale = $derived($settings.locale);

	const PATH_MISSING_HINT = 'could not locate railworks';

	function isPathMissingError(msg: string): boolean {
		return msg.toLowerCase().includes(PATH_MISSING_HINT);
	}

	async function loadRoutes() {
		loading = true;
		error = null;
		progress = null;
		routes = [];
		gamePathMissing = false;

		try {
			const channel = new Channel<ProgressEvent>();
			channel.onmessage = (msg) => {
				progress = msg.message;
			};

			routes = await invoke<Route[]>('get_routes', { onProgress: channel });
			progress = null;

			await invoke<string>('get_game_path').catch(() => null);
		} catch (e) {
			const msg = String(e);
			if (isPathMissingError(msg)) {
				gamePathMissing = true;
			} else {
				error = msg;
			}
		} finally {
			loading = false;
		}
	}

	async function openRoute(route: Route) {
		openingRouteId = route.id;
		error = null;
		try {
			await goto(`/routes/${encodeURIComponent(route.id)}`);
		} catch (e) {
			error = `Could not open route: ${String(e)}`;
			openingRouteId = null;
		}
	}

	$effect(() => {
		// Load current game path for display, then load routes
		invoke<string>('get_game_path')
			.then((p) => {
			})
			.catch(() => {})
			.finally(() => loadRoutes());
	});
</script>

<div class="page">
	<header>
		<h1>Railworks Forge</h1>
		<div class="header-actions">
			<button class="btn-icon" onclick={() => goto('/settings')} title={t(locale, 'nav-settings')}>⚙</button>
			<button class="btn-secondary" onclick={() => goto('/assets')}>{t(locale, 'nav-assets')}</button>
			<button onclick={loadRoutes} disabled={loading}>
				{loading ? t(locale, 'action-loading') : t(locale, 'action-refresh')}
			</button>
		</div>
	</header>

	{#if error}
		<div class="error">
			<strong>{t(locale, 'error-label')}:</strong> {error}
		</div>
	{/if}

	{#if gamePathMissing}
		<div class="error">
			{t(locale, 'home-game-path-missing')}
			<button class="inline-action" onclick={() => goto('/settings')}>{t(locale, 'nav-settings')}</button>
		</div>
	{/if}

	{#if loading}
		<div class="status">{progress ?? t(locale, 'home-scanning-routes')}</div>
	{:else if routes.length === 0 && !error && !gamePathMissing}
		<div class="empty">{t(locale, 'home-no-routes')}</div>
	{:else}
		<ul class="route-list">
			{#each routes as route (route.id)}
				<li>
					<button
						class="route-card"
						onclick={() => openRoute(route)}
						disabled={openingRouteId !== null}
					>
						<span class="route-name">{route.name}</span>
						{#if route.description}
							<span class="route-desc">{route.description}</span>
						{/if}
						<span class="badge {route.packagingType}">
							{openingRouteId === route.id ? t(locale, 'home-opening') : route.packagingType}
						</span>
					</button>
				</li>
			{/each}
		</ul>
	{/if}
</div>

<style>
	:global(*, *::before, *::after) {
		box-sizing: border-box;
		margin: 0;
		padding: 0;
	}

	:global(body) {
		font-family: system-ui, sans-serif;
		background: var(--bg);
		color: var(--text);
		height: 100vh;
		overflow-y: auto;
	}

	.page {
		max-width: 960px;
		margin: 0 auto;
		padding: 2rem 1.5rem;
	}

	header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 2rem;
	}

	.header-actions {
		display: flex;
		gap: 0.5rem;
		align-items: center;
	}

	h1 {
		font-size: 1.5rem;
		font-weight: 700;
		letter-spacing: -0.02em;
	}

	button {
		background: var(--surface-raised);
		color: var(--text);
		border: 1px solid var(--border-strong);
		border-radius: 6px;
		padding: 0.4rem 1rem;
		font-size: 0.875rem;
		cursor: pointer;
	}

	button:hover:not(:disabled) {
		background: var(--surface-hover);
	}

	button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.btn-primary {
		background: var(--primary);
		border-color: var(--primary-border);
		color: #fff;
	}

	.btn-primary:hover:not(:disabled) {
		background: var(--primary-hover);
	}

	.btn-secondary {
		background: var(--accent-surface);
		border-color: var(--accent-border);
		color: var(--accent-text);
	}

	.btn-secondary:hover:not(:disabled) {
		background: var(--accent-surface);
	}

	.btn-icon {
		background: none;
		border: 1px solid transparent;
		color: var(--muted);
		padding: 0.3rem 0.5rem;
		font-size: 1rem;
		line-height: 1;
	}

	.btn-icon:hover {
		color: var(--text);
		background: var(--surface-raised);
		border-color: var(--border-strong);
	}

	.status,
	.empty {
		color: var(--muted);
		font-size: 0.9rem;
		margin-top: 2rem;
		text-align: center;
	}

	.error {
		background: var(--danger-surface);
		border: 1px solid var(--danger-border);
		border-radius: 6px;
		padding: 0.75rem 1rem;
		font-size: 0.875rem;
		color: var(--danger-text);
		margin-bottom: 1.5rem;
	}

	.route-list {
		list-style: none;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.route-card {
		width: 100%;
		text-align: left;
		background: var(--surface);
		border: 1px solid var(--surface-raised);
		border-radius: 8px;
		padding: 0.875rem 1rem;
		display: flex;
		align-items: center;
		gap: 0.75rem;
		transition: border-color 0.15s;
	}

	.route-card:hover {
		background: var(--surface);
		border-color: var(--accent);
	}

	.route-name {
		flex: 1;
		font-weight: 500;
		font-size: 0.95rem;
	}

	.route-desc {
		flex: 2;
		font-size: 0.8rem;
		color: var(--muted);
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.badge {
		font-size: 0.7rem;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		padding: 0.15rem 0.5rem;
		border-radius: 4px;
		flex-shrink: 0;
	}

	.badge.packed {
		background: var(--accent-surface);
		color: var(--accent-text);
	}

	.badge.unpacked {
		background: var(--success-surface);
		color: var(--ok);
	}
</style>
