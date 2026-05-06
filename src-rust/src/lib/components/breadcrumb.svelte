<script lang="ts">
	import * as Breadcrumb from '$lib/components/ui/breadcrumb/index.js';
	import { breadcrumbItems } from '$lib/stores/breadcrumb';
</script>

<Breadcrumb.Root>
	<Breadcrumb.List class="min-w-0 flex-nowrap">
		{#each $breadcrumbItems as item, i (item.href ?? `${item.label}-${i}`)}
			<Breadcrumb.Item class="min-w-0">
				{#if i === $breadcrumbItems.length - 1 || !item.href}
					<Breadcrumb.Page class="inline-block max-w-60 truncate"
						>{item.label}</Breadcrumb.Page
					>
				{:else}
					<Breadcrumb.Link class="inline-block max-w-44 truncate" href={item.href}
						>{item.label}</Breadcrumb.Link
					>
				{/if}
			</Breadcrumb.Item>
			{#if i < $breadcrumbItems.length - 1}
				<Breadcrumb.Separator />
			{/if}
		{/each}
	</Breadcrumb.List>
</Breadcrumb.Root>
