<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { Channel } from '@tauri-apps/api/core';
  import { goto } from '$app/navigation';
  import type { ColumnDef, SortingState, Updater } from '@tanstack/table-core';
  import { getCoreRowModel, getSortedRowModel } from '@tanstack/table-core';
  import {
    createSvelteTable,
    DataTableHeader,
    getDataTableCellClass,
  } from '$lib/components/ui/data-table/index.js';
  import { Badge } from '$lib/components/ui/badge/index.js';
  import * as Table from '$lib/components/ui/table/index.js';
  import { t } from '$lib/i18n';
  import { settings } from '$lib/settings';
  import { setBreadcrumbs } from '$lib/stores/breadcrumb';
  import { setRefreshControl } from '$lib/stores/refresh';

  interface Route {
    id: string;
    name: string;
    description: string | null;
    directoryPath: string;
    packagingType: 'packed' | 'unpacked';
    imageDataUrl: string | null;
  }

  interface ProgressEvent {
    current: number;
    total: number;
    message: string;
  }

  let routes = $state<Route[]>([]);
  let loading = $state(false);
  let error = $state<string | null>(null);
  let progress = $state<string | null>(null);
  let openingRouteId = $state<string | null>(null);
  let sorting = $state<SortingState>([]);

  // Game path state
  let gamePathMissing = $state(false);
  let locale = $derived($settings.locale);

  const PATH_MISSING_HINT = 'could not locate railworks';
  const routeColumns: ColumnDef<Route>[] = [
    {
      id: 'image',
      header: '',
      enableSorting: false,
      meta: {
        columnClass: 'w-36',
      },
    },
    {
      accessorKey: 'name',
      header: 'Route',
    },
    {
      accessorKey: 'packagingType',
      header: 'Type',
      meta: {
        columnClass: 'w-32',
        headerAlign: 'right',
        cellAlign: 'right',
      },
    },
  ];

  const table = createSvelteTable<Route>({
    get data() {
      return routes;
    },
    columns: routeColumns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getRowId: (route) => route.id,
    state: {
      get sorting() {
        return sorting;
      },
    },
    onSortingChange: (updater: Updater<SortingState>) => {
      sorting = updater instanceof Function ? updater(sorting) : updater;
    },
  });

  function isPathMissingError(msg: string): boolean {
    return msg.toLowerCase().includes(PATH_MISSING_HINT);
  }

  async function loadRoutes() {
    loading = true;
    error = null;
    progress = null;
    routes = [];
    gamePathMissing = false;

    try {
      const channel = new Channel<ProgressEvent>();
      channel.onmessage = (msg) => {
        progress = msg.message;
      };

      routes = await invoke<Route[]>('get_routes', { onProgress: channel });
      progress = null;

      await invoke<string>('get_game_path').catch(() => null);
    } catch (e) {
      const msg = String(e);
      if (isPathMissingError(msg)) {
        gamePathMissing = true;
      } else {
        error = msg;
      }
    } finally {
      loading = false;
    }
  }

  async function openRoute(route: Route) {
    if (openingRouteId !== null) return;
    openingRouteId = route.id;
    error = null;
    try {
      await goto(`/routes/${encodeURIComponent(route.id)}`, {
        state: { route: $state.snapshot(route) },
      });
    } catch (e) {
      error = `Could not open route: ${String(e)}`;
      openingRouteId = null;
    }
  }

  function openRouteFromKeyboard(event: KeyboardEvent, route: Route) {
    if (event.key !== 'Enter' && event.key !== ' ') return;
    event.preventDefault();
    openRoute(route);
  }

  $effect(() => {
    setBreadcrumbs([{ label: t(locale, 'nav-routes'), href: '/' }]);
  });

  $effect(() => {
    setRefreshControl({
      onRefresh: loadRoutes,
      disabled: loading,
      loading,
    });
  });

  $effect(() => {
    // Load current game path for display, then load routes
    invoke<string>('get_game_path')
      .then((p) => {})
      .catch(() => {})
      .finally(() => loadRoutes());
  });
</script>

<div class="px-6">
  {#if error}
    <div
      class="mb-6 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"
    >
      <strong>{t(locale, 'error-label')}:</strong>
      {error}
    </div>
  {/if}

  {#if gamePathMissing}
    <div
      class="mb-6 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"
    >
      {t(locale, 'home-game-path-missing')}
      <button
        class="ml-2 cursor-pointer rounded-md border border-border-strong bg-surface-raised px-3 py-1 text-sm text-text hover:bg-surface-hover"
        onclick={() => goto('/settings')}>{t(locale, 'nav-settings')}</button
      >
    </div>
  {/if}

  {#if loading}
    <div class="mt-8 text-center text-sm text-muted">
      {progress ?? t(locale, 'home-scanning-routes')}
    </div>
  {:else if routes.length === 0 && !error && !gamePathMissing}
    <div class="mt-8 text-center text-sm text-muted">
      {t(locale, 'home-no-routes')}
    </div>
  {:else}
    <div>
      <Table.Root
        class="table-fixed"
        containerClass="overflow-x-auto rounded-md border"
      >
        <Table.Header class="block w-full">
          {#each table.getHeaderGroups() as headerGroup (headerGroup.id)}
            <Table.Row class="table w-full table-fixed">
              {#each headerGroup.headers as header (header.id)}
                <DataTableHeader {header} />
              {/each}
            </Table.Row>
          {/each}
        </Table.Header>

        <Table.Body
          class="block max-h-[calc(100vh-156px)] overflow-y-auto [scrollbar-gutter:stable]"
        >
          {#each table.getRowModel().rows as row (row.id)}
            <Table.Row
              class={`table w-full table-fixed cursor-pointer ${openingRouteId !== null ? 'opacity-50' : ''}`}
              tabindex={openingRouteId === null ? 0 : -1}
              aria-disabled={openingRouteId !== null}
              onclick={() => openRoute(row.original)}
              onkeydown={(event) => openRouteFromKeyboard(event, row.original)}
            >
              {#each row.getVisibleCells() as cell (cell.id)}
                <Table.Cell class={getDataTableCellClass(cell)}>
                  {#if cell.column.id === 'image'}
                    <div
                      class="h-16 w-28 overflow-hidden rounded-[4px] bg-surface-raised"
                    >
                      {#if row.original.imageDataUrl}
                        <img
                          src={row.original.imageDataUrl}
                          alt=""
                          class="h-full w-full object-cover"
                          loading="lazy"
                        />
                      {:else}
                        <div
                          class="flex h-full w-full items-center justify-center text-xs text-muted"
                        >
                          —
                        </div>
                      {/if}
                    </div>
                  {:else if cell.column.id === 'packagingType'}
                    <Badge variant="outline">
                      {openingRouteId === row.original.id
                        ? t(locale, 'home-opening')
                        : row.original.packagingType}
                    </Badge>
                  {:else}
                    {row.original.name}
                  {/if}
                </Table.Cell>
              {/each}
            </Table.Row>
          {/each}
        </Table.Body>
      </Table.Root>
    </div>
  {/if}
</div>
