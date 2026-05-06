<script lang="ts">
	import { listen } from '@tauri-apps/api/event';
	import favicon from '$lib/assets/favicon.svg';

	let { children } = $props();

	type DbStatus =
		| { status: 'loading' }
		| { status: 'ready' }
		| { status: 'failed'; message: string };

	let dbStatus = $state<DbStatus | null>(null);

	$effect(() => {
		const unlisten = listen<DbStatus>('scenario-db-status', (event) => {
			dbStatus = event.payload;
			if (event.payload.status === 'ready') {
				setTimeout(() => {
					dbStatus = null;
				}, 2000);
			}
		});
		return () => {
			unlisten.then((fn) => fn());
		};
	});
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

{@render children()}

{#if dbStatus !== null && dbStatus.status !== 'ready'}
	<div class="db-status {dbStatus.status}">
		{#if dbStatus.status === 'loading'}
			<span class="dot"></span> Loading player data…
		{:else}
			<span class="dot"></span> Player data unavailable: {dbStatus.message}
			<button onclick={() => (dbStatus = null)}>×</button>
		{/if}
	</div>
{/if}

<style>
	.db-status {
		position: fixed;
		bottom: 1rem;
		right: 1rem;
		background: #1a202c;
		border: 1px solid #2d3748;
		border-radius: 6px;
		padding: 0.4rem 0.75rem;
		font-size: 0.78rem;
		color: #a0aec0;
		display: flex;
		align-items: center;
		gap: 0.5rem;
		z-index: 100;
		max-width: 28rem;
	}

	.db-status.failed {
		border-color: #742a2a;
		color: #fc8181;
	}

	.dot {
		width: 6px;
		height: 6px;
		border-radius: 50%;
		background: #4a90d9;
		flex-shrink: 0;
		animation: pulse 1.2s ease-in-out infinite;
	}

	.failed .dot {
		background: #fc8181;
		animation: none;
	}

	@keyframes pulse {
		0%,
		100% {
			opacity: 1;
		}
		50% {
			opacity: 0.3;
		}
	}

	button {
		background: none;
		border: none;
		color: inherit;
		cursor: pointer;
		padding: 0 0.1rem;
		font-size: 1rem;
		line-height: 1;
		opacity: 0.6;
		margin-left: auto;
	}

	button:hover {
		opacity: 1;
	}
</style>
