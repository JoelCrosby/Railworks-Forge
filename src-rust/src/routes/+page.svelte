<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { Channel } from '@tauri-apps/api/core';
  import { goto } from '$app/navigation';
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

  // Game path state
  let gamePathMissing = $state(false);
  let locale = $derived($settings.locale);

  const PATH_MISSING_HINT = 'could not locate railworks';

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
    <ul class="flex list-none flex-col gap-2">
      {#each routes as route (route.id)}
        <li>
          <button
            class="flex w-full cursor-pointer items-center gap-3 rounded-lg border border-surface-raised bg-surface px-4 py-3.5 text-left text-text transition-colors hover:border-accent disabled:cursor-not-allowed disabled:opacity-50"
            onclick={() => openRoute(route)}
            disabled={openingRouteId !== null}
          >
            <span class="flex-1 text-[0.95rem] font-medium">{route.name}</span>
            {#if route.description}
              <span class="flex-2 truncate text-[0.8rem] text-muted"
                >{route.description}</span
              >
            {/if}
            <span
              class={`shrink-0 rounded px-2 py-0.5 text-[0.7rem] tracking-wider uppercase ${route.packagingType === 'packed' ? 'bg-accent-surface text-accent-text' : 'bg-success-surface text-ok'}`}
            >
              {openingRouteId === route.id
                ? t(locale, 'home-opening')
                : route.packagingType}
            </span>
          </button>
        </li>
      {/each}
    </ul>
  {/if}
</div>
