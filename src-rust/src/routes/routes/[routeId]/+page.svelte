<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { t } from '$lib/i18n';
  import { settings } from '$lib/settings';
  import { setBreadcrumbs } from '$lib/stores/breadcrumb';

  interface Route {
    id: string;
    name: string;
    description: string | null;
    directoryPath: string;
    packagingType: 'packed' | 'unpacked';
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
    scenarioClass: 'passenger' | 'freight' | 'shunting' | 'mixed' | 'empty';
    packagingType: 'packed' | 'unpacked';
    directoryPath: string;
    routeId: string;
    playerInfo: {
      scenarioId: string;
      score: number;
      completion: string;
      medalsAwarded: number;
    };
    consists: unknown[];
  }

  // Route is passed via navigation state; fall back to fetching if missing.
  let route = $state<Route | null>(
    ($page.state as { route?: Route })?.route ?? null,
  );
  let routeId = $derived($page.params.routeId ?? '');
  let routeLoadAttemptedFor = $state<string | null>(null);
  let loadingRoute = $state(false);

  let scenarios = $state<Scenario[]>([]);
  let loading = $state(false);
  let error = $state<string | null>(null);
  let search = $state('');
  let locale = $derived($settings.locale);

  let filtered = $derived(
    search.trim()
      ? scenarios.filter((s) =>
          [s.name, s.locomotive, s.season]
            .join(' ')
            .toLowerCase()
            .includes(search.toLowerCase()),
        )
      : scenarios,
  );

  async function loadRoute() {
    if (!routeId || loadingRoute || routeLoadAttemptedFor === routeId) return;
    routeLoadAttemptedFor = routeId;
    loadingRoute = true;
    error = null;
    try {
      route = await invoke<Route | null>('get_route', { routeId });
      if (!route) {
        error = `Route ${routeId} was not found.`;
      }
    } catch (e) {
      error = String(e);
    } finally {
      loadingRoute = false;
    }
  }

  async function loadScenarios() {
    if (!route) return;
    loading = true;
    error = null;
    scenarios = [];
    try {
      scenarios = await invoke<Scenario[]>('get_scenarios', { route });
    } catch (e) {
      error = String(e);
    } finally {
      loading = false;
    }
  }

  function openScenario(scenario: Scenario) {
    goto(
      `/routes/${encodeURIComponent(routeId)}/scenarios/${encodeURIComponent(scenario.id)}`,
      {
        state: {
          route: route ? $state.snapshot(route) : null,
          scenario: $state.snapshot(scenario),
        },
      },
    );
  }

  function formatDuration(mins: number): string {
    if (mins <= 0) return '—';
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    return h > 0 ? `${h}h ${m}m` : `${m}m`;
  }

  function scenarioBadgeClass(
    scenarioClass: Scenario['scenarioClass'],
  ): string {
    const base =
      'shrink-0 rounded px-2 py-0.5 text-[0.65rem] tracking-wider uppercase';
    switch (scenarioClass) {
      case 'passenger':
        return `${base} bg-accent-surface text-accent-text`;
      case 'freight':
        return `${base} bg-[#3d2a14] text-warn`;
      case 'shunting':
        return `${base} bg-[#2d3a1a] text-success-text`;
      case 'mixed':
        return `${base} bg-[#3a2a4a] text-[#d6bcfa]`;
      default:
        return `${base} bg-surface-raised text-muted`;
    }
  }

  $effect(() => {
    if (route) {
      loadScenarios();
    } else {
      loadRoute();
    }
  });

  $effect(() => {
    setBreadcrumbs([
      { label: t(locale, 'nav-routes'), href: '/' },
      {
        label: route?.name ?? `Route ${routeId}`,
        href: routeId ? `/routes/${encodeURIComponent(routeId)}` : undefined,
      },
    ]);
  });
</script>

<div class="mx-auto max-w-275 p-6">
  {#if route}
    <header class="mb-6 flex items-start justify-between gap-4">
      <div>
        <h1 class="text-[1.3rem] font-bold">{route.name}</h1>
        {#if route.description}
          <p class="mt-1 text-[0.85rem] text-muted">{route.description}</p>
        {/if}
      </div>
      <div class="flex shrink-0 gap-2">
        <button
          class="shrink-0 cursor-pointer rounded-md border border-accent-border bg-accent-surface px-4 py-1.5 text-sm text-accent-text disabled:cursor-not-allowed disabled:opacity-50"
          onclick={() =>
            goto(`/routes/${encodeURIComponent(routeId)}/tracks`, {
              state: { route: route ? $state.snapshot(route) : null },
            })}
        >
          {t(locale, 'route-tracks')}
        </button>
        <button
          class="shrink-0 cursor-pointer rounded-md border border-border-strong bg-surface-raised px-4 py-1.5 text-sm text-text hover:bg-surface-hover disabled:cursor-not-allowed disabled:opacity-50"
          onclick={loadScenarios}
          disabled={loading}
        >
          {loading ? t(locale, 'action-loading') : t(locale, 'action-refresh')}
        </button>
      </div>
    </header>
  {:else}
    <header class="mb-6">
      <h1 class="text-[1.3rem] font-bold">Route {routeId}</h1>
    </header>
  {/if}

  {#if error}
    <div
      class="mb-6 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"
    >
      <strong>{t(locale, 'error-label')}:</strong>
      {error}
    </div>
  {/if}

  {#if !loading && scenarios.length > 0}
    <div class="mb-4 flex items-center gap-3">
      <input
        class="flex-1 rounded-md border border-surface-raised bg-surface px-3 py-2 text-sm text-text outline-none focus:border-accent"
        type="search"
        placeholder={t(locale, 'route-search-scenarios')}
        bind:value={search}
      />
      <span class="whitespace-nowrap text-[0.8rem] text-muted"
        >{filtered.length} / {scenarios.length}</span
      >
    </div>
  {/if}

  {#if loadingRoute}
    <div class="mt-8 text-center text-sm text-muted">
      {t(locale, 'route-opening')}
    </div>
  {:else if loading}
    <div class="mt-8 text-center text-sm text-muted">
      {t(locale, 'route-loading-scenarios')}
    </div>
  {:else if scenarios.length === 0 && !error}
    <div class="mt-8 text-center text-sm text-muted">
      {t(locale, 'route-no-scenarios')}
    </div>
  {:else}
    <ul class="flex list-none flex-col gap-1.5">
      {#each filtered as scenario (scenario.id)}
        <li>
          <button
            class="flex w-full cursor-pointer items-center gap-3 rounded-lg border border-surface-raised bg-surface px-4 py-3 text-left text-text transition-colors hover:border-accent"
            onclick={() => openScenario(scenario)}
          >
            <span class="flex-2 text-sm font-medium">{scenario.name}</span>
            <span
              class="flex flex-3 items-center gap-3 text-[0.8rem] text-muted"
            >
              <span class="flex-1 truncate">{scenario.locomotive || '—'}</span>
              <span class="whitespace-nowrap"
                >{formatDuration(scenario.duration)}</span
              >
              <span class="whitespace-nowrap">{scenario.season || '—'}</span>
              <span class={scenarioBadgeClass(scenario.scenarioClass)}
                >{scenario.scenarioClass}</span
              >
            </span>
            {#if scenario.playerInfo.completion}
              <span class="whitespace-nowrap text-xs text-ok"
                >{scenario.playerInfo.completion}</span
              >
            {/if}
          </button>
        </li>
      {/each}
    </ul>
  {/if}
</div>
