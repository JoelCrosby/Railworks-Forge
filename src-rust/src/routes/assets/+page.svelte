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

<div class="page">
	<nav>
		<button class="back" onclick={() => goto('/')}>← {t(locale, 'nav-routes')}</button>
	</nav>

	<header>
		<div>
			<h1>{t(locale, 'assets-title')}</h1>
			{#if !loading && nodes.length > 0}
				<p class="subtitle">{filteredCount} / {totalProducts} products</p>
			{/if}
		</div>
		<button onclick={load} disabled={loading}>
			{loading ? t(locale, 'action-loading') : t(locale, 'action-refresh')}
		</button>
	</header>

	{#if error}
		<div class="error"><strong>{t(locale, 'error-label')}:</strong> {error}</div>
	{/if}

	{#if !loading && nodes.length > 0}
		<div class="toolbar">
			<input class="search" type="search" placeholder={t(locale, 'assets-search')} bind:value={search} />
			<label class="filter-label">
				<input type="checkbox" bind:checked={filterRailVehicles} />
				{t(locale, 'assets-railvehicles-only')}
			</label>
		</div>
	{/if}

	{#if loading}
		<div class="status">{t(locale, 'assets-scanning')}</div>
	{:else if groups.length === 0 && !error}
		<div class="empty">{nodes.length === 0 ? t(locale, 'assets-no-assets') : t(locale, 'assets-no-matches')}</div>
	{:else}
		<div class="provider-list">
			{#each groups as group (group.name)}
				<details class="provider-group" open={groups.length <= 6}>
					<summary class="provider-name">
						{group.name}
						<span class="provider-count">({group.products.length})</span>
					</summary>

					<div class="product-grid">
						{#each group.products as node (node.product)}
							<div class="product-card">
								<span class="product-name">{node.product}</span>
								<div class="flags">
									{#if node.hasRailVehicles}
										<span class="flag rail" title="Contains RailVehicles">R</span>
									{/if}
									{#if node.hasPreloadData}
										<span class="flag preload" title="Contains PreloadData">P</span>
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
		font-size: 0.8rem;
		color: var(--muted);
		margin-top: 0.2rem;
	}

	button {
		background: var(--surface-raised);
		color: var(--text);
		border: 1px solid var(--border-strong);
		border-radius: 6px;
		padding: 0.4rem 1rem;
		font-size: 0.875rem;
		cursor: pointer;
		flex-shrink: 0;
	}

	button:hover:not(:disabled) {
		background: var(--surface-hover);
	}

	button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.toolbar {
		display: flex;
		align-items: center;
		gap: 1rem;
		margin-bottom: 1rem;
	}

	.search {
		flex: 1;
		background: var(--surface);
		border: 1px solid var(--surface-raised);
		border-radius: 6px;
		padding: 0.45rem 0.75rem;
		color: var(--text);
		font-size: 0.875rem;
		outline: none;
	}

	.search:focus {
		border-color: var(--accent);
	}

	.filter-label {
		display: flex;
		align-items: center;
		gap: 0.4rem;
		font-size: 0.8rem;
		color: var(--muted);
		cursor: pointer;
		white-space: nowrap;
	}

	.status,
	.empty {
		color: var(--muted);
		font-size: 0.9rem;
		margin-top: 2rem;
		text-align: center;
	}

	.error {
		background: var(--danger-surface);
		border: 1px solid var(--danger-border);
		border-radius: 6px;
		padding: 0.75rem 1rem;
		font-size: 0.875rem;
		color: var(--danger-text);
		margin-bottom: 1.5rem;
	}

	.provider-list {
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.provider-group {
		background: var(--surface);
		border: 1px solid var(--surface-raised);
		border-radius: 8px;
		overflow: hidden;
	}

	.provider-name {
		padding: 0.65rem 1rem;
		font-weight: 600;
		font-size: 0.9rem;
		cursor: pointer;
		user-select: none;
		list-style: none;
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.provider-name::-webkit-details-marker {
		display: none;
	}

	.provider-group[open] .provider-name {
		border-bottom: 1px solid var(--surface-raised);
	}

	.provider-count {
		color: var(--muted);
		font-weight: 400;
		font-size: 0.8rem;
	}

	.product-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
		gap: 0.4rem;
		padding: 0.5rem;
	}

	.product-card {
		background: var(--bg);
		border: 1px solid var(--surface-raised);
		border-radius: 6px;
		padding: 0.5rem 0.75rem;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.5rem;
	}

	.product-name {
		font-size: 0.8rem;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.flags {
		display: flex;
		gap: 0.25rem;
		flex-shrink: 0;
	}

	.flag {
		font-size: 0.6rem;
		font-weight: 700;
		letter-spacing: 0.03em;
		padding: 0.1rem 0.3rem;
		border-radius: 3px;
	}

	.flag.rail {
		background: var(--accent-surface);
		color: var(--accent-text);
	}

	.flag.preload {
		background: var(--success-border);
		color: var(--success-text);
	}
</style>
