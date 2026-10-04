//The instance API the views and plugins reach through GrandAdmin.grids.get(el): 177 calls
//across the panels, mostly dataSource.read() and dataSource.page(1) (admin.common.js
//tabstrip_on_tab_show among them), plus dataItem, select, sync, remove and get.
//Only that subset exists; everything else is intentionally absent so a missing method
//fails loudly instead of silently doing nothing.

function wrap(elements) {
    const $ = window.jQuery
    return $ ? $(elements) : elements
}

/**
 * @param {import('./grid.js').GrandGrid} grid
 */
export function createKendoApi(grid) {
    const ds = grid.dataSource
    const dataSource = {
        read: () => ds.read(),
        page: value => ds.page(value),
        pageSize: value => ds.pageSize(value),
        totalPages: () => ds.totalPages(),
        total: () => ds.total(),
        data: () => ds.data(),
        view: () => ds.view(),
        at: index => ds.at(index),
        get: id => ds.get(id),
        indexOf: item => ds.indexOf(item),
        remove: item => ds.remove(item),
        sync: () => ds.sync(),
        cancelChanges: () => {
            if (grid.editMode === 'Batch') return grid.cancelChanges()
            grid.cancelEdit()
            ds.cancelChanges()
        },
        hasChanges: () => (grid.editMode === 'Batch' ? grid.hasChanges() : ds.hasChanges())
    }

    const api = {
        grandGrid: grid,
        dataSource,
        get element() {
            return wrap(grid.element)
        },
        get table() {
            return grid.table
        },
        get selectedIds() {
            return grid.selectedIds
        },
        dataItem: row => grid.dataItem(row),
        /** The selected row (selectable="Row"), or the rows ticked in the checkbox column on the current page. */
        select: () => {
            if (grid.selectableRow) {
                const row = grid.selectedRowElement()
                return wrap(row ? [row] : [])
            }
            const rows = Array.from(grid.tableElement.querySelectorAll('input.grand-grid-select:checked'))
                .map(input => input.closest('.tabulator-row'))
                .filter(Boolean)
            return wrap(rows)
        },
        clearSelection: () => grid.clearSelection(),
        refresh: () => grid.refresh(),
        //kendo.resize(container) - called by the Kendo TabStrip when a tab is shown - finds
        //every [data-role] element and calls resize() on the widget stored in its data
        resize: () => grid.resize(),
        addRow: () => grid.addRow(),
        editRow: row => grid.editRow(row),
        saveRow: () => grid.saveRow(),
        cancelRow: () => grid.cancelEdit(),
        saveChanges: () => grid.saveChanges(),
        removeRow: row => grid.destroyRow(row),
        cancelChanges: () => dataSource.cancelChanges(),
        destroy: () => grid.destroy()
    }
    return api
}
