<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import type { ColumnDef, SortingState, Updater } from '@tanstack/table-core';
  import { getCoreRowModel, getSortedRowModel } from '@tanstack/table-core';
  import {
    createSvelteTable,
    DataTableHeader,
  } from '$lib/components/ui/data-table/index.js';
  import { Badge } from '$lib/components/ui/badge/index.js';
  import { Button } from '$lib/components/ui/button/index.js';
  import * as Table from '$lib/components/ui/table/index.js';
  import { t } from '$lib/i18n';
  import { settings } from '$lib/settings';
  import { setBreadcrumbs } from '$lib/stores/breadcrumb';
  import { setRefreshControl } from '$lib/stores/refresh';

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
    playerInfo: {
      scenarioId: string;
      score: number;
      completion: string;
      medalsAwarded: number;
    };
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
  let sorting = $state<SortingState>([]);
  let locale = $derived($settings.locale);

  let consists = $derived(detail?.consists ?? []);
  let filtered = $derived(
    search.trim()
      ? consists.filter((c) =>
          [c.serviceName, c.locomotiveName, c.locoAuthor ?? '']
            .join(' ')
            .toLowerCase()
            .includes(search.toLowerCase()),
        )
      : consists,
  );

  const consistColumns: ColumnDef<Consist>[] = [
    {
      accessorKey: 'serviceName',
      header: 'Service',
    },
    {
      accessorKey: 'locomotiveName',
      header: 'Locomotive',
    },
    {
      accessorKey: 'locoClass',
      header: 'Class',
      meta: {
        headerClass: 'w-28',
      },
    },
    {
      id: 'vehicles',
      accessorFn: (consist) => consist.vehicles.length,
      header: 'Vehicles',
      meta: {
        headerClass: 'w-24 text-right',
        headerAlign: 'right',
      },
    },
    {
      accessorKey: 'acquisitionState',
      header: 'State',
      meta: {
        headerClass: 'w-20 text-center',
        headerAlign: 'right',
      },
    },
    {
      id: 'actions',
      header: '',
      enableSorting: false,
      meta: {
        headerClass: 'w-20',
      },
    },
  ];

  const consistTable = createSvelteTable<Consist>({
    get data() {
      return filtered;
    },
    columns: consistColumns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getRowId: (consist) => consist.id || consist.serviceId,
    state: {
      get sorting() {
        return sorting;
      },
    },
    onSortingChange: (updater: Updater<SortingState>) => {
      sorting = updater instanceof Function ? updater(sorting) : updater;
    },
  });

  async function loadDetail() {
    if (!scenarioBase) return;
    loading = true;
    error = null;
    detail = null;
    try {
      detail = await invoke<Scenario>('get_scenario_detail', {
        scenario: scenarioBase,
      });
    } catch (e) {
      error = String(e);
    } finally {
      loading = false;
    }
  }

  function backToRoute() {
    goto(`/routes/${encodeURIComponent(routeId)}`, {
      state: { route: route ? $state.snapshot(route) : null },
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
          consist: $state.snapshot(consist),
        },
      },
    );
  }

  function acquisitionIcon(state: string): string {
    return state === 'found' ? '✓' : state === 'partial' ? '~' : '✗';
  }

  function acquisitionClass(state: string): string {
    return state === 'found'
      ? 'found'
      : state === 'partial'
        ? 'partial'
        : 'missing';
  }

  function acquisitionTextClass(state: string): string {
    return state === 'found'
      ? 'text-ok'
      : state === 'partial'
        ? 'text-warn'
        : 'text-danger-text';
  }

  function locoBadgeClass(locoClass: Consist['locoClass']): string {
    const base =
      'rounded-[3px] px-1.5 py-0.5 text-[0.65rem] tracking-wide uppercase';
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
    const base =
      'inline-flex size-5 shrink-0 items-center justify-center rounded-[3px] text-[0.65rem] font-bold';
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

  $effect(() => {
    setBreadcrumbs([
      { label: t(locale, 'nav-routes'), href: '/' },
      {
        label: route?.name ?? `Route ${routeId}`,
        href: routeId ? `/routes/${encodeURIComponent(routeId)}` : undefined,
      },
      {
        label: scenarioBase?.name ?? `Scenario ${scenarioId}`,
        href:
          routeId && scenarioId
            ? `/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}`
            : undefined,
      },
    ]);
  });

  $effect(() => {
    setRefreshControl({
      onRefresh: loadDetail,
      disabled: !scenarioBase || loading,
      loading,
    });
  });
</script>

<div class="px-6">
  {#if scenarioBase}
    <header class="mb-6 flex items-start justify-between gap-4">
      <div class="flex-1">
        <h1 class="mb-1.5 text-[1.3rem] font-bold">{scenarioBase.name}</h1>
        <div
          class="flex flex-wrap items-center gap-1.5 text-[0.8rem] text-muted"
        >
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
          <p class="mt-2 text-[0.82rem] leading-6 text-muted">
            {scenarioBase.description}
          </p>
        {/if}
      </div>
    </header>
  {:else}
    <header class="mb-6">
      <h1 class="text-[1.3rem] font-bold">Scenario {scenarioId}</h1>
    </header>
  {/if}

  {#if error}
    <div
      class="mb-6 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"
    >
      <strong>Error:</strong>
      {error}
    </div>
  {/if}

  {#if loading}
    <div class="mt-8 text-center text-sm text-muted">Parsing Scenario.bin…</div>
  {:else if detail}
    <div class="mt-2">
      <div class="mb-3 flex items-center gap-4">
        <h2 class="text-base font-semibold">
          Consists <span class="font-normal text-muted"
            >({consists.length})</span
          >
        </h2>
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
        <div class="mt-8 text-center text-sm text-muted">
          No consists found in this scenario.
        </div>
      {:else}
        <Table.Root
          class="table-fixed"
          containerClass="overflow-x-auto rounded-md border"
        >
          <Table.Header class="block w-full">
            {#each consistTable.getHeaderGroups() as headerGroup (headerGroup.id)}
              <Table.Row class="table w-full table-fixed">
                {#each headerGroup.headers as header (header.id)}
                  <DataTableHeader {header} />
                {/each}
              </Table.Row>
            {/each}
          </Table.Header>

          <Table.Body
            class="block max-h-[calc(100vh-360px)] overflow-y-auto [scrollbar-gutter:stable]"
          >
            {#each consistTable.getRowModel().rows as row (row.id)}
              {@const consist = row.original}
              <Table.Row class="table w-full table-fixed">
                {#each row.getVisibleCells() as cell (cell.id)}
                  <Table.Cell
                    class={cell.column.id === 'vehicles'
                      ? 'text-right'
                      : cell.column.id === 'acquisitionState'
                        ? 'text-center'
                        : cell.column.id === 'actions'
                          ? 'text-right'
                          : ''}
                  >
                    {#if cell.column.id === 'serviceName'}
                      <div class="flex items-center gap-2">
                        <span class="font-medium"
                          >{consist.serviceName || '—'}</span
                        >
                        {#if consist.playerDriver}
                          <Badge variant="outline" class="text-accent-text"
                            >Player</Badge
                          >
                        {/if}
                      </div>
                    {:else if cell.column.id === 'locomotiveName'}
                      <div class="flex min-w-0 items-center gap-2 text-muted">
                        <span class="truncate italic"
                          >{consist.locomotiveName || '—'}</span
                        >
                        {#if consist.locoAuthor}
                          <span class="truncate text-[0.73rem] text-border-strong"
                            >{consist.locoAuthor}</span
                          >
                        {/if}
                      </div>
                    {:else if cell.column.id === 'locoClass'}
                      <Badge
                        variant="outline"
                        class={locoBadgeClass(consist.locoClass)}
                        >{consist.locoClass}</Badge
                      >
                    {:else if cell.column.id === 'vehicles'}
                      {consist.vehicles.length}
                    {:else if cell.column.id === 'acquisitionState'}
                      <span
                        class={`text-xs font-bold ${acquisitionTextClass(consist.acquisitionState)}`}
                        title={consist.acquisitionState}
                      >
                        {acquisitionIcon(consist.acquisitionState)}
                      </span>
                    {:else if cell.column.id === 'actions'}
                      <Button
                        variant="outline"
                        size="xs"
                        onclick={() => openConsistDetail(consist)}>Edit</Button
                      >
                    {/if}
                  </Table.Cell>
                {/each}
              </Table.Row>

              {#if consist.vehicles.length > 0}
                <Table.Row class="table w-full table-fixed bg-surface/40">
                  <Table.Cell colspan={consistColumns.length} class="p-0">
                    <div class="py-1">
                      {#each consist.vehicles as vehicle (vehicle.index)}
                        <div
                          class="flex items-center gap-2 border-t border-border px-4 py-1.5 text-[0.78rem] hover:bg-border"
                        >
                          <span
                            class={vehicleBadgeClass(vehicle.blueprintType)}
                            title={vehicle.blueprintType}
                          >
                            {vehicle.blueprintType[0].toUpperCase()}
                          </span>
                          <span
                            class="flex-2 truncate whitespace-nowrap font-medium"
                            >{vehicle.name || '—'}</span
                          >
                          <span class="whitespace-nowrap text-border-strong"
                            >#{vehicle.uniqueNumber}</span
                          >
                          <span
                            class="flex-1 truncate whitespace-nowrap text-[0.72rem] text-border-strong"
                            >{vehicle.blueprint.provider}</span
                          >
                          {#if vehicle.flipped}
                            <span
                              class="text-[0.85rem] text-muted"
                              title="Flipped">↩</span
                            >
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
                  </Table.Cell>
                </Table.Row>
              {/if}
            {/each}
          </Table.Body>
        </Table.Root>
      {/if}
    </div>
  {/if}
</div>
