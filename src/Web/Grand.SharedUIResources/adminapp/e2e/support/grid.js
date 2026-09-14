import { expect } from '@playwright/test'

/*
 * Helpers that work for both grid engines: Kendo grids built in script and <admin-grid>
 * (admin.grid.js). They read the widget through $(el).data('kendoGrid') - the API both
 * provide - and use a selector for each engine where the markup differs.
 */

//markup of the two engines; each selector matches either
export const gridSelectors = {
    loading: '.k-loading-mask',
    editButton: 'tbody .k-grid-edit, .grand-grid-edit',
    editRow: 'tr.k-grid-edit-row, .grand-grid-edit-row',
    cancelButton: '.k-grid-cancel, .grand-grid-cancel',
    updateButton: '.k-grid-update, .grand-grid-update',
    detailExpander: 'tbody > tr.k-master-row .k-hierarchy-cell a, .grand-grid-detail-toggle',
    detailRow: 'tr.k-detail-row, .grand-grid-detail',
    checkedRowCheckbox: 'tbody input[type=checkbox]:checked, .grand-grid-table input.grand-grid-select:checked',
    selectAll: '#mastercheckbox, .grand-grid-select-all'
}

/** Opens a panel page and waits until its requests (including grid reads) settle. */
export async function openPage(page, path) {
    const response = await page.goto(path, { waitUntil: 'domcontentloaded' })
    expect(response?.status(), `GET ${path}`).toBeLessThan(400)
    await page.waitForLoadState('networkidle')
    return response
}

/** Waits until the grid finished loading and returns { rows, total }. */
export async function waitForGrid(page, gridId) {
    const grid = page.locator(`#${gridId}`)
    await expect(grid, `grid #${gridId} is rendered`).toBeVisible()
    await page.waitForLoadState('networkidle')
    await expect(grid.locator(gridSelectors.loading)).toHaveCount(0)
    const state = await page.evaluate(id => {
        const widget = $('#' + id).data('kendoGrid')
        if (!widget) return null
        const element = $('#' + id)
        const rows = widget.grandGrid
            ? element.children('.grand-grid-table').find('.tabulator-table').first().children('.tabulator-row').length
            : element.find('tbody > tr').not('.k-detail-row, .k-grouping-row').length
        return {
            rows,
            total: widget.dataSource.total(),
            dataLength: widget.dataSource.data().length
        }
    }, gridId)
    expect(state, `#${gridId} is a kendoGrid`).not.toBeNull()
    //either rows were rendered for the data that came back, or the grid is empty
    if (state.dataLength > 0) expect(state.rows).toBeGreaterThan(0)
    else expect(state.total).toBe(0)
    return state
}

/** Returns a field of the first data item, e.g. the Id to open an edit page. */
export async function firstItemField(page, gridId, field = 'Id') {
    return page.evaluate(([id, name]) => {
        const item = $('#' + id).data('kendoGrid').dataSource.data()[0]
        return item ? item[name] : undefined
    }, [gridId, field])
}

/** Opens inline edit on the first row and cancels it; nothing is sent to the server. */
export async function openInlineEditAndCancel(page, gridId) {
    const grid = page.locator(`#${gridId}`)
    const edit = grid.locator(gridSelectors.editButton).first()
    if ((await edit.count()) === 0) return false
    const requests = []
    const onRequest = request => { if (request.method() !== 'GET') requests.push(request.url()) }
    page.on('request', onRequest)
    await edit.click()
    await expect(grid.locator(gridSelectors.editRow)).toHaveCount(1)
    await grid.locator(gridSelectors.editRow).locator(gridSelectors.cancelButton).click()
    await expect(grid.locator(gridSelectors.editRow)).toHaveCount(0)
    page.off('request', onRequest)
    expect(requests, 'cancelling inline edit must not call the server').toEqual([])
    return true
}

/** Expands the first master row of a grid with a detail grid and waits for it. */
export async function expandFirstDetail(page, gridId) {
    const grid = page.locator(`#${gridId}`)
    const expander = grid.locator(gridSelectors.detailExpander).first()
    if ((await expander.count()) === 0) return false
    await expander.click()
    const detail = grid.locator(gridSelectors.detailRow).first()
    await expect(detail).toBeVisible()
    await page.waitForLoadState('networkidle')
    await expect(detail.locator('[data-role="grid"]').first()).toBeVisible()
    return true
}

/** Selects a tab of an <admin-tabstrip> by index (language independent). */
export async function openTab(page, tabStripName, index) {
    //Kendo 2021 wraps the items in .k-tabstrip-items-wrapper
    const tab = page.locator(`#${tabStripName} > .k-tabstrip-items-wrapper > ul.k-tabstrip-items > li, #${tabStripName} > ul.k-tabstrip-items > li`).nth(index)
    //the Kendo TabStrip ignores a click while the previous tab is still fading in
    await expect(async () => {
        await tab.click()
        await expect(tab).toHaveClass(/k-state-active/, { timeout: 1_000 })
    }).toPass({ timeout: 15_000 })
    await page.waitForLoadState('networkidle')
    const content = page.locator(`#${tabStripName} > .k-content.k-state-active`)
    await expect(content).toBeVisible()
    return content
}

/**
 * Waits for every visible grid inside a container (e.g. an opened tab). Grids in nested
 * tabs that are not shown, or in sections the product type hides, are skipped.
 */
export async function waitForGridsIn(page, container) {
    const ids = await container.locator('[data-role="grid"][id]').evaluateAll(els => els.filter(e => e.offsetParent !== null).map(e => e.id))
    const states = {}
    for (const id of ids) states[id] = await waitForGrid(page, id)
    return states
}
