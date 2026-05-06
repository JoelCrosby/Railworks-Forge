<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import type { ColumnDef, SortingState, Updater } from '@tanstack/table-core';
  import { getCoreRowModel, getSortedRowModel } from '@tanstack/table-core';
  import {
    createSvelteTable,
    DataTableHeader,
    getDataTableCellClass,
  } from '$lib/components/ui/data-table/index.js';
  import { Button } from '$lib/components/ui/button/index.js';
  import * as Table from '$lib/components/ui/table/index.js';
  import { t } from '$lib/i18n';
  import { settings } from '$lib/settings';
  import { setBreadcrumbs } from '$lib/stores/breadcrumb';
  import { clearRefreshControl } from '$lib/stores/refresh';

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
    imageDataUrl: string | null;
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
    imageDataUrl: string | null;
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

  const navState = $page.state as {
    route?: Route;
    scenario?: Scenario;
    consist?: Consist;
  };
  let route = $state<Route | null>(navState.route ?? null);
  let scenario = $state<Scenario | null>(navState.scenario ?? null);
  let consist = $state<Consist | null>(navState.consist ?? null);

  let routeId = $derived($page.params.routeId ?? '');
  let scenarioId = $derived($page.params.scenarioId ?? '');
  let consistId = $derived($page.params.consistId ?? '');
  let locale = $derived($settings.locale);

  let busy = $state(false);
  let error = $state<string | null>(null);
  let successMsg = $state<string | null>(null);
  let vehicleSorting = $state<SortingState>([]);

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
  let newType = $state<'engine' | 'tender' | 'coach' | 'wagon' | 'unknown'>(
    'wagon',
  );

  let vehicles = $derived(consist?.vehicles ?? []);

  const vehicleColumns: ColumnDef<VehicleBlueprint>[] = [
    {
      id: 'index',
      accessorFn: (vehicle) => vehicle.index + 1,
      header: '#',
      meta: {
        columnClass: 'w-12',
        cellClass: 'text-border-strong',
      },
    },
    {
      accessorKey: 'blueprintType',
      header: 'Type',
      meta: {
        columnClass: 'w-20',
      },
    },
    {
      accessorKey: 'name',
      header: 'Name',
      meta: {
        cellClass: 'font-medium',
      },
    },
    {
      accessorKey: 'uniqueNumber',
      header: 'Number',
      meta: {
        columnClass: 'w-32',
      },
    },
    {
      id: 'provider',
      accessorFn: (vehicle) => vehicle.blueprint.provider,
      header: 'Provider',
      meta: {
        columnClass: 'w-40',
        cellClass: 'truncate text-muted',
      },
    },
    {
      id: 'blueprint',
      accessorFn: (vehicle) => vehicle.blueprint.blueprintId,
      header: 'Blueprint',
      meta: {
        cellClass: 'truncate text-border-strong',
      },
    },
    {
      accessorKey: 'flipped',
      header: 'Flip',
      meta: {
        columnClass: 'w-20',
        headerAlign: 'center',
        cellAlign: 'center',
      },
    },
    {
      id: 'state',
      accessorFn: (vehicle) => vehicle.blueprint.acquisitionState,
      header: 'State',
      meta: {
        columnClass: 'w-20',
        headerAlign: 'center',
        cellAlign: 'center',
      },
    },
    {
      id: 'actions',
      header: '',
      enableSorting: false,
      meta: {
        columnClass: 'w-16',
      },
    },
  ];

  const vehicleTable = createSvelteTable<VehicleBlueprint>({
    get data() {
      return vehicles;
    },
    columns: vehicleColumns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getRowId: (vehicle) => String(vehicle.index),
    state: {
      get sorting() {
        return vehicleSorting;
      },
    },
    onSortingChange: (updater: Updater<SortingState>) => {
      vehicleSorting =
        updater instanceof Function ? updater(vehicleSorting) : updater;
    },
  });

  function backToScenario() {
    goto(
      `/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}`,
      {
        state: {
          route: route ? $state.snapshot(route) : null,
          scenario: scenario ? $state.snapshot(scenario) : null,
        },
      },
    );
  }

  function acquisitionIcon(s: string): string {
    return s === 'found' ? '✓' : s === 'partial' ? '~' : '✗';
  }

  function acquisitionClass(s: string): string {
    return s === 'found' ? 'found' : s === 'partial' ? 'partial' : 'missing';
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

  function vehicleBadgeClass(type: VehicleEntry['blueprintType']): string {
    const base =
      'inline-flex size-5 items-center justify-center rounded-[3px] text-[0.65rem] font-bold';
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
          vehicleIndex,
        },
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
    replaceEntries =
      consist?.vehicles.map((v) => ({
        provider: v.blueprint.provider,
        product: v.blueprint.product,
        blueprintId: v.blueprint.blueprintId,
        flipped: v.flipped,
        blueprintType: v.blueprintType,
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
        blueprintType: newType,
      },
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
        consist: { name: saveTemplateName.trim(), entries: replaceEntries },
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
          entries: replaceEntries,
        },
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
    if (
      !confirm(
        `Delete consist "${consist.serviceName}"? This cannot be undone.`,
      )
    )
      return;

    busy = true;
    error = null;
    try {
      const updated = await invoke<Scenario>('delete_consist', {
        request: { scenario, consistId: consist.id },
      });
      // Consist no longer exists; go back to scenario.
      scenario = updated;
      goto(
        `/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}`,
        {
          state: {
            route: route ? $state.snapshot(route) : null,
            scenario: $state.snapshot(updated),
          },
        },
      );
    } catch (e) {
      error = String(e);
    } finally {
      busy = false;
    }
  }

  $effect(() => {
    setBreadcrumbs([
      { label: t(locale, 'nav-routes'), href: '/' },
      {
        label: route?.name ?? `Route ${routeId}`,
        href: routeId ? `/routes/${encodeURIComponent(routeId)}` : undefined,
      },
      {
        label: scenario?.name ?? `Scenario ${scenarioId}`,
        href:
          routeId && scenarioId
            ? `/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}`
            : undefined,
      },
      {
        label:
          consist?.serviceName ||
          consist?.locomotiveName ||
          `Service ${consistId}`,
        href:
          routeId && scenarioId && consistId
            ? `/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenarioId)}/consists/${encodeURIComponent(consistId)}`
            : undefined,
      },
    ]);
  });

  $effect(() => {
    clearRefreshControl();
  });
</script>

<div class="px-6">
  {#if consist}
    <header class="mb-6 flex items-start justify-between gap-4">
      <div class="flex min-w-0 flex-1 items-start gap-4">
        {#if consist.imageDataUrl}
          <img
            src={consist.imageDataUrl}
            alt=""
            class="h-[76px] w-[136px] shrink-0 rounded-[4px] object-cover"
          />
        {/if}
        <div class="min-w-0">
          <h1 class="mb-1.5 text-[1.3rem] font-bold">
            {consist.serviceName || consist.locomotiveName || '—'}
          </h1>
          <div
            class="flex flex-wrap items-center gap-1.5 text-[0.8rem] text-muted"
          >
            <span class="italic">{consist.locomotiveName || '—'}</span>
            {#if consist.locoAuthor}
              <span class="text-border-strong">·</span>
              <span>{consist.locoAuthor}</span>
            {/if}
            <span class="text-border-strong">·</span>
            <span class={locoBadgeClass(consist.locoClass)}
              >{consist.locoClass}</span
            >
            {#if consist.playerDriver}
              <span class="text-border-strong">·</span>
              <span
                class="rounded bg-accent-surface px-1.5 py-0.5 text-[0.65rem] tracking-wider text-accent-text uppercase"
                >Player</span
              >
            {/if}
            <span class="text-border-strong">·</span>
            <span
              class={`text-xs font-bold ${acquisitionTextClass(consist.acquisitionState)}`}
            >
              {acquisitionIcon(consist.acquisitionState)}
            </span>
          </div>
        </div>
      </div>
      <div class="flex shrink-0 gap-2">
        <button
          class="cursor-pointer rounded-md border border-primary-border bg-primary px-4 py-1.5 text-sm text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          onclick={openReplaceDialog}
          disabled={busy}>Replace Consist</button
        >
        <button
          class="cursor-pointer rounded-md border border-danger-border bg-danger-border px-4 py-1.5 text-sm text-white disabled:cursor-not-allowed disabled:opacity-50"
          onclick={deleteConsist}
          disabled={busy}>Delete Consist</button
        >
      </div>
    </header>
  {:else}
    <header class="mb-6">
      <h1 class="text-[1.3rem] font-bold">Consist</h1>
    </header>
  {/if}

  {#if successMsg}
    <div
      class="mb-4 rounded-md border border-success-border bg-success-surface px-4 py-3 text-sm text-success-text"
    >
      {successMsg}
    </div>
  {/if}
  {#if error}
    <div
      class="mb-4 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"
    >
      <strong>Error:</strong>
      {error}
    </div>
  {/if}

  <!-- Vehicle list -->
  {#if consist}
    <section class="mt-2">
      <div class="mb-3 flex items-center gap-4">
        <h2 class="text-base font-semibold">
          Vehicles <span class="font-normal text-muted"
            >({consist.vehicles.length})</span
          >
        </h2>
      </div>

      {#if consist.vehicles.length === 0}
        <div class="mt-8 text-center text-sm text-muted">
          No vehicles in this consist.
        </div>
      {:else}
        <Table.Root
          class="table-fixed"
          containerClass="overflow-x-auto rounded-md border"
        >
          <Table.Header class="block w-full">
            {#each vehicleTable.getHeaderGroups() as headerGroup (headerGroup.id)}
              <Table.Row class="table w-full table-fixed">
                {#each headerGroup.headers as header (header.id)}
                  <DataTableHeader {header} />
                {/each}
              </Table.Row>
            {/each}
          </Table.Header>

          <Table.Body
            class="block max-h-[calc(100vh-330px)] overflow-y-auto [scrollbar-gutter:stable]"
          >
            {#each vehicleTable.getRowModel().rows as row (row.id)}
              {@const v = row.original}
              <Table.Row class="table w-full table-fixed hover:bg-surface">
                {#each row.getVisibleCells() as cell (cell.id)}
                  <Table.Cell class={getDataTableCellClass(cell)}>
                    {#if cell.column.id === 'index'}
                      {v.index + 1}
                    {:else if cell.column.id === 'blueprintType'}
                      <span
                        class={vehicleBadgeClass(v.blueprintType)}
                        title={v.blueprintType}
                      >
                        {v.blueprintType[0].toUpperCase()}
                      </span>
                    {:else if cell.column.id === 'name'}
                      {v.name || '—'}
                    {:else if cell.column.id === 'uniqueNumber'}
                      <span class="text-border-strong">#{v.uniqueNumber}</span>
                    {:else if cell.column.id === 'provider'}
                      {v.blueprint.provider}
                    {:else if cell.column.id === 'blueprint'}
                      {v.blueprint.blueprintId}
                    {:else if cell.column.id === 'flipped'}
                      {v.flipped ? '↩' : ''}
                    {:else if cell.column.id === 'state'}
                      <span
                        class={`text-xs font-bold ${acquisitionTextClass(v.blueprint.acquisitionState)}`}
                      >
                        {acquisitionIcon(v.blueprint.acquisitionState)}
                      </span>
                    {:else if cell.column.id === 'actions'}
                      <Button
                        variant="destructive"
                        size="xs"
                        onclick={() => deleteVehicle(v.index)}
                        disabled={busy}
                        title="Delete vehicle">✕</Button
                      >
                    {/if}
                  </Table.Cell>
                {/each}
              </Table.Row>
            {/each}
          </Table.Body>
        </Table.Root>
      {/if}
    </section>
  {/if}
</div>

<!-- Replace Consist Dialog -->
{#if showReplaceDialog}
  <div
    class="fixed inset-0 z-100 flex items-center justify-center bg-black/70"
    role="dialog"
    aria-modal="true"
  >
    <div
      class="flex max-h-[85vh] w-[min(720px,95vw)] flex-col gap-4 overflow-y-auto rounded-[10px] border border-surface-raised bg-surface p-6"
    >
      <div class="flex items-center justify-between">
        <h2 class="text-base font-semibold">Replace Consist</h2>
        <button
          class="cursor-pointer border-0 bg-transparent p-1 text-base text-muted hover:text-text"
          onclick={() => (showReplaceDialog = false)}>✕</button
        >
      </div>

      {#if replaceError}
        <div
          class="mb-4 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"
        >
          <strong>Error:</strong>
          {replaceError}
        </div>
      {/if}

      <!-- Saved templates -->
      {#if savedConsists.length > 0}
        <div class="flex flex-wrap items-center gap-2">
          <span class="text-[0.8rem] text-muted">Load template:</span>
          {#each savedConsists as t}
            <button
              class="cursor-pointer rounded-md border border-border-strong bg-surface-raised px-2.5 py-1 text-[0.78rem] text-text hover:bg-surface-hover"
              onclick={() => loadTemplate(t)}>{t.name}</button
            >
          {/each}
        </div>
      {/if}

      <!-- Current replacement vehicle list -->
      <div
        class="flex max-h-50 flex-col gap-1 overflow-y-auto rounded-md border border-surface-raised p-2"
      >
        {#if replaceEntries.length === 0}
          <div class="mt-8 text-center text-sm text-muted">
            Add vehicles below.
          </div>
        {:else}
          {#each replaceEntries as entry, i}
            <div
              class="flex items-center gap-2 rounded px-1.5 py-1 text-[0.78rem] hover:bg-bg"
            >
              <span class="w-5 shrink-0 text-right text-border-strong"
                >{i + 1}</span
              >
              <span class={`${vehicleBadgeClass(entry.blueprintType)} shrink-0`}
                >{entry.blueprintType[0].toUpperCase()}</span
              >
              <span class="shrink-0 text-muted">{entry.provider}</span>
              <span class="shrink-0 text-muted">{entry.product}</span>
              <span class="flex-1 truncate whitespace-nowrap text-border-strong"
                >{entry.blueprintId}</span
              >
              {#if entry.flipped}<span class="shrink-0 text-muted">↩</span
                >{/if}
              <button
                class="cursor-pointer rounded-md border border-danger-border bg-danger-border px-2.5 py-1 text-[0.78rem] text-white"
                onclick={() => removeReplaceEntry(i)}>✕</button
              >
            </div>
          {/each}
        {/if}
      </div>

      <!-- Add vehicle form -->
      <details class="rounded-md border border-surface-raised p-3">
        <summary class="cursor-pointer text-sm text-muted select-none"
          >Add vehicle</summary
        >
        <div class="mt-3 grid grid-cols-2 gap-x-4 gap-y-2">
          <label class="flex flex-col gap-1 text-[0.78rem] text-muted">
            Type
            <select
              class="rounded border border-surface-raised bg-bg px-2 py-1 text-[0.8rem] text-text outline-none focus:border-accent"
              bind:value={newType}
            >
              <option value="engine">Engine</option>
              <option value="tender">Tender</option>
              <option value="wagon">Wagon</option>
              <option value="coach">Coach</option>
            </select>
          </label>
          <label class="flex flex-col gap-1 text-[0.78rem] text-muted">
            Provider
            <input
              class="rounded border border-surface-raised bg-bg px-2 py-1 text-[0.8rem] text-text outline-none focus:border-accent"
              bind:value={newProvider}
              placeholder="e.g. DTG"
            />
          </label>
          <label class="flex flex-col gap-1 text-[0.78rem] text-muted">
            Product
            <input
              class="rounded border border-surface-raised bg-bg px-2 py-1 text-[0.8rem] text-text outline-none focus:border-accent"
              bind:value={newProduct}
              placeholder="e.g. SomeProduct"
            />
          </label>
          <label
            class="col-span-2 flex flex-col gap-1 text-[0.78rem] text-muted"
          >
            Blueprint ID
            <input
              class="rounded border border-surface-raised bg-bg px-2 py-1 text-[0.8rem] text-text outline-none focus:border-accent"
              bind:value={newBlueprintId}
              placeholder="RailVehicles\Engines\Foo.xml"
            />
          </label>
          <label
            class="flex flex-row items-center gap-2 text-[0.78rem] text-text"
          >
            <input type="checkbox" bind:checked={newFlipped} />
            Flipped
          </label>
          <button
            class="cursor-pointer rounded-md border border-primary-border bg-primary px-4 py-1.5 text-sm text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
            onclick={addReplaceEntry}
            disabled={!newProvider || !newProduct || !newBlueprintId}
            >Add</button
          >
        </div>
      </details>

      <!-- Save as template -->
      {#if replaceEntries.length > 0}
        {#if showSaveTemplate}
          <div class="flex items-center gap-2">
            <input
              class="flex-1 rounded border border-surface-raised bg-bg px-2 py-1 text-[0.8rem] text-text outline-none focus:border-accent"
              bind:value={saveTemplateName}
              placeholder="Template name…"
            />
            <button
              class="cursor-pointer rounded-md border border-primary-border bg-primary px-4 py-1.5 text-sm text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
              onclick={saveTemplate}
              disabled={!saveTemplateName.trim()}>Save</button
            >
            <button
              class="cursor-pointer rounded-md border border-border-strong bg-surface-raised px-4 py-1.5 text-sm text-text hover:bg-surface-hover"
              onclick={() => (showSaveTemplate = false)}>Cancel</button
            >
          </div>
        {:else}
          <button
            class="cursor-pointer rounded-md border border-border-strong bg-surface-raised px-2.5 py-1 text-[0.78rem] text-text hover:bg-surface-hover"
            onclick={() => (showSaveTemplate = true)}>Save as template…</button
          >
        {/if}
      {/if}

      <div class="flex justify-end gap-2 border-t border-surface-raised pt-2">
        <button
          class="cursor-pointer rounded-md border border-border-strong bg-surface-raised px-4 py-1.5 text-sm text-text hover:bg-surface-hover"
          onclick={() => (showReplaceDialog = false)}>Cancel</button
        >
        <button
          class="cursor-pointer rounded-md border border-primary-border bg-primary px-4 py-1.5 text-sm text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          onclick={confirmReplace}
          disabled={busy || replaceEntries.length === 0}
        >
          {busy ? 'Applying…' : `Replace (${replaceEntries.length} vehicles)`}
        </button>
      </div>
    </div>
  </div>
{/if}
