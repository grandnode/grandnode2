import { test } from '../support/fixtures.js'
import { openInlineEditAndCancel, openPage, waitForGrid } from '../support/grid.js'

//Store-manager panel smoke set. Read-only.
test.use({ panel: 'store' })

test.describe('Store', () => {
    test('product list', async ({ panelPage: page }) => {
        await openPage(page, '/Store/Product/List')
        await waitForGrid(page, 'products-grid')
    })

    test('order list', async ({ panelPage: page }) => {
        await openPage(page, '/Store/Order/List')
        await waitForGrid(page, 'orders-grid')
    })

    test('currency list', async ({ panelPage: page }) => {
        await openPage(page, '/Store/Currency/List')
        await waitForGrid(page, 'currencies-grid')
    })

    test('language list', async ({ panelPage: page }) => {
        await openPage(page, '/Store/Language/List')
        await waitForGrid(page, 'languages-grid')
    })

    test('tax categories inline edit', async ({ panelPage: page }) => {
        await openPage(page, '/Store/Tax/Categories')
        await waitForGrid(page, 'tax-categories-grid')
        await openInlineEditAndCancel(page, 'tax-categories-grid')
    })
})
