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

    //Every plugin that ships a store-owner configuration grid; each is skipped when the
    //plugin is not installed in the instance the specs run against.
    const pluginPages = [
        { plugin: 'Shipping.ByWeight', path: '/Store/ShippingByWeight/Configure', grid: 'shipping-byweight-grid' },
        { plugin: 'Shipping.ShippingPoint', path: '/Store/ShippingPoint/Configure', grid: 'shipping-points-grid' },
        { plugin: 'Tax.CountryStateZip', path: '/Store/TaxCountryStateZip/Configure', grid: 'tax-countrystatezip-grid' },
        { plugin: 'Tax.FixedRate', path: '/Store/TaxFixedRate/Configure', grid: 'tax-categories-grid', inlineEdit: true }
    ]

    for (const { plugin, path, grid, inlineEdit } of pluginPages) {
        test(`${plugin} configuration`, async ({ panelPage: page }) => {
            const response = await page.goto(path)
            test.skip(response?.status() === 404, `${plugin} is not installed`)
            await page.waitForLoadState('networkidle')
            const state = await waitForGrid(page, grid)
            if (inlineEdit && state.rows > 0) await openInlineEditAndCancel(page, grid)
        })
    }
})
