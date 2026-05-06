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

<div class="page">
	<nav>
		<button class="back" onclick={backToScenario}>← {scenario?.name ?? 'Scenario'}</button>
	</nav>

	{#if consist}
		<header>
			<div class="header-info">
				<h1>{consist.serviceName || consist.locomotiveName || '—'}</h1>
				<div class="meta-row">
					<span class="loco-name">{consist.locomotiveName || '—'}</span>
					{#if consist.locoAuthor}
						<span class="sep">·</span>
						<span>{consist.locoAuthor}</span>
					{/if}
					<span class="sep">·</span>
					<span class="badge-{consist.locoClass} loco-badge">{consist.locoClass}</span>
					{#if consist.playerDriver}
						<span class="sep">·</span>
						<span class="player-badge">Player</span>
					{/if}
					<span class="sep">·</span>
					<span class="acq {acquisitionClass(consist.acquisitionState)}">
						{acquisitionIcon(consist.acquisitionState)}
					</span>
				</div>
			</div>
			<div class="header-actions">
				<button onclick={openReplaceDialog} disabled={busy} class="btn-primary">Replace Consist</button>
				<button onclick={deleteConsist} disabled={busy} class="btn-danger">Delete Consist</button>
			</div>
		</header>
	{:else}
		<header><h1>Consist</h1></header>
	{/if}

	{#if successMsg}
		<div class="banner success">{successMsg}</div>
	{/if}
	{#if error}
		<div class="banner error"><strong>Error:</strong> {error}</div>
	{/if}

	<!-- Vehicle list -->
	{#if consist}
		<section>
			<div class="section-header">
				<h2>Vehicles <span class="count">({consist.vehicles.length})</span></h2>
			</div>

			{#if consist.vehicles.length === 0}
				<div class="empty">No vehicles in this consist.</div>
			{:else}
				<table class="vehicle-table">
					<thead>
						<tr>
							<th>#</th>
							<th>Type</th>
							<th>Name</th>
							<th>Number</th>
							<th>Provider</th>
							<th>Blueprint</th>
							<th>Flip</th>
							<th>State</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						{#each consist.vehicles as v (v.index)}
							<tr>
								<td class="col-idx">{v.index + 1}</td>
								<td>
									<span class="veh-type badge-veh-{v.blueprintType}" title={v.blueprintType}>
										{v.blueprintType[0].toUpperCase()}
									</span>
								</td>
								<td class="col-name">{v.name || '—'}</td>
								<td class="col-num">#{v.uniqueNumber}</td>
								<td class="col-provider">{v.blueprint.provider}</td>
								<td class="col-bp">{v.blueprint.blueprintId}</td>
								<td class="col-flip">{v.flipped ? '↩' : ''}</td>
								<td>
									<span class="acq {acquisitionClass(v.blueprint.acquisitionState)}">
										{acquisitionIcon(v.blueprint.acquisitionState)}
									</span>
								</td>
								<td>
									<button
										class="btn-small btn-danger"
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
	<div class="overlay" role="dialog" aria-modal="true">
		<div class="dialog">
			<div class="dialog-header">
				<h2>Replace Consist</h2>
				<button class="close-btn" onclick={() => (showReplaceDialog = false)}>✕</button>
			</div>

			{#if replaceError}
				<div class="banner error"><strong>Error:</strong> {replaceError}</div>
			{/if}

			<!-- Saved templates -->
			{#if savedConsists.length > 0}
				<div class="templates-row">
					<span class="label">Load template:</span>
					{#each savedConsists as t}
						<button class="btn-small" onclick={() => loadTemplate(t)}>{t.name}</button>
					{/each}
				</div>
			{/if}

			<!-- Current replacement vehicle list -->
			<div class="replace-list">
				{#if replaceEntries.length === 0}
					<div class="empty">Add vehicles below.</div>
				{:else}
					{#each replaceEntries as entry, i}
						<div class="replace-entry">
							<span class="re-idx">{i + 1}</span>
							<span class="re-type badge-veh-{entry.blueprintType}">{entry.blueprintType[0].toUpperCase()}</span>
							<span class="re-provider">{entry.provider}</span>
							<span class="re-product">{entry.product}</span>
							<span class="re-bp">{entry.blueprintId}</span>
							{#if entry.flipped}<span class="re-flip">↩</span>{/if}
							<button class="btn-small btn-danger" onclick={() => removeReplaceEntry(i)}>✕</button>
						</div>
					{/each}
				{/if}
			</div>

			<!-- Add vehicle form -->
			<details class="add-vehicle-form">
				<summary>Add vehicle</summary>
				<div class="form-grid">
					<label>
						Type
						<select bind:value={newType}>
							<option value="engine">Engine</option>
							<option value="tender">Tender</option>
							<option value="wagon">Wagon</option>
							<option value="coach">Coach</option>
						</select>
					</label>
					<label>
						Provider
						<input bind:value={newProvider} placeholder="e.g. DTG" />
					</label>
					<label>
						Product
						<input bind:value={newProduct} placeholder="e.g. SomeProduct" />
					</label>
					<label class="col-span-2">
						Blueprint ID
						<input bind:value={newBlueprintId} placeholder="RailVehicles\Engines\Foo.xml" />
					</label>
					<label class="checkbox-label">
						<input type="checkbox" bind:checked={newFlipped} />
						Flipped
					</label>
					<button
						class="btn-primary"
						onclick={addReplaceEntry}
						disabled={!newProvider || !newProduct || !newBlueprintId}
					>Add</button>
				</div>
			</details>

			<!-- Save as template -->
			{#if replaceEntries.length > 0}
				{#if showSaveTemplate}
					<div class="save-template-row">
						<input bind:value={saveTemplateName} placeholder="Template name…" />
						<button class="btn-primary" onclick={saveTemplate} disabled={!saveTemplateName.trim()}>Save</button>
						<button onclick={() => (showSaveTemplate = false)}>Cancel</button>
					</div>
				{:else}
					<button class="btn-small" onclick={() => (showSaveTemplate = true)}>Save as template…</button>
				{/if}
			{/if}

			<div class="dialog-footer">
				<button onclick={() => (showReplaceDialog = false)}>Cancel</button>
				<button
					class="btn-primary"
					onclick={confirmReplace}
					disabled={busy || replaceEntries.length === 0}
				>
					{busy ? 'Applying…' : `Replace (${replaceEntries.length} vehicles)`}
				</button>
			</div>
		</div>
	</div>
{/if}

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
		max-width: 1200px;
		margin: 0 auto;
		padding: 1.5rem;
	}

	nav { margin-bottom: 1rem; }

	.back {
		background: none;
		border: none;
		color: #4a90d9;
		font-size: 0.875rem;
		cursor: pointer;
		padding: 0;
	}
	.back:hover { text-decoration: underline; }

	header {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 1rem;
		margin-bottom: 1.5rem;
	}

	.header-info { flex: 1; }

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

	.sep { color: #4a5568; }
	.loco-name { font-style: italic; }

	.loco-badge {
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

	.player-badge {
		font-size: 0.65rem;
		background: #2a4365;
		color: #90cdf4;
		padding: 0.1rem 0.4rem;
		border-radius: 4px;
		text-transform: uppercase;
		letter-spacing: 0.05em;
	}

	.acq { font-size: 0.75rem; font-weight: 700; }
	.acq.found   { color: #68d391; }
	.acq.partial { color: #f6ad55; }
	.acq.missing { color: #fc8181; }

	.header-actions {
		display: flex;
		gap: 0.5rem;
		flex-shrink: 0;
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
	button:hover:not(:disabled) { background: #3a4a5c; }
	button:disabled { opacity: 0.5; cursor: not-allowed; }

	.btn-primary {
		background: #2b6cb0;
		border-color: #3182ce;
		color: #fff;
	}
	.btn-primary:hover:not(:disabled) { background: #2c5282; }

	.btn-danger {
		background: #742a2a;
		border-color: #9b2c2c;
		color: #fff;
	}
	.btn-danger:hover:not(:disabled) { background: #9b2c2c; }

	.btn-small {
		padding: 0.25rem 0.6rem;
		font-size: 0.78rem;
	}

	.banner {
		padding: 0.75rem 1rem;
		border-radius: 6px;
		font-size: 0.875rem;
		margin-bottom: 1rem;
	}
	.banner.success { background: #1a2f1a; border: 1px solid #276227; color: #9ae6b4; }
	.banner.error   { background: #2d1a1a; border: 1px solid #742a2a; color: #fc8181; }

	section { margin-top: 0.5rem; }

	.section-header {
		display: flex;
		align-items: center;
		gap: 1rem;
		margin-bottom: 0.75rem;
	}

	h2 { font-size: 1rem; font-weight: 600; }
	.count { color: #718096; font-weight: 400; }

	.empty {
		color: #718096;
		font-size: 0.9rem;
		text-align: center;
		margin-top: 2rem;
	}

	/* Vehicle table */
	.vehicle-table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.8rem;
	}

	.vehicle-table th {
		text-align: left;
		padding: 0.4rem 0.6rem;
		color: #718096;
		font-weight: 500;
		border-bottom: 1px solid #2d3748;
	}

	.vehicle-table td {
		padding: 0.35rem 0.6rem;
		border-top: 1px solid #1e2535;
		vertical-align: middle;
	}

	.vehicle-table tr:hover td { background: #1a202c; }

	.col-idx  { width: 2rem; color: #4a5568; }
	.col-name { font-weight: 500; }
	.col-num  { color: #4a5568; }
	.col-provider { color: #718096; max-width: 8rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.col-bp   { color: #4a5568; max-width: 20rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
	.col-flip { text-align: center; }

	.veh-type {
		font-weight: 700;
		font-size: 0.65rem;
		width: 1.2rem;
		height: 1.2rem;
		display: inline-flex;
		align-items: center;
		justify-content: center;
		border-radius: 3px;
	}
	.badge-veh-engine  { background: #2d1f10; color: #f6ad55; }
	.badge-veh-tender  { background: #2d2010; color: #fbd38d; }
	.badge-veh-coach   { background: #1a2d38; color: #90cdf4; }
	.badge-veh-wagon   { background: #2d2a1a; color: #f6e05e; }
	.badge-veh-unknown { background: #2d3748; color: #718096; }

	/* Replace dialog */
	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(0,0,0,0.7);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 100;
	}

	.dialog {
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 10px;
		width: min(720px, 95vw);
		max-height: 85vh;
		overflow-y: auto;
		padding: 1.5rem;
		display: flex;
		flex-direction: column;
		gap: 1rem;
	}

	.dialog-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
	}

	.close-btn {
		background: none;
		border: none;
		color: #718096;
		font-size: 1rem;
		cursor: pointer;
		padding: 0.25rem;
	}
	.close-btn:hover { color: #e2e8f0; }

	.templates-row {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		flex-wrap: wrap;
	}
	.label { font-size: 0.8rem; color: #718096; }

	.replace-list {
		display: flex;
		flex-direction: column;
		gap: 0.3rem;
		max-height: 200px;
		overflow-y: auto;
		border: 1px solid #2d3748;
		border-radius: 6px;
		padding: 0.5rem;
	}

	.replace-entry {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.78rem;
		padding: 0.25rem 0.35rem;
		border-radius: 4px;
	}
	.replace-entry:hover { background: #0f1117; }

	.re-idx   { color: #4a5568; width: 1.2rem; text-align: right; flex-shrink: 0; }
	.re-type  { flex-shrink: 0; }
	.re-provider { color: #718096; flex-shrink: 0; }
	.re-product  { color: #718096; flex-shrink: 0; }
	.re-bp    { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #4a5568; }
	.re-flip  { color: #718096; flex-shrink: 0; }

	.add-vehicle-form {
		border: 1px solid #2d3748;
		border-radius: 6px;
		padding: 0.75rem;
	}

	.add-vehicle-form summary {
		cursor: pointer;
		font-size: 0.875rem;
		color: #718096;
		user-select: none;
	}

	.form-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 0.5rem 1rem;
		margin-top: 0.75rem;
	}

	.col-span-2 { grid-column: span 2; }

	.form-grid label {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		font-size: 0.78rem;
		color: #718096;
	}

	.checkbox-label {
		flex-direction: row !important;
		align-items: center;
		gap: 0.5rem !important;
		color: #e2e8f0 !important;
	}

	.form-grid input,
	.form-grid select {
		background: #0f1117;
		border: 1px solid #2d3748;
		border-radius: 4px;
		padding: 0.3rem 0.5rem;
		color: #e2e8f0;
		font-size: 0.8rem;
		outline: none;
	}
	.form-grid input:focus,
	.form-grid select:focus { border-color: #4a90d9; }

	.save-template-row {
		display: flex;
		gap: 0.5rem;
		align-items: center;
	}
	.save-template-row input {
		flex: 1;
		background: #0f1117;
		border: 1px solid #2d3748;
		border-radius: 4px;
		padding: 0.3rem 0.5rem;
		color: #e2e8f0;
		font-size: 0.8rem;
		outline: none;
	}
	.save-template-row input:focus { border-color: #4a90d9; }

	.dialog-footer {
		display: flex;
		justify-content: flex-end;
		gap: 0.5rem;
		padding-top: 0.5rem;
		border-top: 1px solid #2d3748;
	}
</style>
