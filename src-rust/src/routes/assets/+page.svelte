<script lang="ts">
	import { invoke } from '@tauri-apps/api/core';
	import { goto } from '$app/navigation';
	import { t } from '$lib/i18n';
	import { settings } from '$lib/settings';

	interface AssetNode {
		provider: string;
		product: string;
		hasRailVehicles: boolean;
		hasPreloadData: boolean;
	}

	interface ProviderGroup {
		name: string;
		products: AssetNode[];
	}

	let nodes = $state<AssetNode[]>([]);
	let loading = $state(false);
	let error = $state<string | null>(null);
	let search = $state('');
	let filterRailVehicles = $state(false);
	let locale = $derived($settings.locale);

	let groups = $derived.by<ProviderGroup[]>(() => {
		const filtered = nodes.filter((n) => {
			if (filterRailVehicles && !n.hasRailVehicles) return false;
			if (search.trim()) {
				const q = search.toLowerCase();
				return n.provider.toLowerCase().includes(q) || n.product.toLowerCase().includes(q);
			}
			return true;
		});

		const map = new Map<string, AssetNode[]>();
		for (const node of filtered) {
			if (!map.has(node.provider)) map.set(node.provider, []);
			map.get(node.provider)!.push(node);
		}

		return Array.from(map.entries())
			.sort(([a], [b]) => a.localeCompare(b))
			.map(([name, products]) => ({ name, products }));
	});

	let totalProducts = $derived(nodes.length);
	let filteredCount = $derived(groups.reduce((sum, g) => sum + g.products.length, 0));

	async function load() {
		loading = true;
		error = null;
		nodes = [];
		try {
			nodes = await invoke<AssetNode[]>('get_asset_tree');
		} catch (e) {
			error = String(e);
		} finally {
			loading = false;
		}
	}

	$effect(() => {
		load();
	});
</script>

<div class="mx-auto max-w-275 p-6">
	<nav class="mb-4">
		<button
			class="cursor-pointer border-0 bg-transparent p-0 text-sm text-accent hover:underline"
			onclick={() => goto('/')}>← {t(locale, 'nav-routes')}</button
		>
	</nav>

	<header class="mb-6 flex items-start justify-between gap-4">
		<div>
			<h1 class="text-[1.3rem] font-bold">{t(locale, 'assets-title')}</h1>
			{#if !loading && nodes.length > 0}
				<p class="mt-1 text-[0.8rem] text-muted">{filteredCount} / {totalProducts} products</p>
			{/if}
		</div>
		<button
			class="shrink-0 cursor-pointer rounded-md border border-border-strong bg-surface-raised px-4 py-1.5 text-sm text-text hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
			onclick={load}
			disabled={loading}
		>
			{loading ? t(locale, 'action-loading') : t(locale, 'action-refresh')}
		</button>
	</header>

	{#if error}
		<div class="mb-6 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"><strong>{t(locale, 'error-label')}:</strong> {error}</div>
	{/if}

	{#if !loading && nodes.length > 0}
		<div class="mb-4 flex items-center gap-4">
			<input
				class="flex-1 rounded-md border border-surface-raised bg-surface px-3 py-2 text-sm text-text outline-none focus:border-accent"
				type="search"
				placeholder={t(locale, 'assets-search')}
				bind:value={search}
			/>
			<label class="flex cursor-pointer items-center gap-1.5 whitespace-nowrap text-[0.8rem] text-muted">
				<input type="checkbox" bind:checked={filterRailVehicles} />
				{t(locale, 'assets-railvehicles-only')}
			</label>
		</div>
	{/if}

	{#if loading}
		<div class="mt-8 text-center text-sm text-muted">{t(locale, 'assets-scanning')}</div>
	{:else if groups.length === 0 && !error}
		<div class="mt-8 text-center text-sm text-muted">{nodes.length === 0 ? t(locale, 'assets-no-assets') : t(locale, 'assets-no-matches')}</div>
	{:else}
		<div class="flex flex-col gap-2">
			{#each groups as group (group.name)}
				<details
					class="overflow-hidden rounded-lg border border-surface-raised bg-surface [&[open]>summary]:border-b [&[open]>summary]:border-surface-raised"
					open={groups.length <= 6}
				>
					<summary class="flex cursor-pointer list-none items-center gap-2 px-4 py-2.5 text-sm font-semibold select-none [&::-webkit-details-marker]:hidden">
						{group.name}
						<span class="text-[0.8rem] font-normal text-muted">({group.products.length})</span>
					</summary>

					<div class="grid grid-cols-[repeat(auto-fill,minmax(220px,1fr))] gap-1.5 p-2">
						{#each group.products as node (node.product)}
							<div class="flex items-center justify-between gap-2 rounded-md border border-surface-raised bg-bg px-3 py-2">
								<span class="truncate text-[0.8rem]">{node.product}</span>
								<div class="flex shrink-0 gap-1">
									{#if node.hasRailVehicles}
										<span
											class="rounded-[3px] bg-accent-surface px-1.5 py-0.5 text-[0.6rem] font-bold tracking-wide text-accent-text"
											title="Contains RailVehicles">R</span
										>
									{/if}
									{#if node.hasPreloadData}
										<span
											class="rounded-[3px] bg-success-border px-1.5 py-0.5 text-[0.6rem] font-bold tracking-wide text-success-text"
											title="Contains PreloadData">P</span
										>
									{/if}
								</div>
							</div>
						{/each}
					</div>
				</details>
			{/each}
		</div>
	{/if}
</div>
