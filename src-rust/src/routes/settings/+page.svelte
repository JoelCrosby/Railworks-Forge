<script lang="ts">
  import { invoke } from '@tauri-apps/api/core';
  import { goto } from '$app/navigation';
  import { Button } from '$lib/components/ui/button/index.js';
  import * as Card from '$lib/components/ui/card/index.js';
  import { Input } from '$lib/components/ui/input/index.js';
  import { Label } from '$lib/components/ui/label/index.js';
  import * as Select from '$lib/components/ui/select/index.js';
  import { t } from '$lib/i18n';
  import {
    applyTheme,
    loadSettings,
    saveSettings,
    settings,
    type AppSettings,
    type Theme,
  } from '$lib/settings';
  import { setBreadcrumbs } from '$lib/stores/breadcrumb';
  import { clearRefreshControl } from '$lib/stores/refresh';

  let form = $state<AppSettings>({
    gamePath: null,
    theme: 'dark',
    locale: 'en-US',
  });
  let loading = $state(true);
  let saving = $state(false);
  let clearing = $state(false);
  let error = $state<string | null>(null);
  let success = $state<string | null>(null);
  let locale = $derived($settings.locale);

  async function load() {
    loading = true;
    error = null;
    try {
      form = await loadSettings();
    } catch (e) {
      error = String(e);
    } finally {
      loading = false;
    }
  }

  async function save() {
    saving = true;
    error = null;
    success = null;
    try {
      form = await saveSettings({
        ...form,
        gamePath: form.gamePath?.trim() || null,
      });
      success = t(form.locale, 'settings-saved');
    } catch (e) {
      error = String(e);
    } finally {
      saving = false;
    }
  }

  async function clearCache() {
    clearing = true;
    error = null;
    success = null;
    try {
      await invoke('clear_xml_cache');
      success = t(locale, 'settings-cache-cleared');
    } catch (e) {
      error = String(e);
    } finally {
      clearing = false;
    }
  }

  function setTheme(theme: Theme) {
    form.theme = theme;
    applyTheme(theme);
  }

  function languageLabel(nextLocale: AppSettings['locale']): string {
    return nextLocale === 'de-DE'
      ? t(nextLocale, 'settings-language-german')
      : t(nextLocale, 'settings-language-english');
  }

  $effect(() => {
    load();
  });

  $effect(() => {
    setBreadcrumbs([{ label: t(locale, 'nav-settings'), href: '/settings' }]);
  });

  $effect(() => {
    clearRefreshControl();
  });
</script>

<div class="mx-auto max-w-190 p-6">
  <header class="mb-6">
    <h1 class="text-[1.35rem] font-bold">{t(locale, 'settings-title')}</h1>
  </header>

  {#if error}
    <div
      class="mb-4 rounded-md border border-danger-border bg-danger-surface px-4 py-3 text-sm text-danger-text"
    >
      <strong>{t(locale, 'error-label')}:</strong>
      {error}
    </div>
  {/if}
  {#if success}
    <div
      class="mb-4 rounded-md border border-success-border bg-success-surface px-4 py-3 text-sm text-success-text"
    >
      {success}
    </div>
  {/if}

  {#if loading}
    <div class="text-center text-muted">{t(locale, 'action-loading')}</div>
  {:else}
    <div class="flex flex-col gap-4">
      <Card.Root size="sm">
        <Card.Header>
          <Card.Title>{t(locale, 'settings-game-path')}</Card.Title>
          <Card.Description>
            {t(locale, 'settings-game-path-hint')}
          </Card.Description>
        </Card.Header>
        <Card.Content class="flex flex-col gap-2">
          <Label for="game-path">{t(locale, 'settings-game-path')}</Label>
          <Input
            id="game-path"
            value={form.gamePath ?? ''}
            oninput={(event) => (form.gamePath = event.currentTarget.value)}
            placeholder="/path/to/RailWorks"
          />
          {#if form.gamePath}
            <p class="text-[0.82rem] text-muted">
              {t(locale, 'settings-current-path', { path: form.gamePath })}
            </p>
          {/if}
        </Card.Content>
      </Card.Root>

      <Card.Root size="sm">
        <Card.Header>
          <Card.Title>{t(locale, 'settings-theme')}</Card.Title>
        </Card.Header>
        <Card.Content>
          <div class="grid grid-cols-3 gap-1.5">
            <Button
              variant={form.theme === 'dark' ? 'default' : 'outline'}
              onclick={() => setTheme('dark')}
            >
              {t(locale, 'settings-theme-dark')}
            </Button>
            <Button
              variant={form.theme === 'light' ? 'default' : 'outline'}
              onclick={() => setTheme('light')}
            >
              {t(locale, 'settings-theme-light')}
            </Button>
            <Button
              variant={form.theme === 'system' ? 'default' : 'outline'}
              onclick={() => setTheme('system')}
            >
              {t(locale, 'settings-theme-system')}
            </Button>
          </div>
        </Card.Content>
      </Card.Root>

      <Card.Root size="sm">
        <Card.Header>
          <Card.Title>{t(locale, 'settings-language')}</Card.Title>
        </Card.Header>
        <Card.Content class="flex flex-col gap-2">
          <Label for="language-select">{t(locale, 'settings-language')}</Label>
          <Select.Root type="single" bind:value={form.locale}>
            <Select.Trigger id="language-select" class="w-full">
              {languageLabel(form.locale)}
            </Select.Trigger>
            <Select.Content>
              <Select.Item value="en-US">
                {t(locale, 'settings-language-english')}
              </Select.Item>
              <Select.Item value="de-DE">
                {t(locale, 'settings-language-german')}
              </Select.Item>
            </Select.Content>
          </Select.Root>
        </Card.Content>
      </Card.Root>

      <Card.Root size="sm">
        <Card.Header>
          <Card.Title>{t(locale, 'settings-cache')}</Card.Title>
        </Card.Header>
        <Card.Content>
          <Button variant="outline" onclick={clearCache} disabled={clearing}>
            {clearing
              ? t(locale, 'action-loading')
              : t(locale, 'settings-clear-cache')}
          </Button>
        </Card.Content>
      </Card.Root>
    </div>

    <footer class="flex justify-end gap-2 mt-4">
      <Button onclick={save} disabled={saving}>
        {saving ? t(locale, 'action-saving') : t(locale, 'action-save')}
      </Button>
      <Button variant="outline" onclick={() => goto('/')}>
        {t(locale, 'action-cancel')}
      </Button>
    </footer>
  {/if}
</div>
