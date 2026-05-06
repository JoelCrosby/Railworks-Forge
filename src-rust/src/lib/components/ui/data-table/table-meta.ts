import type { Cell } from '@tanstack/table-core';
import { cn } from '$lib/utils.js';

export type DataTableColumnMeta = {
	headerClass?: string;
	headerAlign?: 'left' | 'center' | 'right';
	cellClass?: string;
	cellAlign?: 'left' | 'center' | 'right';
};

function alignClass(align: DataTableColumnMeta['headerAlign']): string {
	switch (align) {
		case 'center':
			return 'text-center';
		case 'right':
			return 'text-right';
		default:
			return '';
	}
}

export function getDataTableCellClass<TData, TValue>(
	cell: Cell<TData, TValue>,
	className?: string,
) {
	const meta = cell.column.columnDef.meta as DataTableColumnMeta | undefined;
	return cn(
		alignClass(meta?.cellAlign ?? meta?.headerAlign),
		meta?.cellClass,
		className,
	);
}
