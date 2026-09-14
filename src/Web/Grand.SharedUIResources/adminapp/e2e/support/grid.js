import { expect } from '@playwright/test'

/*
 * Helpers for the Kendo grids as they are rendered today. They read the widget
 * through $(el).data('kendoGrid') - the same API the views and plugins use - so the
 * specs keep working unchanged once GrandGrid provides that API.
 */

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
    await expect(grid.locator('.k-loading-mask')).toHaveCount(0)
    const state = await page.evaluate(id => {
        const widget = $('#' + id).data('kendoGrid')
        if (!widget) return null
        return {
            rows: $('#' + id).find('tbody > tr').not('.k-detail-row, .k-grouping-row').length,
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
    const edit = grid.locator('tbody .k-grid-edit').first()
    if ((await edit.count()) === 0) return false
    const requests = []
    const onRequest = request => { if (request.method() !== 'GET') requests.push(request.url()) }
    page.on('request', onRequest)
    await edit.click()
    await expect(grid.locator('tr.k-grid-edit-row')).toHaveCount(1)
    await grid.locator('tr.k-grid-edit-row .k-grid-cancel').click()
    await expect(grid.locator('tr.k-grid-edit-row')).toHaveCount(0)
    page.off('request', onRequest)
    expect(requests, 'cancelling inline edit must not call the server').toEqual([])
    return true
}

/** Expands the first master row of a grid with detailInit and waits for the detail grid. */
export async function expandFirstDetail(page, gridId) {
    const grid = page.locator(`#${gridId}`)
    const expander = grid.locator('tbody > tr.k-master-row .k-hierarchy-cell a').first()
    if ((await expander.count()) === 0) return false
    await expander.click()
    const detail = grid.locator('tr.k-detail-row').first()
    await expect(detail).toBeVisible()
    await page.waitForLoadState('networkidle')
    await expect(detail.locator('[data-role="grid"]').first()).toBeVisible()
    return true
}

/** Selects a tab of an <admin-tabstrip> by index (language independent). */
export async function openTab(page, tabStripName, index) {
    const tab = page.locator(`#${tabStripName} > ul.k-tabstrip-items > li`).nth(index)
    await tab.click()
    await expect(tab).toHaveClass(/k-state-active/)
    await page.waitForLoadState('networkidle')
    const content = page.locator(`#${tabStripName} > .k-content.k-state-active`)
    await expect(content).toBeVisible()
    return content
}

/** Waits for every grid inside a container (e.g. an opened tab). */
export async function waitForGridsIn(page, container) {
    const ids = await container.locator('[data-role="grid"][id]').evaluateAll(els => els.map(e => e.id))
    const states = {}
    for (const id of ids) states[id] = await waitForGrid(page, id)
    return states
}
