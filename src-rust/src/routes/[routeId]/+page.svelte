<script lang="ts">
	import { invoke } from '@tauri-apps/api/core';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	interface Route {
		id: string;
		name: string;
		description: string | null;
		directoryPath: string;
		packagingType: 'packed' | 'unpacked';
	}

	interface Scenario {
		id: string;
		name: string;
		description: string | null;
		briefing: string | null;
		startLocation: string | null;
		locomotive: string;
		duration: number;
		rating: number;
		season: string;
		scenarioClass: 'passenger' | 'freight' | 'shunting' | 'mixed' | 'empty';
		packagingType: 'packed' | 'unpacked';
		directoryPath: string;
		routeId: string;
		playerInfo: { scenarioId: string; score: number; completion: string; medalsAwarded: number };
		consists: unknown[];
	}

	// Route is passed via navigation state; fall back to fetching if missing.
	let route = $state<Route | null>(($page.state as { route?: Route })?.route ?? null);
	let routeId = $derived($page.params.routeId);

	let scenarios = $state<Scenario[]>([]);
	let loading = $state(false);
	let error = $state<string | null>(null);
	let search = $state('');

	let filtered = $derived(
		search.trim()
			? scenarios.filter((s) =>
					[s.name, s.locomotive, s.season].join(' ').toLowerCase().includes(search.toLowerCase())
				)
			: scenarios
	);

	async function loadScenarios() {
		if (!route) return;
		loading = true;
		error = null;
		scenarios = [];
		try {
			scenarios = await invoke<Scenario[]>('get_scenarios', { route });
		} catch (e) {
			error = String(e);
		} finally {
			loading = false;
		}
	}

	function openScenario(scenario: Scenario) {
		goto(`/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenario.id)}`, {
			state: { route, scenario }
		});
	}

	function formatDuration(mins: number): string {
		if (mins <= 0) return '—';
		const h = Math.floor(mins / 60);
		const m = mins % 60;
		return h > 0 ? `${h}h ${m}m` : `${m}m`;
	}

	$effect(() => {
		if (route) loadScenarios();
	});
</script>

<div class="page">
	<nav>
		<button class="back" onclick={() => goto('/')}>← Routes</button>
	</nav>

	{#if route}
		<header>
			<div>
				<h1>{route.name}</h1>
				{#if route.description}
					<p class="subtitle">{route.description}</p>
				{/if}
			</div>
			<button onclick={loadScenarios} disabled={loading}>
				{loading ? 'Loading…' : 'Refresh'}
			</button>
		</header>
	{:else}
		<header><h1>Route {routeId}</h1></header>
	{/if}

	{#if error}
		<div class="error"><strong>Error:</strong> {error}</div>
	{/if}

	{#if !loading && scenarios.length > 0}
		<div class="toolbar">
			<input
				class="search"
				type="search"
				placeholder="Search scenarios…"
				bind:value={search}
			/>
			<span class="count">{filtered.length} / {scenarios.length}</span>
		</div>
	{/if}

	{#if loading}
		<div class="status">Loading scenarios…</div>
	{:else if scenarios.length === 0 && !error}
		<div class="empty">No scenarios found for this route.</div>
	{:else}
		<ul class="scenario-list">
			{#each filtered as scenario (scenario.id)}
				<li>
					<button class="scenario-card" onclick={() => openScenario(scenario)}>
						<span class="scenario-name">{scenario.name}</span>
						<span class="meta">
							<span class="loco">{scenario.locomotive || '—'}</span>
							<span class="duration">{formatDuration(scenario.duration)}</span>
							<span class="season">{scenario.season || '—'}</span>
							<span class="badge {scenario.scenarioClass}">{scenario.scenarioClass}</span>
						</span>
						{#if scenario.playerInfo.completion}
							<span class="completion">{scenario.playerInfo.completion}</span>
						{/if}
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
		max-width: 1100px;
		margin: 0 auto;
		padding: 1.5rem;
	}

	nav {
		margin-bottom: 1rem;
	}

	.back {
		background: none;
		border: none;
		color: #4a90d9;
		font-size: 0.875rem;
		cursor: pointer;
		padding: 0;
	}

	.back:hover {
		text-decoration: underline;
	}

	header {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		margin-bottom: 1.5rem;
		gap: 1rem;
	}

	h1 {
		font-size: 1.3rem;
		font-weight: 700;
	}

	.subtitle {
		font-size: 0.85rem;
		color: #718096;
		margin-top: 0.25rem;
	}

	button {
		background: #2d3748;
		color: #e2e8f0;
		border: 1px solid #4a5568;
		border-radius: 6px;
		padding: 0.4rem 1rem;
		font-size: 0.875rem;
		cursor: pointer;
		flex-shrink: 0;
	}

	button:hover:not(:disabled) {
		background: #3a4a5c;
	}

	button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.toolbar {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		margin-bottom: 1rem;
	}

	.search {
		flex: 1;
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 6px;
		padding: 0.45rem 0.75rem;
		color: #e2e8f0;
		font-size: 0.875rem;
		outline: none;
	}

	.search:focus {
		border-color: #4a90d9;
	}

	.count {
		font-size: 0.8rem;
		color: #718096;
		white-space: nowrap;
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

	.scenario-list {
		list-style: none;
		display: flex;
		flex-direction: column;
		gap: 0.4rem;
	}

	.scenario-card {
		width: 100%;
		text-align: left;
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 8px;
		padding: 0.75rem 1rem;
		display: flex;
		align-items: center;
		gap: 0.75rem;
		transition: border-color 0.15s;
	}

	.scenario-card:hover {
		border-color: #4a90d9;
	}

	.scenario-name {
		flex: 2;
		font-weight: 500;
		font-size: 0.9rem;
	}

	.meta {
		flex: 3;
		display: flex;
		align-items: center;
		gap: 0.75rem;
		font-size: 0.8rem;
		color: #718096;
	}

	.loco {
		flex: 1;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.duration,
	.season {
		white-space: nowrap;
	}

	.completion {
		font-size: 0.75rem;
		color: #68d391;
		white-space: nowrap;
	}

	.badge {
		font-size: 0.65rem;
		text-transform: uppercase;
		letter-spacing: 0.05em;
		padding: 0.15rem 0.45rem;
		border-radius: 4px;
		flex-shrink: 0;
	}

	.badge.passenger { background: #2a4365; color: #90cdf4; }
	.badge.freight    { background: #3d2a14; color: #f6ad55; }
	.badge.shunting   { background: #2d3a1a; color: #9ae6b4; }
	.badge.mixed      { background: #3a2a4a; color: #d6bcfa; }
	.badge.empty      { background: #2d3748; color: #718096; }
</style>
