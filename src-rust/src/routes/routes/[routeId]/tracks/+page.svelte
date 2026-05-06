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

	interface TrackBlueprint {
		provider: string;
		product: string;
		blueprintId: string;
	}

	interface TrackReplacement {
		from: TrackBlueprint;
		to: TrackBlueprint | null;
	}

	const navState = $page.state as { route?: Route };
	let route = $state<Route | null>(navState.route ?? null);
	let routeId = $derived($page.params.routeId ?? '');

	let tracks = $state<TrackBlueprint[]>([]);
	let loading = $state(false);
	let applying = $state(false);
	let error = $state<string | null>(null);
	let successMsg = $state<string | null>(null);

	// Per-track replacement selections.
	// Key: `${provider}|${product}|${blueprintId}` → replacement blueprint | null
	let replacements = $state<Map<string, TrackBlueprint | null>>(new Map());

	// Edit dialog state
	let editingKey = $state<string | null>(null);
	let editProvider = $state('');
	let editProduct = $state('');
	let editBlueprintId = $state('');

	function trackKey(t: TrackBlueprint): string {
		return `${t.provider}|${t.product}|${t.blueprintId}`;
	}

	function replacementFor(t: TrackBlueprint): TrackBlueprint | null {
		return replacements.get(trackKey(t)) ?? null;
	}

	function pendingCount(): number {
		let count = 0;
		for (const v of replacements.values()) {
			if (v !== null) count++;
		}
		return count;
	}

	async function loadTracks() {
		if (!route) return;
		loading = true;
		error = null;
		tracks = [];
		replacements = new Map();
		try {
			tracks = await invoke<TrackBlueprint[]>('get_tracks', { route });
		} catch (e) {
			error = String(e);
		} finally {
			loading = false;
		}
	}

	function openEditDialog(track: TrackBlueprint) {
		const existing = replacementFor(track);
		editProvider = existing?.provider ?? track.provider;
		editProduct = existing?.product ?? track.product;
		editBlueprintId = existing?.blueprintId ?? track.blueprintId;
		editingKey = trackKey(track);
	}

	function clearReplacement(track: TrackBlueprint) {
		const m = new Map(replacements);
		m.delete(trackKey(track));
		replacements = m;
	}

	function confirmEdit() {
		if (!editingKey || !editProvider || !editProduct || !editBlueprintId) return;
		const m = new Map(replacements);
		m.set(editingKey, { provider: editProvider, product: editProduct, blueprintId: editBlueprintId });
		replacements = m;
		editingKey = null;
	}

	async function applyReplacements() {
		if (!route || pendingCount() === 0) return;

		const replList: { from: TrackBlueprint; to: TrackBlueprint | null }[] = tracks.map((t) => ({
			from: t,
			to: replacementFor(t)
		}));

		applying = true;
		error = null;
		successMsg = null;
		try {
			await invoke('replace_tracks', {
				request: { route, replacements: replList }
			});
			successMsg = `Applied ${pendingCount()} track replacement(s).`;
			replacements = new Map();
			await loadTracks();
		} catch (e) {
			error = String(e);
		} finally {
			applying = false;
		}
	}

	$effect(() => {
		if (route) loadTracks();
	});

	$effect(() => {
		if (successMsg) {
			const id = setTimeout(() => (successMsg = null), 4000);
			return () => clearTimeout(id);
		}
	});
</script>

<div class="page">
	<nav>
		<button
			class="back"
			onclick={() =>
				goto(`/routes/${encodeURIComponent(routeId)}`, {
					state: { route: route ? $state.snapshot(route) : null }
				})}
		>
			← {route?.name ?? 'Route'}
		</button>
	</nav>

	<header>
		<div>
			<h1>Track Replacement</h1>
			{#if route}
				<p class="subtitle">{route.name}</p>
			{/if}
		</div>
		<div class="header-actions">
			<button onclick={loadTracks} disabled={loading || applying}>
				{loading ? 'Loading…' : 'Refresh'}
			</button>
			{#if pendingCount() > 0}
				<button class="btn-primary" onclick={applyReplacements} disabled={applying}>
					{applying ? 'Applying…' : `Apply ${pendingCount()} replacement(s)`}
				</button>
			{/if}
		</div>
	</header>

	{#if successMsg}
		<div class="banner success">{successMsg}</div>
	{/if}
	{#if error}
		<div class="banner error"><strong>Error:</strong> {error}</div>
	{/if}

	{#if loading}
		<div class="status">Parsing Tracks.bin…</div>
	{:else if tracks.length === 0 && !error}
		<div class="empty">No track blueprints found. The route may not have a Tracks.bin.</div>
	{:else}
		<div class="tracks-section">
			<div class="section-header">
				<h2>Track Blueprints <span class="count">({tracks.length})</span></h2>
				{#if pendingCount() > 0}
					<span class="pending-note">{pendingCount()} pending replacement(s)</span>
				{/if}
			</div>

			<table class="tracks-table">
				<thead>
					<tr>
						<th>Provider</th>
						<th>Product</th>
						<th>Blueprint ID</th>
						<th>Replace with</th>
						<th></th>
					</tr>
				</thead>
				<tbody>
					{#each tracks as track (trackKey(track))}
						{@const repl = replacementFor(track)}
						<tr class:has-replacement={repl !== null}>
							<td class="col-provider">{track.provider}</td>
							<td class="col-product">{track.product}</td>
							<td class="col-bp">{track.blueprintId}</td>
							<td class="col-repl">
								{#if repl}
									<div class="repl-preview">
										<span class="repl-provider">{repl.provider}</span>
										<span class="repl-sep">›</span>
										<span class="repl-product">{repl.product}</span>
										<span class="repl-sep">›</span>
										<span class="repl-bp">{repl.blueprintId}</span>
									</div>
								{:else}
									<span class="no-repl">—</span>
								{/if}
							</td>
							<td class="col-actions">
								<button class="btn-small" onclick={() => openEditDialog(track)}>
									{repl ? 'Edit' : 'Set…'}
								</button>
								{#if repl}
									<button class="btn-small btn-danger" onclick={() => clearReplacement(track)} title="Clear replacement">✕</button>
								{/if}
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</div>

<!-- Edit replacement dialog -->
{#if editingKey !== null}
	{@const sourceTrack = tracks.find((t) => trackKey(t) === editingKey)}
	<div class="overlay" role="dialog" aria-modal="true">
		<div class="dialog">
			<div class="dialog-header">
				<h2>Set Replacement</h2>
				<button class="close-btn" onclick={() => (editingKey = null)}>✕</button>
			</div>

			{#if sourceTrack}
				<div class="source-info">
					<span class="label">Replacing:</span>
					<span class="source-bp">{sourceTrack.provider} / {sourceTrack.product} / {sourceTrack.blueprintId}</span>
				</div>
			{/if}

			<div class="form-grid">
				<label>
					Provider
					<input bind:value={editProvider} placeholder="e.g. DTG" />
				</label>
				<label>
					Product
					<input bind:value={editProduct} placeholder="e.g. SomeTrackPack" />
				</label>
				<label class="col-span-2">
					Blueprint ID
					<input bind:value={editBlueprintId} placeholder="e.g. Track\TrackType.xml" />
				</label>
			</div>

			<div class="dialog-footer">
				<button onclick={() => (editingKey = null)}>Cancel</button>
				<button
					class="btn-primary"
					onclick={confirmEdit}
					disabled={!editProvider || !editProduct || !editBlueprintId}
				>
					Set Replacement
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
		background: var(--bg);
		color: var(--text);
		height: 100vh;
		overflow-y: auto;
	}

	.page {
		max-width: 1200px;
		margin: 0 auto;
		padding: 1.5rem;
	}

	nav {
		margin-bottom: 1rem;
	}

	.back {
		background: none;
		border: none;
		color: var(--accent);
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

	h1 {
		font-size: 1.3rem;
		font-weight: 700;
	}

	.subtitle {
		font-size: 0.82rem;
		color: var(--muted);
		margin-top: 0.2rem;
	}

	.header-actions {
		display: flex;
		gap: 0.5rem;
		flex-shrink: 0;
		align-items: flex-start;
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

	.btn-danger {
		background: var(--danger-border);
		border-color: var(--danger-border);
		color: #fff;
	}

	.btn-danger:hover:not(:disabled) {
		background: var(--danger-border);
	}

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

	.banner.success {
		background: var(--success-surface);
		border: 1px solid var(--success-border);
		color: var(--success-text);
	}

	.banner.error {
		background: var(--danger-surface);
		border: 1px solid var(--danger-border);
		color: var(--danger-text);
	}

	.status,
	.empty {
		color: var(--muted);
		font-size: 0.9rem;
		margin-top: 2rem;
		text-align: center;
	}

	.tracks-section {
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
		color: var(--muted);
		font-weight: 400;
	}

	.pending-note {
		font-size: 0.8rem;
		color: var(--warn);
	}

	.tracks-table {
		width: 100%;
		border-collapse: collapse;
		font-size: 0.8rem;
	}

	.tracks-table th {
		text-align: left;
		padding: 0.4rem 0.6rem;
		color: var(--muted);
		font-weight: 500;
		border-bottom: 1px solid var(--surface-raised);
	}

	.tracks-table td {
		padding: 0.35rem 0.6rem;
		border-top: 1px solid var(--border);
		vertical-align: middle;
	}

	.tracks-table tr:hover td {
		background: var(--surface);
	}

	.tracks-table tr.has-replacement td {
		background: #1a2020;
	}

	.tracks-table tr.has-replacement:hover td {
		background: #1a2530;
	}

	.col-provider {
		color: var(--muted);
		white-space: nowrap;
	}

	.col-product {
		color: var(--muted-strong);
		white-space: nowrap;
	}

	.col-bp {
		color: var(--border-strong);
		font-size: 0.75rem;
		max-width: 20rem;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.col-repl {
		max-width: 24rem;
	}

	.repl-preview {
		display: flex;
		align-items: center;
		gap: 0.3rem;
		font-size: 0.75rem;
	}

	.repl-provider {
		color: var(--ok);
		white-space: nowrap;
	}

	.repl-product {
		color: var(--success-text);
		white-space: nowrap;
	}

	.repl-bp {
		color: var(--success-border);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.repl-sep {
		color: var(--surface-raised);
		flex-shrink: 0;
	}

	.no-repl {
		color: var(--surface-raised);
	}

	.col-actions {
		display: flex;
		gap: 0.3rem;
		white-space: nowrap;
	}

	/* Dialog */
	.overlay {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.7);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 100;
	}

	.dialog {
		background: var(--surface);
		border: 1px solid var(--surface-raised);
		border-radius: 10px;
		width: min(560px, 95vw);
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
		color: var(--muted);
		font-size: 1rem;
		cursor: pointer;
		padding: 0.25rem;
	}

	.close-btn:hover {
		color: var(--text);
	}

	.source-info {
		display: flex;
		align-items: baseline;
		gap: 0.5rem;
		font-size: 0.8rem;
		background: var(--bg);
		border: 1px solid var(--surface-raised);
		border-radius: 6px;
		padding: 0.5rem 0.75rem;
	}

	.label {
		color: var(--muted);
		flex-shrink: 0;
	}

	.source-bp {
		color: var(--muted-strong);
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.form-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		gap: 0.5rem 1rem;
	}

	.col-span-2 {
		grid-column: span 2;
	}

	.form-grid label {
		display: flex;
		flex-direction: column;
		gap: 0.25rem;
		font-size: 0.78rem;
		color: var(--muted);
	}

	.form-grid input {
		background: var(--bg);
		border: 1px solid var(--surface-raised);
		border-radius: 4px;
		padding: 0.3rem 0.5rem;
		color: var(--text);
		font-size: 0.8rem;
		outline: none;
	}

	.form-grid input:focus {
		border-color: var(--accent);
	}

	.dialog-footer {
		display: flex;
		justify-content: flex-end;
		gap: 0.5rem;
		padding-top: 0.5rem;
		border-top: 1px solid var(--surface-raised);
	}
</style>
