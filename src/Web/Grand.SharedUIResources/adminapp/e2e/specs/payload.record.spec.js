import fs from 'node:fs'
import path from 'node:path'
import { test } from '../support/fixtures.js'
import { openPage, waitForGrid, firstItemField } from '../support/grid.js'

/*
 * Records what the admin forms would post, so a change to the editor templates can be
 * compared against the engine it replaced. Nothing is submitted: the form data is read in
 * the browser and written to e2e/payloads/<name>.json (git-ignored, like e2e/har).
 *
 *   GRAND_PAYLOAD_DIR=e2e/payloads/before npx playwright test ... payload.record.spec.js
 */
test.use({ panel: 'admin' })

const outDir = process.env.GRAND_PAYLOAD_DIR || 'e2e/payloads/current'

function write(name, data) {
    fs.mkdirSync(outDir, { recursive: true })
    fs.writeFileSync(path.join(outDir, `${name}.json`), JSON.stringify(data, null, 1))
}

async function record(page, name, formSelector) {
    const data = await page.evaluate(selector => {
        const form = document.querySelector(selector)
        const out = {}
        for (const [key, value] of new FormData(form)) {
            if (key in out) out[key] = [].concat(out[key], value)
            else out[key] = value
        }
        return out
    }, formSelector)
    write(name, data)
    return data
}

test.describe('payloads', () => {
    test('product edit form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Product/List')
        await waitForGrid(page, 'products-grid')
        const id = await firstItemField(page, 'products-grid')
        await openPage(page, `/Admin/Product/Edit/${id}`)
        await record(page, 'product-edit', '#product-form')
    })

    test('catalog settings form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Setting/Catalog')
        await record(page, 'setting-catalog', 'form')
    })

    test('customer settings form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Setting/Customer')
        await record(page, 'setting-customer', 'form')
    })

    test('sales settings form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Setting/Sales')
        await record(page, 'setting-sales', 'form')
    })

    test('media settings form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Setting/Media')
        await record(page, 'setting-media', 'form')
    })

    test('discount edit form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Discount/List')
        await waitForGrid(page, 'discounts-grid')
        const id = await firstItemField(page, 'discounts-grid')
        test.skip(!id, 'no discounts')
        await openPage(page, `/Admin/Discount/Edit/${id}`)
        await record(page, 'discount-edit', 'form')
    })

    test('campaign edit form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Campaign/List')
        await waitForGrid(page, 'campaigns-grid')
        const id = await firstItemField(page, 'campaigns-grid')
        test.skip(!id, 'no campaigns')
        await openPage(page, `/Admin/Campaign/Edit/${id}`)
        await record(page, 'campaign-edit', 'form')
    })

    test('customer edit form', async ({ panelPage: page }) => {
        await openPage(page, '/Admin/Customer/List')
        await waitForGrid(page, 'customers-grid')
        const id = await firstItemField(page, 'customers-grid')
        await openPage(page, `/Admin/Customer/Edit/${id}`)
        await record(page, 'customer-edit', 'form')
    })
})
