# Tailwind Migration Plan

## Summary

Migrate the SvelteKit app in `src` from component-scoped CSS blocks to Tailwind CSS utilities. Use Tailwind v4's current SvelteKit/Vite setup: install `tailwindcss` and `@tailwindcss/vite`, add the Vite plugin, and import Tailwind from `src/app.css`.

Reference: official Tailwind SvelteKit guide, `https://tailwindcss.com/docs/guides/sveltekit`.

## Key Changes

- Add dev dependencies with `pnpm add -D tailwindcss @tailwindcss/vite`.
- Update `vite.config.ts` to import `@tailwindcss/vite` and set plugins to `plugins: [tailwindcss(), sveltekit()]`.
- Replace `src/app.css` with:
  - `@import "tailwindcss";`
  - existing dark/light CSS theme variables under `:root` and `:root[data-theme='light']`
  - minimal base rules for `body`, `button`, `input`, and `select`
- Keep `src/routes/+layout.svelte` importing `../app.css`.

## Component Migration

- Convert all component `<style>` blocks in these eight files to Tailwind utility classes:
  - `src/routes/+layout.svelte`
  - `src/routes/+page.svelte`
  - `src/routes/settings/+page.svelte`
  - `src/routes/assets/+page.svelte`
  - `src/routes/routes/[routeId]/+page.svelte`
  - `src/routes/routes/[routeId]/tracks/+page.svelte`
  - `src/routes/routes/[routeId]/scenarios/[scenarioId]/+page.svelte`
  - `src/routes/routes/[routeId]/scenarios/[scenarioId]/consists/[consistId]/+page.svelte`
- Prefer utility classes directly in markup, using arbitrary values for existing theme tokens, for example `bg-[var(--surface)]`, `text-[var(--muted)]`, and `border-[var(--border)]`.
- Preserve conditional visual states with Svelte class expressions or ternaries, for example success/error banners, acquisition states, route packaging badges, scenario type badges, and dialog visibility.
- Use Tailwind responsive/layout utilities for existing grids, tables, dialogs, toolbars, cards, badges, fixed status toast, hover states, disabled states, truncation, spacing, borders, and typography.
- Remove each component `<style>` block after its markup has equivalent Tailwind classes.

## Test Plan

- Run `pnpm check`.
- Run `pnpm build`.
- Start `pnpm dev` and manually verify:
  - home route list
  - settings page and theme switching
  - assets page search/filter
  - route scenario list
  - scenario detail page
  - consist detail page
  - tracks replacement dialog
  - database loading/error toast
- Verify both dark and light themes still use the existing CSS variable values.

## Assumptions

- The migration should be strict utility-first Tailwind, not a shared `@apply` component layer.
- Existing app layout and visual design should be preserved rather than redesigned.
- `tailwind-migration.md` should be created at the repo root with this plan content when file edits are allowed.
