<script lang="ts">
	import { invoke } from '@tauri-apps/api/core';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	interface Blueprint {
		provider: string;
		product: string;
		blueprintId: string;
		acquisitionState: 'found' | 'partial' | 'missing';
	}

	interface VehicleBlueprint {
		blueprint: Blueprint;
		name: string;
		uniqueNumber: string;
		blueprintType: 'engine' | 'tender' | 'coach' | 'wagon' | 'unknown';
		flipped: boolean;
		index: number;
	}

	interface Consist {
		id: string;
		locomotiveName: string;
		serviceName: string;
		serviceId: string;
		locoAuthor: string | null;
		locoClass: 'steam' | 'diesel' | 'electric' | 'unknown';
		playerDriver: boolean;
		blueprint: Blueprint;
		vehicles: VehicleBlueprint[];
		acquisitionState: 'found' | 'partial' | 'missing';
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
		scenarioClass: string;
		packagingType: 'packed' | 'unpacked';
		directoryPath: string;
		routeId: string;
		playerInfo: { scenarioId: string; score: number; completion: string; medalsAwarded: number };
		consists: Consist[];
	}

	interface Route {
		id: string;
		name: string;
		description: string | null;
		directoryPath: string;
		packagingType: 'packed' | 'unpacked';
	}

	const navState = $page.state as { route?: Route; scenario?: Scenario };
	let route = $state<Route | null>(navState.route ?? null);
	let scenarioBase = $state<Scenario | null>(navState.scenario ?? null);

	let routeId = $derived($page.params.routeId ?? '');
	let scenarioId = $derived($page.params.scenarioId ?? '');

	let detail = $state<Scenario | null>(null);
	let loading = $state(false);
	let error = $state<string | null>(null);
	let search = $state('');

	let consists = $derived(detail?.consists ?? []);
	let filtered = $derived(
		search.trim()
			? consists.filter((c) =>
					[c.serviceName, c.locomotiveName, c.locoAuthor ?? '']
						.join(' ')
						.toLowerCase()
						.includes(search.toLowerCase())
				)
			: consists
	);

	async function loadDetail() {
		if (!scenarioBase) return;
		loading = true;
		error = null;
		detail = null;
		try {
			detail = await invoke<Scenario>('get_scenario_detail', { scenario: scenarioBase });
		} catch (e) {
			error = String(e);
		} finally {
			loading = false;
		}
	}

	function backToRoute() {
		goto(`/routes/${encodeURIComponent(routeId)}`, {
			state: { route: route ? $state.snapshot(route) : null }
		});
	}

	function openConsistDetail(consist: Consist) {
		if (!detail) return;
		goto(
			`/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}/consists/${encodeURIComponent(consist.id)}`,
			{
				state: {
					route: route ? $state.snapshot(route) : null,
					scenario: $state.snapshot(detail),
					consist: $state.snapshot(consist)
				}
			}
		);
	}

	function acquisitionIcon(state: string): string {
		return state === 'found' ? '✓' : state === 'partial' ? '~' : '✗';
	}

	function acquisitionClass(state: string): string {
		return state === 'found' ? 'found' : state === 'partial' ? 'partial' : 'missing';
	}

	$effect(() => {
		if (scenarioBase) loadDetail();
	});
</script>

<div class="page">
	<nav>
		<button class="back" onclick={backToRoute}>← {route?.name ?? 'Route'}</button>
	</nav>

	{#if scenarioBase}
		<header>
			<div class="header-info">
				<h1>{scenarioBase.name}</h1>
				<div class="meta-row">
					<span>{scenarioBase.locomotive || '—'}</span>
					<span class="sep">·</span>
					<span>{scenarioBase.season || '—'}</span>
					{#if scenarioBase.startLocation}
						<span class="sep">·</span>
						<span>{scenarioBase.startLocation}</span>
					{/if}
					{#if scenarioBase.playerInfo.completion}
						<span class="sep">·</span>
						<span class="completion">{scenarioBase.playerInfo.completion}</span>
					{/if}
				</div>
				{#if scenarioBase.description}
					<p class="description">{scenarioBase.description}</p>
				{/if}
			</div>
			<button onclick={loadDetail} disabled={loading}>
				{loading ? 'Loading…' : 'Refresh'}
			</button>
		</header>
	{:else}
		<header><h1>Scenario {scenarioId}</h1></header>
	{/if}

	{#if error}
		<div class="error"><strong>Error:</strong> {error}</div>
	{/if}

	{#if loading}
		<div class="status">Parsing Scenario.bin…</div>
	{:else if detail}
		<div class="consists-section">
			<div class="section-header">
				<h2>Consists <span class="count">({consists.length})</span></h2>
				{#if consists.length > 4}
					<input
						class="search"
						type="search"
						placeholder="Search consists…"
						bind:value={search}
					/>
				{/if}
			</div>

			{#if consists.length === 0}
				<div class="empty">No consists found in this scenario.</div>
			{:else}
				<div class="consist-list">
					{#each filtered as consist (consist.id || consist.serviceId)}
						<div class="consist-card">
							<div class="consist-header">
								<div class="consist-title">
									<span class="service-name">{consist.serviceName || '—'}</span>
									{#if consist.playerDriver}
										<span class="player-badge">Player</span>
									{/if}
									<span class="acq {acquisitionClass(consist.acquisitionState)}" title={consist.acquisitionState}>
										{acquisitionIcon(consist.acquisitionState)}
									</span>
								</div>
								<div class="consist-meta">
									<span class="loco-name">{consist.locomotiveName || '—'}</span>
									{#if consist.locoAuthor}
										<span class="author">{consist.locoAuthor}</span>
									{/if}
									<span class="loco-class badge-{consist.locoClass}">{consist.locoClass}</span>
									<span class="vehicle-count">{consist.vehicles.length} vehicles</span>
									<button class="edit-btn" onclick={() => openConsistDetail(consist)}>Edit</button>
								</div>
							</div>

							{#if consist.vehicles.length > 0}
								<div class="vehicle-list">
									{#each consist.vehicles as vehicle (vehicle.index)}
										<div class="vehicle-row">
											<span class="veh-type badge-veh-{vehicle.blueprintType}" title={vehicle.blueprintType}>
												{vehicle.blueprintType[0].toUpperCase()}
											</span>
											<span class="veh-name">{vehicle.name || '—'}</span>
											<span class="veh-number">#{vehicle.uniqueNumber}</span>
											<span class="veh-provider">{vehicle.blueprint.provider}</span>
											{#if vehicle.flipped}
												<span class="flipped" title="Flipped">↩</span>
											{/if}
											<span class="veh-acq {acquisitionClass(vehicle.blueprint.acquisitionState)}" title={vehicle.blueprint.acquisitionState}>
												{acquisitionIcon(vehicle.blueprint.acquisitionState)}
											</span>
										</div>
									{/each}
								</div>
							{/if}
						</div>
					{/each}
				</div>
			{/if}
		</div>
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
		gap: 1rem;
		margin-bottom: 1.5rem;
	}

	.header-info {
		flex: 1;
	}

	h1 {
		font-size: 1.3rem;
		font-weight: 700;
		margin-bottom: 0.35rem;
	}

	.meta-row {
		font-size: 0.8rem;
		color: #718096;
		display: flex;
		align-items: center;
		gap: 0.35rem;
		flex-wrap: wrap;
	}

	.sep {
		color: #4a5568;
	}

	.completion {
		color: #68d391;
	}

	.description {
		font-size: 0.82rem;
		color: #718096;
		margin-top: 0.5rem;
		line-height: 1.5;
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

	button:hover:not(:disabled) { background: #3a4a5c; }
	button:disabled { opacity: 0.5; cursor: not-allowed; }

	.status, .empty {
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

	.consists-section {
		margin-top: 0.5rem;
	}

	.section-header {
		display: flex;
		align-items: center;
		gap: 1rem;
		margin-bottom: 0.75rem;
	}

	h2 {
		font-size: 1rem;
		font-weight: 600;
	}

	.count {
		color: #718096;
		font-weight: 400;
	}

	.search {
		flex: 1;
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 6px;
		padding: 0.35rem 0.75rem;
		color: #e2e8f0;
		font-size: 0.8rem;
		outline: none;
		max-width: 280px;
	}

	.search:focus { border-color: #4a90d9; }

	.consist-list {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.consist-card {
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 8px;
		overflow: hidden;
	}

	.consist-header {
		padding: 0.75rem 1rem;
		border-bottom: 1px solid #2d3748;
	}

	.consist-title {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		margin-bottom: 0.25rem;
	}

	.service-name {
		font-weight: 500;
		font-size: 0.9rem;
	}

	.player-badge {
		font-size: 0.65rem;
		background: #2a4365;
		color: #90cdf4;
		padding: 0.1rem 0.4rem;
		border-radius: 4px;
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.acq {
		font-size: 0.75rem;
		font-weight: 700;
		width: 1rem;
		text-align: center;
	}

	.acq.found { color: #68d391; }
	.acq.partial { color: #f6ad55; }
	.acq.missing { color: #fc8181; }

	.consist-meta {
		display: flex;
		align-items: center;
		gap: 0.6rem;
		font-size: 0.78rem;
		color: #718096;
	}

	.loco-name { font-style: italic; }

	.author {
		color: #4a5568;
		font-size: 0.73rem;
	}

	.loco-class {
		font-size: 0.65rem;
		text-transform: uppercase;
		letter-spacing: 0.04em;
		padding: 0.1rem 0.35rem;
		border-radius: 3px;
	}

	.badge-steam    { background: #2d1f10; color: #f6ad55; }
	.badge-diesel   { background: #1a2d1a; color: #9ae6b4; }
	.badge-electric { background: #1a1a3d; color: #90cdf4; }
	.badge-unknown  { background: #2d3748; color: #718096; }

	.vehicle-count { margin-left: auto; }

	.vehicle-list {
		padding: 0.25rem 0;
	}

	.vehicle-row {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.3rem 1rem;
		font-size: 0.78rem;
		border-top: 1px solid #1e2535;
	}

	.vehicle-row:hover {
		background: #1e2535;
	}

	.veh-type {
		font-weight: 700;
		font-size: 0.65rem;
		width: 1.2rem;
		height: 1.2rem;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		border-radius: 3px;
		flex-shrink: 0;
	}

	.badge-veh-engine  { background: #2d1f10; color: #f6ad55; }
	.badge-veh-tender  { background: #2d2010; color: #fbd38d; }
	.badge-veh-coach   { background: #1a2d38; color: #90cdf4; }
	.badge-veh-wagon   { background: #2d2a1a; color: #f6e05e; }
	.badge-veh-unknown { background: #2d3748; color: #718096; }

	.veh-name {
		flex: 2;
		font-weight: 500;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.veh-number {
		color: #4a5568;
		white-space: nowrap;
	}

	.veh-provider {
		flex: 1;
		color: #4a5568;
		font-size: 0.72rem;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.flipped {
		color: #718096;
		font-size: 0.85rem;
	}

	.veh-acq {
		font-size: 0.7rem;
		font-weight: 700;
		width: 0.9rem;
		text-align: center;
		flex-shrink: 0;
	}

	.veh-acq.found   { color: #68d391; }
	.veh-acq.partial { color: #f6ad55; }
	.veh-acq.missing { color: #fc8181; }

	.edit-btn {
		margin-left: auto;
		background: none;
		border: 1px solid #4a5568;
		border-radius: 4px;
		color: #718096;
		padding: 0.15rem 0.5rem;
		font-size: 0.7rem;
		cursor: pointer;
		flex-shrink: 0;
	}
	.edit-btn:hover { background: #2d3748; color: #e2e8f0; }
</style>
