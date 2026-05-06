<script lang="ts" generics="TData, TValue">
  import ArrowDownIcon from '@lucide/svelte/icons/arrow-down';
  import ArrowUpIcon from '@lucide/svelte/icons/arrow-up';
  import ArrowUpDownIcon from '@lucide/svelte/icons/arrow-up-down';
  import type { Header } from '@tanstack/table-core';
  import { Button } from '$lib/components/ui/button/index.js';
  import * as Table from '$lib/components/ui/table/index.js';
  import { cn } from '$lib/utils.js';
  import { FlexRender } from './index.js';

  type Props = {
    header: Header<TData, TValue>;
    class?: string;
    align?: 'left' | 'right';
  };

  type HeaderMeta = {
    headerClass?: string;
    headerAlign?: 'left' | 'right';
  };

  let { header, class: className, align }: Props = $props();

  let sortDirection = $derived(header.column.getIsSorted());
  let headerMeta = $derived(
    header.column.columnDef.meta as HeaderMeta | undefined,
  );
  let resolvedClass = $derived(
    cn(
      'sticky top-0 z-20 bg-bg shadow-[inset_0_-1px_0_var(--border)]',
      headerMeta?.headerClass,
      className,
    ),
  );
  let resolvedAlign = $derived(align ?? headerMeta?.headerAlign ?? 'left');
  let ariaSort = $derived<'ascending' | 'descending' | undefined>(
    sortDirection === 'asc'
      ? 'ascending'
      : sortDirection === 'desc'
        ? 'descending'
        : undefined,
  );
  let buttonClass = $derived(
    resolvedAlign === 'right' ? 'ml-auto -mr-2' : '-ml-2',
  );
</script>

<Table.Head class={resolvedClass} aria-sort={ariaSort}>
  {#if !header.isPlaceholder}
    {#if header.column.getCanSort()}
      <Button
        variant="ghost"
        size="sm"
        class={buttonClass}
        onclick={header.column.getToggleSortingHandler()}
      >
        <FlexRender
          content={header.column.columnDef.header}
          context={header.getContext()}
        />
        {#if sortDirection === 'asc'}
          <ArrowUpIcon class="size-3.5" />
        {:else if sortDirection === 'desc'}
          <ArrowDownIcon class="size-3.5" />
        {:else}
          <ArrowUpDownIcon class="size-3.5 opacity-55" />
        {/if}
      </Button>
    {:else}
      <FlexRender
        content={header.column.columnDef.header}
        context={header.getContext()}
      />
    {/if}
  {/if}
</Table.Head>
