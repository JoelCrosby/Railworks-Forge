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
	let openingRouteId = $state<string | null>(null);

	// Game path state
	let gamePath = $state<string | null>(null);
	let gamePathMissing = $state(false);
	let showPathForm = $state(false);
	let pathInput = $state('');
	let savingPath = $state(false);
	let savePathError = $state<string | null>(null);

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

			// Refresh displayed path on success
			gamePath = await invoke<string>('get_game_path').catch(() => null);
		} catch (e) {
			const msg = String(e);
			if (isPathMissingError(msg)) {
				gamePathMissing = true;
				showPathForm = true;
			} else {
				error = msg;
			}
		} finally {
			loading = false;
		}
	}

	async function savePath() {
		if (!pathInput.trim()) return;
		savingPath = true;
		savePathError = null;
		try {
			await invoke('set_game_path', { path: pathInput.trim() });
			gamePath = pathInput.trim();
			showPathForm = false;
			gamePathMissing = false;
			await loadRoutes();
		} catch (e) {
			savePathError = String(e);
		} finally {
			savingPath = false;
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
				gamePath = p;
				pathInput = p;
			})
			.catch(() => {
				pathInput = '';
			})
			.finally(() => loadRoutes());
	});
</script>

<div class="page">
	<header>
		<h1>Railworks Forge</h1>
		<div class="header-actions">
			<button class="btn-icon" onclick={() => (showPathForm = !showPathForm)} title="Settings">⚙</button>
			<button class="btn-secondary" onclick={() => goto('/assets')}>Assets</button>
			<button onclick={loadRoutes} disabled={loading}>
				{loading ? 'Loading…' : 'Refresh'}
			</button>
		</div>
	</header>

	<!-- Settings / game path form -->
	{#if showPathForm}
		<div class="settings-panel">
			<h2>Game Path</h2>
			<p class="settings-hint">
				Full path to your Train Simulator / Railworks installation directory<br />
				(e.g. <code>/home/user/.steam/steam/steamapps/common/RailWorks</code>)
			</p>
			<div class="path-row">
				<input
					class="path-input"
					type="text"
					placeholder="/path/to/RailWorks"
					bind:value={pathInput}
					onkeydown={(e) => e.key === 'Enter' && savePath()}
				/>
				<button class="btn-primary" onclick={savePath} disabled={savingPath || !pathInput.trim()}>
					{savingPath ? 'Saving…' : 'Save'}
				</button>
				{#if !gamePathMissing}
					<button onclick={() => (showPathForm = false)}>Cancel</button>
				{/if}
			</div>
			{#if savePathError}
				<div class="inline-error">{savePathError}</div>
			{/if}
			{#if gamePath && !gamePathMissing}
				<p class="current-path">Current: <code>{gamePath}</code></p>
			{/if}
		</div>
	{/if}

	{#if error}
		<div class="error">
			<strong>Error:</strong> {error}
		</div>
	{/if}

	{#if gamePathMissing && !showPathForm}
		<div class="error">
			Game path is not configured. Use the ⚙ button above to set it.
		</div>
	{/if}

	{#if loading}
		<div class="status">{progress ?? 'Scanning routes…'}</div>
	{:else if routes.length === 0 && !error && !gamePathMissing}
		<div class="empty">No routes found. Check your game path in settings.</div>
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
							{openingRouteId === route.id ? 'opening' : route.packagingType}
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
		background: #0f1117;
		color: #e2e8f0;
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

	.btn-primary {
		background: #2b6cb0;
		border-color: #3182ce;
		color: #fff;
	}

	.btn-primary:hover:not(:disabled) {
		background: #2c5282;
	}

	.btn-secondary {
		background: #1a3a5c;
		border-color: #2a5a8c;
		color: #90cdf4;
	}

	.btn-secondary:hover:not(:disabled) {
		background: #1e4a70;
	}

	.btn-icon {
		background: none;
		border: 1px solid transparent;
		color: #718096;
		padding: 0.3rem 0.5rem;
		font-size: 1rem;
		line-height: 1;
	}

	.btn-icon:hover {
		color: #e2e8f0;
		background: #2d3748;
		border-color: #4a5568;
	}

	/* Settings panel */
	.settings-panel {
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 8px;
		padding: 1rem 1.25rem;
		margin-bottom: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 0.6rem;
	}

	.settings-panel h2 {
		font-size: 0.95rem;
		font-weight: 600;
	}

	.settings-hint {
		font-size: 0.78rem;
		color: #718096;
		line-height: 1.5;
	}

	.settings-hint code {
		color: #a0aec0;
		font-family: monospace;
		font-size: 0.75rem;
	}

	.path-row {
		display: flex;
		gap: 0.5rem;
		align-items: center;
	}

	.path-input {
		flex: 1;
		background: #0f1117;
		border: 1px solid #4a5568;
		border-radius: 6px;
		padding: 0.4rem 0.75rem;
		color: #e2e8f0;
		font-size: 0.875rem;
		font-family: monospace;
		outline: none;
		min-width: 0;
	}

	.path-input:focus {
		border-color: #4a90d9;
	}

	.inline-error {
		font-size: 0.8rem;
		color: #fc8181;
	}

	.current-path {
		font-size: 0.78rem;
		color: #4a5568;
	}

	.current-path code {
		color: #718096;
		font-family: monospace;
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
