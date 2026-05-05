import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
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
