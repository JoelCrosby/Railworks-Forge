<script lang="ts">
  import RefreshCwIcon from '@lucide/svelte/icons/refresh-cw';
  import { Separator } from '$lib/components/ui/separator/index.js';
  import * as Sidebar from '$lib/components/ui/sidebar/index.js';
  import { Button } from '$lib/components/ui/button/index.js';
  import { t } from '$lib/i18n';
  import { settings } from '$lib/settings';
  import { refreshControl } from '$lib/stores/refresh';
  import Breadcrumb from './breadcrumb.svelte';

  let locale = $derived($settings.locale);
  let refreshing = $state(false);

  let canRefresh = $derived(
    Boolean($refreshControl.onRefresh) &&
      !$refreshControl.disabled &&
      !$refreshControl.loading &&
      !refreshing,
  );

  async function refresh() {
    const onRefresh = $refreshControl.onRefresh;
    if (!onRefresh || !canRefresh) return;

    refreshing = true;
    try {
      await onRefresh();
    } finally {
      refreshing = false;
    }
  }
</script>

<header
  class="flex h-(--header-height) shrink-0 items-center gap-2 border-b transition-[width,height] ease-linear group-has-data-[collapsible=icon]/sidebar-wrapper:h-(--header-height) sticky top-0 bg-background rounded-t-xl"
>
  <div class="flex w-full items-center gap-1 px-4 lg:gap-2 lg:px-6">
    <Sidebar.Trigger class="-ms-1" />
    <Separator
      orientation="vertical"
      class="mx-2 data-[orientation=vertical]:h-4"
    />
    <Breadcrumb />
    {#if $refreshControl.onRefresh}
      <Button
        class="ml-auto"
        variant="outline"
        size="sm"
        onclick={refresh}
        disabled={!canRefresh}
        title={t(locale, 'action-refresh')}
      >
        <RefreshCwIcon
          class={`size-4 ${$refreshControl.loading || refreshing ? 'animate-spin' : ''}`}
        />
        <span class="hidden sm:inline"
          >{$refreshControl.loading || refreshing
            ? t(locale, 'action-loading')
            : t(locale, 'action-refresh')}</span
        >
      </Button>
    {/if}
  </div>
</header>
