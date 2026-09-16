import { test, expect } from '../support/fixtures.js'
import { firstItemField, openPage, openTab, waitForGrid } from '../support/grid.js'

/*
 * The widgets that replaced Kendo UI in phase 4: the tab strip, GrandAdmin.modal, the
 * numeric text box, the date and time inputs and the Tom Select lists. Read-only, like
 * the rest of the smoke set: modals are opened and closed, nothing is submitted.
 */
test.use({ panel: 'admin' })

async function firstId(page, listPath, gridId) {
    await openPage(page, listPath)
    await waitForGrid(page, gridId)
    const id = await firstItemField(page, gridId)
    test.skip(!id, `${listPath} has no items`)
    return id
}

test.describe('Admin widgets', () => {
    test('tab strip records the selected tab and shows one pane at a time', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Product/List', 'products-grid')
        await openPage(page, `/Admin/Product/Edit/${id}`)

        const panes = page.locator('#product-edit > .tab-content > .tab-pane')
        await expect(page.locator('#product-edit')).toBeVisible()
        await expect(panes.filter({ has: page.locator(':scope.active') })).toHaveCount(1)
        //the page renders a hidden input per strip and they share the id, as they always
        //did; tabstrip_on_tab_select writes the first one, which is the one that is posted
        await expect(page.locator('#selected-tab-index').first()).toHaveValue('0')

        await openTab(page, 'product-edit', 3)
        await expect(page.locator('#selected-tab-index').first()).toHaveValue('3')
        await expect(panes.nth(3)).toBeVisible()
        await expect(panes.nth(0)).toBeHidden()
        //the pane must not collapse around the floated panel layout inside it
        expect((await panes.nth(3).boundingBox()).height).toBeGreaterThan(0)
    })

    test('localized editor renders its own tab strip', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Category/List', 'categories-grid')
        await openPage(page, `/Admin/Category/Edit/${id}`)
        const strips = page.locator('.grand-tabstrip')
        expect(await strips.count()).toBeGreaterThan(0)
    })

    test('delete confirmation opens and closes without posting', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Product/List', 'products-grid')
        await openPage(page, `/Admin/Product/Edit/${id}`)

        const posts = []
        page.on('request', r => { if (r.method() === 'POST') posts.push(r.url()) })
        await page.locator('#product-delete').click()
        const dialog = page.locator('.grand-modal:not([hidden])')
        await expect(dialog).toBeVisible()
        await expect(dialog.locator('form')).toBeVisible()
        await dialog.locator('.grand-modal-close').click()
        await expect(page.locator('.grand-modal:not([hidden])')).toHaveCount(0)
        expect(posts, 'closing the confirmation must not post').toEqual([])
    })

    test('copy product popup opens in a modal', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Product/List', 'products-grid')
        await openPage(page, `/Admin/Product/Edit/${id}`)
        await page.locator('#copyproduct').click()
        const dialog = page.locator('.grand-modal:not([hidden])')
        await expect(dialog).toBeVisible()
        await expect(dialog.locator('#copyproduct-window')).toBeVisible()
        await page.keyboard.press('Escape')
        await expect(page.locator('.grand-modal:not([hidden])')).toHaveCount(0)
    })

    test('import from Excel popup opens in a modal', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Category/List')
        await page.locator('#importexcel').click()
        await expect(page.locator('.grand-modal:not([hidden]) #importexcel-window')).toBeVisible()
        await page.keyboard.press('Escape')
        await expect(page.locator('.grand-modal:not([hidden])')).toHaveCount(0)
    })

    test('numeric editors post the culture format and show the formatted one', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Product/List', 'products-grid')
        await openPage(page, `/Admin/Product/Edit/${id}`)
        await openTab(page, 'product-edit', 1) //prices

        const state = await page.evaluate(() => {
            const element = document.querySelector('input[data-grand-numeric]')
            if (!element) return null
            const decimal = window.GrandAdmin.culture().numberFormat.decimal
            return {
                hidden: element.style.display === 'none',
                named: !!element.name,
                posted: element.value,
                shown: element.grandNumeric.text.value,
                widgetInputHasNoName: element.grandNumeric.text.name === '',
                decimal
            }
        })
        expect(state, 'a numeric editor is on the page').not.toBeNull()
        expect(state.hidden).toBe(true)
        expect(state.named).toBe(true)
        expect(state.widgetInputHasNoName).toBe(true)
        //no group separator in what is posted, and only the culture decimal separator
        expect(state.posted).toMatch(/^-?\d*(\D\d+)?$/)
        if (state.posted.match(/\D/)) expect(state.posted).toContain(state.decimal)
    })

    test('typing in a numeric editor writes the posted value in the request culture', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Product/List', 'products-grid')
        await openPage(page, `/Admin/Product/Edit/${id}`)
        await openTab(page, 'product-edit', 1)
        const result = await page.evaluate(() => {
            //a decimal editor, not one of the Int32 quantity fields next to it
            const element = Array.from(document.querySelectorAll('input[data-grand-numeric]'))
                .find(input => input.grandNumeric?.decimals > 0)
            if (!element) return null
            const before = element.value
            element.grandNumeric.value(1234.567)
            const posted = element.value
            const shown = element.grandNumeric.text.value
            element.grandNumeric.value(before === '' ? null : before)
            return { posted, shown, restored: element.value, before }
        })
        expect(result, 'a decimal editor is on the page').not.toBeNull()
        //rounded to the editor's decimals, the culture separator, no grouping
        expect(result.posted).toMatch(/^1234\D57$/)
        //the visible input shows the same number with grouping
        expect(result.shown.replace(/\D/g, '')).toBe('123457')
        expect(result.restored).toBe(result.before)
    })

    test('date editors keep a native picker next to the posted value', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Discount/List', 'discounts-grid')
        await openPage(page, `/Admin/Discount/Edit/${id}`)
        const state = await page.evaluate(() => {
            const element = document.querySelector('input[data-grand-date]')
            if (!element) return null
            return {
                mode: element.getAttribute('data-grand-date'),
                hidden: element.style.display === 'none',
                pickerType: element.grandDateInput.picker.type,
                pickerHasNoName: element.grandDateInput.picker.name === ''
            }
        })
        expect(state, 'a date editor is on the page').not.toBeNull()
        expect(state.hidden).toBe(true)
        expect(state.pickerHasNoName).toBe(true)
        expect(['date', 'datetime-local', 'time']).toContain(state.pickerType)
    })

    test('store and customer group multiselects load their options', async ({ panelPage: page }) => {
        const id = await firstId(page, '/Admin/Product/List', 'products-grid')
        await openPage(page, `/Admin/Product/Edit/${id}`)
        //the mappings tab carries the store and customer group lists
        await openTab(page, 'product-edit', 2)
        await page.waitForLoadState('networkidle')
        const state = await page.evaluate(() => {
            const element = document.querySelector('select[data-grand-select]')
            if (!element) return null
            return {
                name: element.name,
                multiple: element.multiple,
                options: Object.keys(element.grandSelect.options).length,
                wrapper: !!element.parentElement.querySelector('.ts-wrapper')
            }
        })
        expect(state, 'a multiselect is on the page').not.toBeNull()
        expect(state.name).not.toBe('')
        expect(state.multiple).toBe(true)
        expect(state.wrapper).toBe(true)
        expect(state.options).toBeGreaterThan(0)
    })

    test('brand lookup loads its options and posts the chosen id', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Product/List')
        const state = await page.evaluate(async () => {
            const element = document.querySelector('input[data-grand-select]')
            if (!element) return null
            //the options arrive from Search; give the request a moment
            await new Promise(resolve => setTimeout(resolve, 1500))
            const widget = element.grandSelect
            const first = Object.keys(widget.options).find(value => value !== '')
            if (first) widget.setValue(first)
            return { type: element.type, name: element.name, value: element.value, first: first ?? '' }
        })
        test.skip(!state, 'no lookup on the product list')
        expect(state.type).toBe('hidden')
        expect(state.name).not.toBe('')
        if (state.first) expect(state.value).toBe(state.first)
    })
})
