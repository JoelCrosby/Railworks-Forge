import { writable } from 'svelte/store';

export interface RefreshControl {
	onRefresh: (() => void | Promise<void>) | null;
	disabled?: boolean;
	loading?: boolean;
}

const defaultRefreshControl: RefreshControl = {
	onRefresh: null,
	disabled: true,
	loading: false
};

export const refreshControl = writable<RefreshControl>(defaultRefreshControl);

export function setRefreshControl(control: RefreshControl) {
	refreshControl.set(control);
}

export function clearRefreshControl() {
	refreshControl.set(defaultRefreshControl);
}
