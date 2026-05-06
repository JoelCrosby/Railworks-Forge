import { sveltekit } from '@sveltejs/kit/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [tailwindcss(), sveltekit()],
	// Tauri: don't open a browser, use the webview
	server: {
		port: 5173,
		strictPort: true,
	},
	// Tauri expects a static output
	build: {
		target: 'esnext',
	},
	clearScreen: false,
});
