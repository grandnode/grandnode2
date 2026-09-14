import { test as base, expect } from '@playwright/test'
import { hasCredentials, LOCALES } from './env.js'
import { login, useLanguage } from './auth.js'

/*
 * Shared fixtures for the panel smoke specs:
 * - `panelPage` is a page signed in to the panel named by test.use({ panel })
 *   and switched to the project's language when one is configured;
 * - every test fails on console errors, uncaught exceptions and alert() dialogs
 *   (display_kendoui_grid_error reports grid failures through alert).
 */
export const test = base.extend({
    panel: ['admin', { option: true }],
    languageCode: [undefined, { option: true }],
    rtl: [false, { option: true }],

    panelPage: async ({ page, panel, languageCode, rtl }, use, testInfo) => {
        test.skip(!hasCredentials(panel), `set GRAND_${panel.toUpperCase()}_EMAIL and GRAND_${panel.toUpperCase()}_PASSWORD`)
        const locale = LOCALES.find(l => l.name === testInfo.project.name)
        test.skip(!!locale?.optional && !languageCode, `no language code configured for ${testInfo.project.name}`)

        const problems = []
        page.on('console', msg => { if (msg.type() === 'error') problems.push(`console: ${msg.text()}`) })
        page.on('pageerror', err => problems.push(`pageerror: ${err.message}`))
        page.on('dialog', dialog => {
            problems.push(`dialog: ${dialog.message()}`)
            dialog.dismiss().catch(() => {})
        })

        await login(page, panel)
        await useLanguage(page, panel, languageCode)
        if (rtl) await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

        await use(page)

        expect(problems, 'no console errors, page errors or alert dialogs').toEqual([])
    }
})

export { expect }
