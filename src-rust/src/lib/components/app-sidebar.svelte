<script lang="ts">
  import * as Sidebar from '$lib/components/ui/sidebar/index.js';
  import DashboardIcon from '@tabler/icons-svelte/icons/dashboard';
  import InnerShadowTopIcon from '@tabler/icons-svelte/icons/inner-shadow-top';
  import SearchIcon from '@tabler/icons-svelte/icons/search';
  import SettingsIcon from '@tabler/icons-svelte/icons/settings';
  import type { ComponentProps } from 'svelte';
  import NavMain from './nav-main.svelte';
  import NavSecondary from './nav-secondary.svelte';

  const data = {
    navMain: [
      {
        title: 'Routes',
        url: '/',
        icon: DashboardIcon,
      },
    ],
    navSecondary: [
      {
        title: 'Settings',
        url: '/settings',
        icon: SettingsIcon,
      },
      {
        title: 'Search',
        url: '#',
        icon: SearchIcon,
      },
    ],
  };

  let { ...restProps }: ComponentProps<typeof Sidebar.Root> = $props();
</script>

<Sidebar.Root collapsible="offcanvas" {...restProps}>
  <Sidebar.Header>
    <Sidebar.Menu>
      <Sidebar.MenuItem>
        <Sidebar.MenuButton class="data-[slot=sidebar-menu-button]:p-1.5!">
          {#snippet child({ props })}
            <a href="##" {...props}>
              <InnerShadowTopIcon class="size-5!" />
              <span class="text-base font-semibold">Railworks Forge</span>
            </a>
          {/snippet}
        </Sidebar.MenuButton>
      </Sidebar.MenuItem>
    </Sidebar.Menu>
  </Sidebar.Header>
  <Sidebar.Content>
    <NavMain items={data.navMain} />
    <NavSecondary items={data.navSecondary} class="mt-auto" />
  </Sidebar.Content>
  <Sidebar.Footer></Sidebar.Footer>
</Sidebar.Root>
