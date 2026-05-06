<script lang="ts">
	interface BreadcrumbItem {
		label: string;
		onclick?: () => void;
	}

	let { items }: { items: BreadcrumbItem[] } = $props();
</script>

<nav class="mb-4 flex items-center gap-1.5 text-sm">
	{#each items as item, i}
		{#if i > 0}
			<span class="text-border-strong">/</span>
		{/if}
		{#if i === items.length - 1}
			<span class="text-muted-strong truncate">{item.label}</span>
		{:else}
			<button
				class="cursor-pointer border-0 bg-transparent p-0 text-accent hover:underline"
				onclick={item.onclick}
			>{item.label}</button>
		{/if}
	{/each}
</nav>
