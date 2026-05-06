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

	interface VehicleEntry {
		provider: string;
		product: string;
		blueprintId: string;
		flipped: boolean;
		blueprintType: 'engine' | 'tender' | 'coach' | 'wagon' | 'unknown';
	}

	interface SavedConsist {
		name: string;
		entries: VehicleEntry[];
	}

	const navState = $page.state as { route?: Route; scenario?: Scenario; consist?: Consist };
	let route = $state<Route | null>(navState.route ?? null);
	let scenario = $state<Scenario | null>(navState.scenario ?? null);
	let consist = $state<Consist | null>(navState.consist ?? null);

	let routeId = $derived($page.params.routeId ?? '');
	let scenarioId = $derived($page.params.scenarioId ?? '');

	let busy = $state(false);
	let error = $state<string | null>(null);
	let successMsg = $state<string | null>(null);

	// ── Replace consist dialog ────────────────────────────────────────────────
	let showReplaceDialog = $state(false);
	let replaceEntries = $state<VehicleEntry[]>([]);
	let replaceError = $state<string | null>(null);
	let savedConsists = $state<SavedConsist[]>([]);
	let saveTemplateName = $state('');
	let showSaveTemplate = $state(false);

	// New vehicle form
	let newProvider = $state('');
	let newProduct = $state('');
	let newBlueprintId = $state('');
	let newFlipped = $state(false);
	let newType = $state<'engine' | 'tender' | 'coach' | 'wagon' | 'unknown'>('wagon');

	function backToScenario() {
		goto(
			`/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}`,
			{
				state: {
					route: route ? $state.snapshot(route) : null,
					scenario: scenario ? $state.snapshot(scenario) : null
				}
			}
		);
	}

	function acquisitionIcon(s: string): string {
		return s === 'found' ? '✓' : s === 'partial' ? '~' : '✗';
	}

	function acquisitionClass(s: string): string {
		return s === 'found' ? 'found' : s === 'partial' ? 'partial' : 'missing';
	}

	function acquisitionTextClass(state: string): string {
		return state === 'found' ? 'text-[var(--ok)]' : state === 'partial' ? 'text-[var(--warn)]' : 'text-[var(--danger-text)]';
	}

	function locoBadgeClass(locoClass: Consist['locoClass']): string {
		const base = 'rounded-[3px] px-1.5 py-0.5 text-[0.65rem] tracking-wide uppercase';
		switch (locoClass) {
			case 'steam':
				return `${base} bg-[#2d1f10] text-[var(--warn)]`;
			case 'diesel':
				return `${base} bg-[#1a2d1a] text-[var(--success-text)]`;
			case 'electric':
				return `${base} bg-[#1a1a3d] text-[var(--accent-text)]`;
			default:
				return `${base} bg-[var(--surface-raised)] text-[var(--muted)]`;
		}
	}

	function vehicleBadgeClass(type: VehicleEntry['blueprintType']): string {
		const base = 'inline-flex size-5 items-center justify-center rounded-[3px] text-[0.65rem] font-bold';
		switch (type) {
			case 'engine':
				return `${base} bg-[#2d1f10] text-[var(--warn)]`;
			case 'tender':
				return `${base} bg-[#2d2010] text-[#fbd38d]`;
			case 'coach':
				return `${base} bg-[#1a2d38] text-[var(--accent-text)]`;
			case 'wagon':
				return `${base} bg-[#2d2a1a] text-[#f6e05e]`;
			default:
				return `${base} bg-[var(--surface-raised)] text-[var(--muted)]`;
		}
	}

	function showSuccess(msg: string) {
		successMsg = msg;
		setTimeout(() => (successMsg = null), 3000);
	}

	async function deleteVehicle(vehicleIndex: number) {
		if (!scenario || !consist) return;
		if (!confirm(`Delete vehicle #${vehicleIndex + 1}?`)) return;

		busy = true;
		error = null;
		try {
			const updated = await invoke<Scenario>('delete_vehicle', {
				request: {
					scenario,
					consistId: consist.id,
					vehicleIndex
				}
			});
			scenario = updated;
			consist = updated.consists.find((c) => c.id === consist!.id) ?? consist;
			showSuccess('Vehicle deleted.');
		} catch (e) {
			error = String(e);
		} finally {
			busy = false;
		}
	}

	async function openReplaceDialog() {
		showReplaceDialog = true;
		replaceEntries = consist?.vehicles.map((v) => ({
			provider: v.blueprint.provider,
			product: v.blueprint.product,
			blueprintId: v.blueprint.blueprintId,
			flipped: v.flipped,
			blueprintType: v.blueprintType
		})) ?? [];
		replaceError = null;
		await loadSavedConsists();
	}

	async function loadSavedConsists() {
		try {
			savedConsists = await invoke<SavedConsist[]>('get_saved_consists');
		} catch {
			savedConsists = [];
		}
	}

	function addReplaceEntry() {
		if (!newProvider || !newProduct || !newBlueprintId) return;
		replaceEntries = [
			...replaceEntries,
			{
				provider: newProvider,
				product: newProduct,
				blueprintId: newBlueprintId,
				flipped: newFlipped,
				blueprintType: newType
			}
		];
		newProvider = '';
		newProduct = '';
		newBlueprintId = '';
		newFlipped = false;
		newType = 'wagon';
	}

	function removeReplaceEntry(i: number) {
		replaceEntries = replaceEntries.filter((_, idx) => idx !== i);
	}

	function loadTemplate(t: SavedConsist) {
		replaceEntries = [...t.entries];
	}

	async function saveTemplate() {
		if (!saveTemplateName.trim()) return;
		try {
			await invoke('save_consist', {
				consist: { name: saveTemplateName.trim(), entries: replaceEntries }
			});
			saveTemplateName = '';
			showSaveTemplate = false;
			await loadSavedConsists();
			showSuccess('Template saved.');
		} catch (e) {
			replaceError = String(e);
		}
	}

	async function confirmReplace() {
		if (!scenario || !consist || replaceEntries.length === 0) return;
		busy = true;
		replaceError = null;
		try {
			const updated = await invoke<Scenario>('replace_consist', {
				request: {
					scenario,
					targetConsistId: consist.id,
					entries: replaceEntries
				}
			});
			scenario = updated;
			consist = updated.consists.find((c) => c.id === consist!.id) ?? consist;
			showReplaceDialog = false;
			showSuccess('Consist replaced.');
		} catch (e) {
			replaceError = String(e);
		} finally {
			busy = false;
		}
	}

	async function deleteConsist() {
		if (!scenario || !consist) return;
		if (!confirm(`Delete consist "${consist.serviceName}"? This cannot be undone.`)) return;

		busy = true;
		error = null;
		try {
			const updated = await invoke<Scenario>('delete_consist', {
				request: { scenario, consistId: consist.id }
			});
			// Consist no longer exists; go back to scenario.
			scenario = updated;
			goto(
				`/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}`,
				{
					state: {
						route: route ? $state.snapshot(route) : null,
						scenario: $state.snapshot(updated)
					}
				}
			);
		} catch (e) {
			error = String(e);
		} finally {
			busy = false;
		}
	}
</script>

<div class="mx-auto max-w-[1200px] p-6">
	<nav class="mb-4">
		<button
			class="cursor-pointer border-0 bg-transparent p-0 text-sm text-[var(--accent)] hover:underline"
			onclick={backToScenario}>← {scenario?.name ?? 'Scenario'}</button
		>
	</nav>

	{#if consist}
		<header class="mb-6 flex items-start justify-between gap-4">
			<div class="flex-1">
				<h1 class="mb-1.5 text-[1.3rem] font-bold">{consist.serviceName || consist.locomotiveName || '—'}</h1>
				<div class="flex flex-wrap items-center gap-1.5 text-[0.8rem] text-[var(--muted)]">
					<span class="italic">{consist.locomotiveName || '—'}</span>
					{#if consist.locoAuthor}
						<span class="text-[var(--border-strong)]">·</span>
						<span>{consist.locoAuthor}</span>
					{/if}
					<span class="text-[var(--border-strong)]">·</span>
					<span class={locoBadgeClass(consist.locoClass)}>{consist.locoClass}</span>
					{#if consist.playerDriver}
						<span class="text-[var(--border-strong)]">·</span>
						<span class="rounded bg-[var(--accent-surface)] px-1.5 py-0.5 text-[0.65rem] tracking-wider text-[var(--accent-text)] uppercase">Player</span>
					{/if}
					<span class="text-[var(--border-strong)]">·</span>
					<span class={`text-xs font-bold ${acquisitionTextClass(consist.acquisitionState)}`}>
						{acquisitionIcon(consist.acquisitionState)}
					</span>
				</div>
			</div>
			<div class="flex shrink-0 gap-2">
				<button
					class="cursor-pointer rounded-md border border-[var(--primary-border)] bg-[var(--primary)] px-4 py-1.5 text-sm text-white hover:bg-[var(--primary-hover)] disabled:cursor-not-allowed disabled:opacity-50"
					onclick={openReplaceDialog}
					disabled={busy}>Replace Consist</button
				>
				<button
					class="cursor-pointer rounded-md border border-[var(--danger-border)] bg-[var(--danger-border)] px-4 py-1.5 text-sm text-white disabled:cursor-not-allowed disabled:opacity-50"
					onclick={deleteConsist}
					disabled={busy}>Delete Consist</button
				>
			</div>
		</header>
	{:else}
		<header class="mb-6"><h1 class="text-[1.3rem] font-bold">Consist</h1></header>
	{/if}

	{#if successMsg}
		<div class="mb-4 rounded-md border border-[var(--success-border)] bg-[var(--success-surface)] px-4 py-3 text-sm text-[var(--success-text)]">{successMsg}</div>
	{/if}
	{#if error}
		<div class="mb-4 rounded-md border border-[var(--danger-border)] bg-[var(--danger-surface)] px-4 py-3 text-sm text-[var(--danger-text)]"><strong>Error:</strong> {error}</div>
	{/if}

	<!-- Vehicle list -->
	{#if consist}
		<section class="mt-2">
			<div class="mb-3 flex items-center gap-4">
				<h2 class="text-base font-semibold">Vehicles <span class="font-normal text-[var(--muted)]">({consist.vehicles.length})</span></h2>
			</div>

			{#if consist.vehicles.length === 0}
				<div class="mt-8 text-center text-sm text-[var(--muted)]">No vehicles in this consist.</div>
			{:else}
				<table class="w-full border-collapse text-[0.8rem]">
					<thead>
						<tr>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">#</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Type</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Name</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Number</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Provider</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Blueprint</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Flip</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">State</th>
							<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]"></th>
						</tr>
					</thead>
					<tbody>
						{#each consist.vehicles as v (v.index)}
							<tr class="hover:bg-[var(--surface)]">
								<td class="w-8 border-t border-[var(--border)] px-2.5 py-1.5 align-middle text-[var(--border-strong)]">{v.index + 1}</td>
								<td class="border-t border-[var(--border)] px-2.5 py-1.5 align-middle">
									<span class={vehicleBadgeClass(v.blueprintType)} title={v.blueprintType}>
										{v.blueprintType[0].toUpperCase()}
									</span>
								</td>
								<td class="border-t border-[var(--border)] px-2.5 py-1.5 align-middle font-medium">{v.name || '—'}</td>
								<td class="border-t border-[var(--border)] px-2.5 py-1.5 align-middle text-[var(--border-strong)]">#{v.uniqueNumber}</td>
								<td class="max-w-32 truncate whitespace-nowrap border-t border-[var(--border)] px-2.5 py-1.5 align-middle text-[var(--muted)]">{v.blueprint.provider}</td>
								<td class="max-w-80 truncate whitespace-nowrap border-t border-[var(--border)] px-2.5 py-1.5 align-middle text-[var(--border-strong)]">{v.blueprint.blueprintId}</td>
								<td class="border-t border-[var(--border)] px-2.5 py-1.5 text-center align-middle">{v.flipped ? '↩' : ''}</td>
								<td class="border-t border-[var(--border)] px-2.5 py-1.5 align-middle">
									<span class={`text-xs font-bold ${acquisitionTextClass(v.blueprint.acquisitionState)}`}>
										{acquisitionIcon(v.blueprint.acquisitionState)}
									</span>
								</td>
								<td class="border-t border-[var(--border)] px-2.5 py-1.5 align-middle">
									<button
										class="cursor-pointer rounded-md border border-[var(--danger-border)] bg-[var(--danger-border)] px-2.5 py-1 text-[0.78rem] text-white disabled:cursor-not-allowed disabled:opacity-50"
										onclick={() => deleteVehicle(v.index)}
										disabled={busy}
										title="Delete vehicle"
									>✕</button>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			{/if}
		</section>
	{/if}
</div>

<!-- Replace Consist Dialog -->
{#if showReplaceDialog}
	<div class="fixed inset-0 z-[100] flex items-center justify-center bg-black/70" role="dialog" aria-modal="true">
		<div class="flex max-h-[85vh] w-[min(720px,95vw)] flex-col gap-4 overflow-y-auto rounded-[10px] border border-[var(--surface-raised)] bg-[var(--surface)] p-6">
			<div class="flex items-center justify-between">
				<h2 class="text-base font-semibold">Replace Consist</h2>
				<button
					class="cursor-pointer border-0 bg-transparent p-1 text-base text-[var(--muted)] hover:text-[var(--text)]"
					onclick={() => (showReplaceDialog = false)}>✕</button
				>
			</div>

			{#if replaceError}
				<div class="mb-4 rounded-md border border-[var(--danger-border)] bg-[var(--danger-surface)] px-4 py-3 text-sm text-[var(--danger-text)]"><strong>Error:</strong> {replaceError}</div>
			{/if}

			<!-- Saved templates -->
			{#if savedConsists.length > 0}
				<div class="flex flex-wrap items-center gap-2">
					<span class="text-[0.8rem] text-[var(--muted)]">Load template:</span>
					{#each savedConsists as t}
						<button
							class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-2.5 py-1 text-[0.78rem] text-[var(--text)] hover:bg-[var(--surface-hover)]"
							onclick={() => loadTemplate(t)}>{t.name}</button
						>
					{/each}
				</div>
			{/if}

			<!-- Current replacement vehicle list -->
			<div class="flex max-h-[200px] flex-col gap-1 overflow-y-auto rounded-md border border-[var(--surface-raised)] p-2">
				{#if replaceEntries.length === 0}
					<div class="mt-8 text-center text-sm text-[var(--muted)]">Add vehicles below.</div>
				{:else}
					{#each replaceEntries as entry, i}
						<div class="flex items-center gap-2 rounded px-1.5 py-1 text-[0.78rem] hover:bg-[var(--bg)]">
							<span class="w-5 shrink-0 text-right text-[var(--border-strong)]">{i + 1}</span>
							<span class={`${vehicleBadgeClass(entry.blueprintType)} shrink-0`}>{entry.blueprintType[0].toUpperCase()}</span>
							<span class="shrink-0 text-[var(--muted)]">{entry.provider}</span>
							<span class="shrink-0 text-[var(--muted)]">{entry.product}</span>
							<span class="flex-1 truncate whitespace-nowrap text-[var(--border-strong)]">{entry.blueprintId}</span>
							{#if entry.flipped}<span class="shrink-0 text-[var(--muted)]">↩</span>{/if}
							<button
								class="cursor-pointer rounded-md border border-[var(--danger-border)] bg-[var(--danger-border)] px-2.5 py-1 text-[0.78rem] text-white"
								onclick={() => removeReplaceEntry(i)}>✕</button
							>
						</div>
					{/each}
				{/if}
			</div>

			<!-- Add vehicle form -->
			<details class="rounded-md border border-[var(--surface-raised)] p-3">
				<summary class="cursor-pointer text-sm text-[var(--muted)] select-none">Add vehicle</summary>
				<div class="mt-3 grid grid-cols-2 gap-x-4 gap-y-2">
					<label class="flex flex-col gap-1 text-[0.78rem] text-[var(--muted)]">
						Type
						<select
							class="rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
							bind:value={newType}
						>
							<option value="engine">Engine</option>
							<option value="tender">Tender</option>
							<option value="wagon">Wagon</option>
							<option value="coach">Coach</option>
						</select>
					</label>
					<label class="flex flex-col gap-1 text-[0.78rem] text-[var(--muted)]">
						Provider
						<input
							class="rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
							bind:value={newProvider}
							placeholder="e.g. DTG"
						/>
					</label>
					<label class="flex flex-col gap-1 text-[0.78rem] text-[var(--muted)]">
						Product
						<input
							class="rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
							bind:value={newProduct}
							placeholder="e.g. SomeProduct"
						/>
					</label>
					<label class="col-span-2 flex flex-col gap-1 text-[0.78rem] text-[var(--muted)]">
						Blueprint ID
						<input
							class="rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
							bind:value={newBlueprintId}
							placeholder="RailVehicles\Engines\Foo.xml"
						/>
					</label>
					<label class="flex flex-row items-center gap-2 text-[0.78rem] text-[var(--text)]">
						<input type="checkbox" bind:checked={newFlipped} />
						Flipped
					</label>
					<button
						class="cursor-pointer rounded-md border border-[var(--primary-border)] bg-[var(--primary)] px-4 py-1.5 text-sm text-white hover:bg-[var(--primary-hover)] disabled:cursor-not-allowed disabled:opacity-50"
						onclick={addReplaceEntry}
						disabled={!newProvider || !newProduct || !newBlueprintId}
					>Add</button>
				</div>
			</details>

			<!-- Save as template -->
			{#if replaceEntries.length > 0}
				{#if showSaveTemplate}
					<div class="flex items-center gap-2">
						<input
							class="flex-1 rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
							bind:value={saveTemplateName}
							placeholder="Template name…"
						/>
						<button
							class="cursor-pointer rounded-md border border-[var(--primary-border)] bg-[var(--primary)] px-4 py-1.5 text-sm text-white hover:bg-[var(--primary-hover)] disabled:cursor-not-allowed disabled:opacity-50"
							onclick={saveTemplate}
							disabled={!saveTemplateName.trim()}>Save</button
						>
						<button
							class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-4 py-1.5 text-sm text-[var(--text)] hover:bg-[var(--surface-hover)]"
							onclick={() => (showSaveTemplate = false)}>Cancel</button
						>
					</div>
				{:else}
					<button
						class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-2.5 py-1 text-[0.78rem] text-[var(--text)] hover:bg-[var(--surface-hover)]"
						onclick={() => (showSaveTemplate = true)}>Save as template…</button
					>
				{/if}
			{/if}

			<div class="flex justify-end gap-2 border-t border-[var(--surface-raised)] pt-2">
				<button
					class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-4 py-1.5 text-sm text-[var(--text)] hover:bg-[var(--surface-hover)]"
					onclick={() => (showReplaceDialog = false)}>Cancel</button
				>
				<button
					class="cursor-pointer rounded-md border border-[var(--primary-border)] bg-[var(--primary)] px-4 py-1.5 text-sm text-white hover:bg-[var(--primary-hover)] disabled:cursor-not-allowed disabled:opacity-50"
					onclick={confirmReplace}
					disabled={busy || replaceEntries.length === 0}
				>
					{busy ? 'Applying…' : `Replace (${replaceEntries.length} vehicles)`}
				</button>
			</div>
		</div>
	</div>
{/if}
