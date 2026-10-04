import { test } from '../support/fixtures.js'
import { expandFirstDetail, openPage, waitForGrid } from '../support/grid.js'

//Vendor panel smoke set. Read-only.
test.use({ panel: 'vendor' })

test.describe('Vendor', () => {
    test('product list', async ({ panelPage: page }) => {
        await openPage(page, '/Vendor/Product/List')
        await waitForGrid(page, 'products-grid')
    })

    test('shipment list with detail rows', async ({ panelPage: page }) => {
        await openPage(page, '/Vendor/Shipment/List')
        await waitForGrid(page, 'shipments-grid')
        await expandFirstDetail(page, 'shipments-grid')
    })

    test('reports: bestsellers', async ({ panelPage: page }) => {
        await openPage(page, '/Vendor/Reports/BestsellersReport')
        await waitForGrid(page, 'salesreport-grid')
    })
})
