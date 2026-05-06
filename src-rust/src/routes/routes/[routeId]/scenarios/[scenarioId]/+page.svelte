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

	function acquisitionTextClass(state: string): string {
		return state === 'found' ? 'text-ok' : state === 'partial' ? 'text-warn' : 'text-danger-text';
	}

	function locoBadgeClass(locoClass: Consist['locoClass']): string {
		const base = 'rounded-[3px] px-1.5 py-0.5 text-[0.65rem] tracking-wide uppercase';
		switch (locoClass) {
			case 'steam':
				return `${base} bg-[#2d1f10] text-warn`;
			case 'diesel':
				return `${base} bg-[#1a2d1a] text-success-text`;
			case 'electric':
				return `${base} bg-[#1a1a3d] text-accent-text`;
			default:
				return `${base} bg-surface-raised text-muted`;
		}
	}

	function vehicleBadgeClass(type: VehicleBlueprint['blueprintType']): string {
		const base = 'inline-flex size-5 shrink-0 items-center justify-center rounded-[3px] text-[0.65rem] font-bold';
		switch (type) {
			case 'engine':
				return `${base} bg-[#2d1f10] text-warn`;
			case 'tender':
				return `${base} bg-[#2d2010] text-[#fbd38d]`;
			case 'coach':
				return `${base} bg-[#1a2d38] text-accent-text`;
			case 'wagon':
				return `${base} bg-[#2d2a1a] text-[#f6e05e]`;
			default:
				return `${base} bg-surface-raised text-muted`;
		}
	}

	$effect(() => {
		if (scenarioBase) loadDetail();
	});
</script>

<div class="mx-auto max-w-275 p-6">
	<nav class="mb-4">
		<button
			class="cursor-pointer border-0 bg-transparent p-0 text-sm text-accent hover:underline"
			onclick={backToRoute}>← {route?.name ?? 'Route'}</button
		>
	</nav>

	{#if scenarioBase}
		<header class="mb-6 flex items-start justify-between gap-4">
			<div class="flex-1">
				<h1 class="mb-1.5 text-[1.3rem] font-bold">{scenarioBase.name}</h1>
				<div class="flex flex-wrap items-center gap-1.5 text-[0.8rem] text-muted">
					<span>{scenarioBase.locomotive || '—'}</span>
					<span class="text-border-strong">·</span>
					<span>{scenarioBase.season || '—'}</span>
					{#if scenarioBase.startLocation}
						<span class="text-border-strong">·</span>
						<span>{scenarioBase.startLocation}</span>
					{/if}
					{#if scenarioBase.playerInfo.completion}
						<span class="text-border-strong">·</span>
						<span class="text-ok">{scenarioBase.playerInfo.completion}</span>
					{/if}
				</div>
				{#if scenarioBase.description}
					<p class="mt-2 text-[0.82rem] leading-6 text-muted">{scenarioBase.description}</p>
				{/if}
			</div>
			<button
				class="shrink-0 cursor-pointer rounded-md border border-border-strong bg-surface-raised px-4 py-1.5 text-sm text-text hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
				onclick={loadDetail}
				disabled={loading}
			>
				{loading ? 'Loading…' : 'Refresh'}
			</button>
		</header>
	{:else}
		<header class="mb-6"><h1 class="text-[1.3rem] font-bold">Scenario {scenarioId}</h1></header>
	{/if}

	{#if error}
		<div class="mb-6 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"><strong>Error:</strong> {error}</div>
	{/if}

	{#if loading}
		<div class="mt-8 text-center text-sm text-muted">Parsing Scenario.bin…</div>
	{:else if detail}
		<div class="mt-2">
			<div class="mb-3 flex items-center gap-4">
				<h2 class="text-base font-semibold">Consists <span class="font-normal text-muted">({consists.length})</span></h2>
				{#if consists.length > 4}
					<input
						class="max-w-70 flex-1 rounded-md border border-surface-raised bg-surface px-3 py-1.5 text-[0.8rem] text-text outline-none focus:border-accent"
						type="search"
						placeholder="Search consists…"
						bind:value={search}
					/>
				{/if}
			</div>

			{#if consists.length === 0}
				<div class="mt-8 text-center text-sm text-muted">No consists found in this scenario.</div>
			{:else}
				<div class="flex flex-col gap-2">
					{#each filtered as consist (consist.id || consist.serviceId)}
						<div class="overflow-hidden rounded-lg border border-surface-raised bg-surface">
							<div class="border-b border-surface-raised px-4 py-3">
								<div class="mb-1 flex items-center gap-2">
									<span class="text-sm font-medium">{consist.serviceName || '—'}</span>
									{#if consist.playerDriver}
										<span class="rounded bg-accent-surface px-1.5 py-0.5 text-[0.65rem] tracking-wider text-accent-text uppercase">Player</span>
									{/if}
									<span
										class={`w-4 text-center text-xs font-bold ${acquisitionTextClass(consist.acquisitionState)}`}
										title={consist.acquisitionState}
									>
										{acquisitionIcon(consist.acquisitionState)}
									</span>
								</div>
								<div class="flex items-center gap-2.5 text-[0.78rem] text-muted">
									<span class="italic">{consist.locomotiveName || '—'}</span>
									{#if consist.locoAuthor}
										<span class="text-[0.73rem] text-border-strong">{consist.locoAuthor}</span>
									{/if}
									<span class={locoBadgeClass(consist.locoClass)}>{consist.locoClass}</span>
									<span class="ml-auto">{consist.vehicles.length} vehicles</span>
									<button
										class="ml-auto shrink-0 cursor-pointer rounded border border-border-strong bg-transparent px-2 py-0.5 text-[0.7rem] text-muted hover:bg-surface-raised hover:text-text"
										onclick={() => openConsistDetail(consist)}>Edit</button
									>
								</div>
							</div>

							{#if consist.vehicles.length > 0}
								<div class="py-1">
									{#each consist.vehicles as vehicle (vehicle.index)}
										<div class="flex items-center gap-2 border-t border-border px-4 py-1.5 text-[0.78rem] hover:bg-border">
											<span class={vehicleBadgeClass(vehicle.blueprintType)} title={vehicle.blueprintType}>
												{vehicle.blueprintType[0].toUpperCase()}
											</span>
											<span class="flex-2 truncate whitespace-nowrap font-medium">{vehicle.name || '—'}</span>
											<span class="whitespace-nowrap text-border-strong">#{vehicle.uniqueNumber}</span>
											<span class="flex-1 truncate whitespace-nowrap text-[0.72rem] text-border-strong">{vehicle.blueprint.provider}</span>
											{#if vehicle.flipped}
												<span class="text-[0.85rem] text-muted" title="Flipped">↩</span>
											{/if}
											<span
												class={`w-3.5 shrink-0 text-center text-[0.7rem] font-bold ${acquisitionTextClass(vehicle.blueprint.acquisitionState)}`}
												title={vehicle.blueprint.acquisitionState}
											>
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
