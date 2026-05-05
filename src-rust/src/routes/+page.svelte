<script lang="ts">
	import { invoke } from '@tauri-apps/api/core';
	import { Channel } from '@tauri-apps/api/core';
	import { goto } from '$app/navigation';

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

	async function loadRoutes() {
		loading = true;
		error = null;
		progress = null;
		routes = [];

		try {
			const channel = new Channel<ProgressEvent>();
			channel.onmessage = (msg) => {
				progress = msg.message;
			};

			routes = await invoke<Route[]>('get_routes', { onProgress: channel });
			progress = null;
		} catch (e) {
			error = String(e);
		} finally {
			loading = false;
		}
	}

	function openRoute(route: Route) {
		goto(`/routes/${encodeURIComponent(route.id)}`, { state: { route } });
	}

	$effect(() => {
		loadRoutes();
	});
</script>

<div class="page">
	<header>
		<h1>Railworks Forge</h1>
		<button onclick={loadRoutes} disabled={loading}>
			{loading ? 'Loading…' : 'Refresh'}
		</button>
	</header>

	{#if error}
		<div class="error">
			<strong>Error:</strong> {error}
		</div>
	{/if}

	{#if loading}
		<div class="status">{progress ?? 'Scanning routes…'}</div>
	{:else if routes.length === 0 && !error}
		<div class="empty">No routes found. Check your game path in settings.</div>
	{:else}
		<ul class="route-list">
			{#each routes as route (route.id)}
				<li>
					<button class="route-card" onclick={() => openRoute(route)}>
						<span class="route-name">{route.name}</span>
						{#if route.description}
							<span class="route-desc">{route.description}</span>
						{/if}
						<span class="badge {route.packagingType}">{route.packagingType}</span>
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
		background: #0f1117;
		color: #e2e8f0;
		height: 100vh;
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

	h1 {
		font-size: 1.5rem;
		font-weight: 700;
		letter-spacing: -0.02em;
	}

	button {
		background: #2d3748;
		color: #e2e8f0;
		border: 1px solid #4a5568;
		border-radius: 6px;
		padding: 0.4rem 1rem;
		font-size: 0.875rem;
		cursor: pointer;
	}

	button:hover:not(:disabled) {
		background: #3a4a5c;
	}

	button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.status,
	.empty {
		color: #718096;
		font-size: 0.9rem;
		margin-top: 2rem;
		text-align: center;
	}

	.error {
		background: #2d1a1a;
		border: 1px solid #742a2a;
		border-radius: 6px;
		padding: 0.75rem 1rem;
		font-size: 0.875rem;
		color: #fc8181;
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
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 8px;
		padding: 0.875rem 1rem;
		display: flex;
		align-items: center;
		gap: 0.75rem;
		transition: border-color 0.15s;
	}

	.route-card:hover {
		background: #1a202c;
		border-color: #4a90d9;
	}

	.route-name {
		flex: 1;
		font-weight: 500;
		font-size: 0.95rem;
	}

	.route-desc {
		flex: 2;
		font-size: 0.8rem;
		color: #718096;
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
		background: #2a4365;
		color: #90cdf4;
	}

	.badge.unpacked {
		background: #1a4731;
		color: #68d391;
	}
</style>
