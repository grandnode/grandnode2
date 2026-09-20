import { test, expect } from '../support/fixtures.js'
import { mkdir, writeFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { join } from 'node:path'
import { hasCredentials } from '../support/env.js'
import { login, useLanguage } from '../support/auth.js'
import { expandFirstDetail, gridSelectors, openTab, waitForGrid } from '../support/grid.js'
import { HAR_PAGES } from '../support/har-pages.js'

//Records one HAR per page into e2e/har/ (git-ignored: it holds session cookies,
//antiforgery tokens and store data). Run with: npm run e2e:har
const harDir = fileURLToPath(new URL('../har/', import.meta.url))
const sessions = {}

async function sessionFor(browser, panel, languageCode) {
    if (!sessions[panel]) {
        //sign in outside the recorded context so the login POST (with the password)
        //never lands in a HAR file
        const context = await browser.newContext()
        const page = await context.newPage()
        await login(page, panel)
        await useLanguage(page, panel, languageCode)
        sessions[panel] = await context.storageState()
        await context.close()
    }
    return sessions[panel]
}

for (const entry of HAR_PAGES) {
    test(`${entry.panel}: ${entry.name}`, async ({ browser, baseURL, languageCode }) => {
        test.skip(!hasCredentials(entry.panel), `no credentials for ${entry.panel}`)
        await mkdir(harDir, { recursive: true })
        const harPath = join(harDir, `${entry.panel}-${entry.name}.har`)

        const storageState = await sessionFor(browser, entry.panel, languageCode)
        const context = await browser.newContext({
            baseURL,
            storageState,
            ignoreHTTPSErrors: true,
            recordHar: { path: harPath, content: 'embed', urlFilter: /\/(admin|store|vendor)\//i }
        })
        const page = await context.newPage()
        const captured = []
        try {
            const response = await page.goto(entry.path, { waitUntil: 'networkidle' })
            if (entry.optional && response?.status() === 404) test.skip(true, `${entry.path} not available`)
            expect(response?.status()).toBeLessThan(400)
            if (entry.tab) await openTab(page, entry.tab[0], entry.tab[1])
            const { dataLength } = await waitForGrid(page, entry.grid)

            if (entry.detail && dataLength > 0) await expandFirstDetail(page, entry.grid)

            //rows may exist without an Edit command (e.g. only global tax categories in the Store panel)
            const editable = entry.captureUpdate && dataLength > 0
                && (await page.locator(`#${entry.grid}`).locator(gridSelectors.editButton).count()) > 0
            if (editable) {
                await page.route(entry.captureUpdate, async route => {
                    const request = route.request()
                    captured.push({ url: request.url(), method: request.method(), headers: request.headers(), body: request.postData() })
                    await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' })
                })
                const grid = page.locator(`#${entry.grid}`)
                await grid.locator(gridSelectors.editButton).first().click()
                //Kendo only syncs dirty items; mark the unchanged row dirty so the
                //payload is exactly the row as loaded (<admin-grid> always posts the row)
                await page.evaluate(id => {
                    const widget = $('#' + id).data('kendoGrid')
                    if (!widget.grandGrid) widget.dataSource.data()[0].dirty = true
                }, entry.grid)
                await grid.locator(gridSelectors.editRow).locator(gridSelectors.updateButton).click()
                await expect.poll(() => captured.length).toBeGreaterThan(0)
                await page.waitForLoadState('networkidle')
            }
        } finally {
            await context.close()
        }
        if (captured.length) {
            await writeFile(harPath.replace(/\.har$/, '.update.json'), JSON.stringify(captured, null, 2))
        }
    })
}
