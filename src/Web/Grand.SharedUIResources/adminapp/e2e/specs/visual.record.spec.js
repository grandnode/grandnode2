import { test, expect } from '../support/fixtures.js'
import { mkdir } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import { isAbsolute, join } from 'node:path'
import { hasCredentials } from '../support/env.js'
import { firstItemField, openTab, waitForGrid } from '../support/grid.js'
import { VISUAL_PAGES } from '../support/visual-pages.js'

//Records one full-page screenshot per entry of VISUAL_PAGES into a directory, so two
//builds can be compared by eye (the Kendo stylesheets carry the look of the `k-button`
//and `k-link` classes the views still use). Nothing is saved on the server.
//
//  GRAND_SHOT_DIR=before npx playwright test ... visual.record.spec.js
//
//GRAND_SHOT_DIR is a name under e2e/screenshots/ or an absolute path; it defaults to
//"current". e2e/screenshots/ is git-ignored: the pages show real store data.
const target = process.env.GRAND_SHOT_DIR?.trim() || 'current'
const shotDir = isAbsolute(target) ? target : join(fileURLToPath(new URL('../screenshots/', import.meta.url)), target)

const panels = [...new Set(VISUAL_PAGES.map(entry => entry.panel))]

for (const panel of panels) {
    test.describe(panel, () => {
        //the signed-in panel is a fixture option, so each panel needs its own block
        test.use({ panel })
        for (const entry of VISUAL_PAGES.filter(page => page.panel === panel)) record(entry)
    })
}

function record(entry) {
    test(`${entry.panel}: ${entry.name}`, async ({ panelPage }) => {
        test.skip(!hasCredentials(entry.panel), `no credentials for ${entry.panel}`)
        await mkdir(shotDir, { recursive: true })

        let path = entry.path
        if (entry.lookup) {
            const response = await panelPage.goto(entry.lookup.path, { waitUntil: 'domcontentloaded' })
            if (entry.optional && response?.status() === 404) test.skip(true, `${entry.lookup.path} not available`)
            await panelPage.waitForLoadState('networkidle')
            const { dataLength } = await waitForGrid(panelPage, entry.lookup.grid)
            test.skip(dataLength === 0, `${entry.lookup.path} has no rows`)
            path = entry.lookup.url(await firstItemField(panelPage, entry.lookup.grid))
        }

        const response = await panelPage.goto(path, { waitUntil: 'domcontentloaded' })
        if (entry.optional && (response?.status() === 404 || response?.status() === 403)) test.skip(true, `${path} not available`)
        expect(response?.status(), `GET ${path}`).toBeLessThan(400)
        await panelPage.waitForLoadState('networkidle')

        if (entry.tabs) await openTab(panelPage, entry.tabs[0], entry.tabs[1])
        if (entry.grid) await waitForGrid(panelPage, entry.grid)
        //the dashboard charts and the grids animate in; settle before the shot
        await panelPage.waitForTimeout(500)

        await panelPage.screenshot({ path: join(shotDir, `${entry.panel}-${entry.name}.png`), fullPage: true })
    })
}

