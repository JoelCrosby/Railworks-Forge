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

<div class="mx-auto max-w-[1200px] p-6">
	<nav class="mb-4">
		<button
			class="cursor-pointer border-0 bg-transparent p-0 text-sm text-[var(--accent)] hover:underline"
			onclick={() =>
				goto(`/routes/${encodeURIComponent(routeId)}`, {
					state: { route: route ? $state.snapshot(route) : null }
				})}
		>
			← {route?.name ?? 'Route'}
		</button>
	</nav>

	<header class="mb-6 flex items-start justify-between gap-4">
		<div>
			<h1 class="text-[1.3rem] font-bold">Track Replacement</h1>
			{#if route}
				<p class="mt-1 text-[0.82rem] text-[var(--muted)]">{route.name}</p>
			{/if}
		</div>
		<div class="flex shrink-0 items-start gap-2">
			<button
				class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-4 py-1.5 text-sm text-[var(--text)] hover:bg-[var(--surface-hover)] disabled:cursor-not-allowed disabled:opacity-50"
				onclick={loadTracks}
				disabled={loading || applying}
			>
				{loading ? 'Loading…' : 'Refresh'}
			</button>
			{#if pendingCount() > 0}
				<button
					class="cursor-pointer rounded-md border border-[var(--primary-border)] bg-[var(--primary)] px-4 py-1.5 text-sm text-white hover:bg-[var(--primary-hover)] disabled:cursor-not-allowed disabled:opacity-50"
					onclick={applyReplacements}
					disabled={applying}
				>
					{applying ? 'Applying…' : `Apply ${pendingCount()} replacement(s)`}
				</button>
			{/if}
		</div>
	</header>

	{#if successMsg}
		<div class="mb-4 rounded-md border border-[var(--success-border)] bg-[var(--success-surface)] px-4 py-3 text-sm text-[var(--success-text)]">{successMsg}</div>
	{/if}
	{#if error}
		<div class="mb-4 rounded-md border border-[var(--danger-border)] bg-[var(--danger-surface)] px-4 py-3 text-sm text-[var(--danger-text)]"><strong>Error:</strong> {error}</div>
	{/if}

	{#if loading}
		<div class="mt-8 text-center text-sm text-[var(--muted)]">Parsing Tracks.bin…</div>
	{:else if tracks.length === 0 && !error}
		<div class="mt-8 text-center text-sm text-[var(--muted)]">No track blueprints found. The route may not have a Tracks.bin.</div>
	{:else}
		<div class="mt-2">
			<div class="mb-3 flex items-center gap-4">
				<h2 class="text-base font-semibold">Track Blueprints <span class="font-normal text-[var(--muted)]">({tracks.length})</span></h2>
				{#if pendingCount() > 0}
					<span class="text-[0.8rem] text-[var(--warn)]">{pendingCount()} pending replacement(s)</span>
				{/if}
			</div>

			<table class="w-full border-collapse text-[0.8rem]">
				<thead>
					<tr>
						<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Provider</th>
						<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Product</th>
						<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Blueprint ID</th>
						<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]">Replace with</th>
						<th class="border-b border-[var(--surface-raised)] px-2.5 py-1.5 text-left font-medium text-[var(--muted)]"></th>
					</tr>
				</thead>
				<tbody>
					{#each tracks as track (trackKey(track))}
						{@const repl = replacementFor(track)}
						<tr class={repl !== null ? 'bg-[#1a2020] hover:bg-[#1a2530]' : 'hover:bg-[var(--surface)]'}>
							<td class="whitespace-nowrap border-t border-[var(--border)] px-2.5 py-1.5 align-middle text-[var(--muted)]">{track.provider}</td>
							<td class="whitespace-nowrap border-t border-[var(--border)] px-2.5 py-1.5 align-middle text-[var(--muted-strong)]">{track.product}</td>
							<td class="max-w-80 truncate border-t border-[var(--border)] px-2.5 py-1.5 align-middle text-xs text-[var(--border-strong)]">{track.blueprintId}</td>
							<td class="max-w-96 border-t border-[var(--border)] px-2.5 py-1.5 align-middle">
								{#if repl}
									<div class="flex items-center gap-1 text-xs">
										<span class="whitespace-nowrap text-[var(--ok)]">{repl.provider}</span>
										<span class="shrink-0 text-[var(--surface-raised)]">›</span>
										<span class="whitespace-nowrap text-[var(--success-text)]">{repl.product}</span>
										<span class="shrink-0 text-[var(--surface-raised)]">›</span>
										<span class="truncate text-[var(--success-border)]">{repl.blueprintId}</span>
									</div>
								{:else}
									<span class="text-[var(--surface-raised)]">—</span>
								{/if}
							</td>
							<td class="flex gap-1 whitespace-nowrap border-t border-[var(--border)] px-2.5 py-1.5 align-middle">
								<button
									class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-2.5 py-1 text-[0.78rem] text-[var(--text)] hover:bg-[var(--surface-hover)]"
									onclick={() => openEditDialog(track)}
								>
									{repl ? 'Edit' : 'Set…'}
								</button>
								{#if repl}
									<button
										class="cursor-pointer rounded-md border border-[var(--danger-border)] bg-[var(--danger-border)] px-2.5 py-1 text-[0.78rem] text-white"
										onclick={() => clearReplacement(track)}
										title="Clear replacement">✕</button
									>
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
	<div class="fixed inset-0 z-[100] flex items-center justify-center bg-black/70" role="dialog" aria-modal="true">
		<div class="flex w-[min(560px,95vw)] flex-col gap-4 rounded-[10px] border border-[var(--surface-raised)] bg-[var(--surface)] p-6">
			<div class="flex items-center justify-between">
				<h2 class="text-base font-semibold">Set Replacement</h2>
				<button
					class="cursor-pointer border-0 bg-transparent p-1 text-base text-[var(--muted)] hover:text-[var(--text)]"
					onclick={() => (editingKey = null)}>✕</button
				>
			</div>

			{#if sourceTrack}
				<div class="flex items-baseline gap-2 rounded-md border border-[var(--surface-raised)] bg-[var(--bg)] px-3 py-2 text-[0.8rem]">
					<span class="shrink-0 text-[var(--muted)]">Replacing:</span>
					<span class="truncate text-[var(--muted-strong)]">{sourceTrack.provider} / {sourceTrack.product} / {sourceTrack.blueprintId}</span>
				</div>
			{/if}

			<div class="grid grid-cols-2 gap-x-4 gap-y-2">
				<label class="flex flex-col gap-1 text-[0.78rem] text-[var(--muted)]">
					Provider
					<input
						class="rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
						bind:value={editProvider}
						placeholder="e.g. DTG"
					/>
				</label>
				<label class="flex flex-col gap-1 text-[0.78rem] text-[var(--muted)]">
					Product
					<input
						class="rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
						bind:value={editProduct}
						placeholder="e.g. SomeTrackPack"
					/>
				</label>
				<label class="col-span-2 flex flex-col gap-1 text-[0.78rem] text-[var(--muted)]">
					Blueprint ID
					<input
						class="rounded border border-[var(--surface-raised)] bg-[var(--bg)] px-2 py-1 text-[0.8rem] text-[var(--text)] outline-none focus:border-[var(--accent)]"
						bind:value={editBlueprintId}
						placeholder="e.g. Track\TrackType.xml"
					/>
				</label>
			</div>

			<div class="flex justify-end gap-2 border-t border-[var(--surface-raised)] pt-2">
				<button
					class="cursor-pointer rounded-md border border-[var(--border-strong)] bg-[var(--surface-raised)] px-4 py-1.5 text-sm text-[var(--text)] hover:bg-[var(--surface-hover)]"
					onclick={() => (editingKey = null)}>Cancel</button
				>
				<button
					class="cursor-pointer rounded-md border border-[var(--primary-border)] bg-[var(--primary)] px-4 py-1.5 text-sm text-white hover:bg-[var(--primary-hover)] disabled:cursor-not-allowed disabled:opacity-50"
					onclick={confirmEdit}
					disabled={!editProvider || !editProduct || !editBlueprintId}
				>
					Set Replacement
				</button>
			</div>
		</div>
	</div>
{/if}
