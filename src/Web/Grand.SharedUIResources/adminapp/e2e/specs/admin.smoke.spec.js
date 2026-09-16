import { test, expect } from '../support/fixtures.js'
import { expandFirstDetail, firstItemField, gridSelectors, openInlineEditAndCancel, openPage, openTab, waitForGrid, waitForGridsIn } from '../support/grid.js'

//Admin panel smoke set. Read-only: pages are opened, grids loaded, inline edit is
//opened and cancelled, tabs are switched - nothing is saved, deleted or uploaded.
test.use({ panel: 'admin' })

/** Opens an edit page for the first item of a list grid, or skips when the list is empty. */
async function openFirstFromList(page, listPath, gridId, editPath) {
    await openPage(page, listPath)
    await waitForGrid(page, gridId)
    const id = await firstItemField(page, gridId)
    test.skip(!id, `${listPath} has no items`)
    await openPage(page, `${editPath}/${id}`)
}

test.describe('Admin', () => {
    test('dashboard renders charts', async ({ panelPage: page }) => {
        await openPage(page, '/Admin')
        await expect(page.locator('canvas').first()).toBeVisible()
    })

    test('product list: search and checkbox selection', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Product/List')
        const { dataLength } = await waitForGrid(page, 'products-grid')

        await page.locator('#search-products').click()
        await waitForGrid(page, 'products-grid')

        test.skip(dataLength === 0, 'no products to select')
        //Kendo view: #mastercheckbox; <admin-grid>: the checkbox column header
        const grid = page.locator('#products-grid')
        const master = grid.locator(gridSelectors.selectAll).first()
        const checked = grid.locator(gridSelectors.checkedRowCheckbox)
        await master.check()
        await expect(checked.first()).toBeVisible()
        await master.uncheck()
        await expect(checked).toHaveCount(0)
    })

    test('product edit: categories, prices, pictures, attributes, specification attributes', async ({ panelPage: page }) => {
        await openFirstFromList(page, '/Admin/Product/List', 'products-grid', '/Admin/Product/Edit')

        //index order follows Product/Partials/CreateOrUpdate.cshtml
        await waitForGridsIn(page, await openTab(page, 'product-edit', 1)) //prices
        await waitForGridsIn(page, await openTab(page, 'product-edit', 4)) //pictures
        const mappings = await openTab(page, 'product-edit', 5)
        await waitForGridsIn(page, mappings)
        await openInlineEditAndCancel(page, 'productcategories-grid')
        await waitForGridsIn(page, await openTab(page, 'product-edit', 7)) //specification attributes
        await waitForGridsIn(page, await openTab(page, 'product-edit', 8)) //product attributes
    })

    test('order list and order details', async ({ panelPage: page }) => {
        await openFirstFromList(page, '/Admin/Order/List', 'orders-grid', '/Admin/Order/Edit')
        await waitForGridsIn(page, await openTab(page, 'order-edit', 2)) //shipments
        await waitForGridsIn(page, await openTab(page, 'order-edit', 3)) //products
        await waitForGridsIn(page, await openTab(page, 'order-edit', 4)) //order notes
    })

    test('customer edit: orders tab', async ({ panelPage: page }) => {
        await openFirstFromList(page, '/Admin/Customer/List', 'customers-grid', '/Admin/Customer/Edit')
        const orders = await openTab(page, 'customer-edit', 1)
        await waitForGridsIn(page, orders)
    })

    test('current shopping carts with detail rows', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/ShoppingCart/CurrentCarts')
        await waitForGrid(page, 'carts-grid')
        await expandFirstDetail(page, 'carts-grid')
    })

    test('measures: weights inline edit', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Measure/Index')
        await openTab(page, 'measures-list', 2)
        await waitForGrid(page, 'measureweight-grid')
        await openInlineEditAndCancel(page, 'measureweight-grid')
    })

    test('language resources', async ({ panelPage: page }) => {
        await openFirstFromList(page, '/Admin/Language/List', 'languages-grid', '/Admin/Language/Edit')
        await openTab(page, 'language-edit', 1)
        await waitForGrid(page, 'resources-grid')
        await openInlineEditAndCancel(page, 'resources-grid')
    })

    test('country states', async ({ panelPage: page }) => {
        await openFirstFromList(page, '/Admin/Country/List', 'countries-grid', '/Admin/Country/Edit')
        await openTab(page, 'country-edit', 1)
        await waitForGrid(page, 'states-grid')
    })

    test('general settings with store scope selector', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Setting/GeneralCommon')
        await expect(page.locator('#generalsettings-edit')).toBeVisible()
        await openTab(page, 'generalsettings-edit', 1)
    })

    test('message template editor', async ({ panelPage: page }) => {
        await openFirstFromList(page, '/Admin/MessageTemplate/List', 'templates-grid', '/Admin/MessageTemplate/Edit')
        await expect(page.locator('.CodeMirror').first()).toBeAttached()
    })

    test('reports: bestsellers', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Reports/BestsellersReport')
        await waitForGrid(page, 'salesreport-grid')
    })

    test('discount requirements (DiscountRules.Standard)', async ({ panelPage: page }) => {
        await openFirstFromList(page, '/Admin/Discount/List', 'discounts-grid', '/Admin/Discount/Edit')
        const requirements = await openTab(page, 'discount-edit', 2)
        await expect(requirements).toBeVisible()
    })

    //Every plugin that ships an admin configuration grid; each is skipped when the plugin
    //is not installed in the instance the specs run against.
    const pluginPages = [
        { plugin: 'Shipping.ByWeight', path: '/Admin/ShippingByWeight/Configure', grid: 'shipping-byweight-grid' },
        { plugin: 'Shipping.FixedRate', path: '/Admin/ShippingFixedRate/Configure', grid: 'shipping-rate-grid', inlineEdit: true },
        { plugin: 'Shipping.ShippingPoint', path: '/Admin/ShippingPoint/Configure', grid: 'shipping-points-grid' },
        { plugin: 'Tax.CountryStateZip', path: '/Admin/TaxCountryStateZip/Configure', grid: 'tax-countrystatezip-grid' },
        { plugin: 'Tax.FixedRate', path: '/Admin/TaxFixedRate/Configure', grid: 'tax-categories-grid', inlineEdit: true },
        { plugin: 'Widgets.Slider', path: '/Admin/WidgetsSlider/Configure', grid: 'slider-grid' }
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
