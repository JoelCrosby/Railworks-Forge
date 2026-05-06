import { writable } from 'svelte/store';

export interface BreadcrumbItem {
	label: string;
	href?: string;
}

export const breadcrumbItems = writable<BreadcrumbItem[]>([
	{ label: 'Routes', href: '/' }
]);

export function setBreadcrumbs(items: BreadcrumbItem[]) {
	breadcrumbItems.set(items);
}
