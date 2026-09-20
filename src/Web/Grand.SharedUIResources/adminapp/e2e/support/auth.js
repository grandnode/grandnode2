import { expect } from '@playwright/test'
import { env, PANELS } from './env.js'

/**
 * Signs in through the panel's own login form (/admin/login, /store/login,
 * /vendor/login). The form shows either an e-mail or a username field depending on
 * CustomerSettings.UsernamesEnabled; both are handled.
 */
export async function login(page, panel) {
    const { email, password } = env.credentials[panel]
    const { loginPath, area } = PANELS[panel]
    await page.goto(loginPath)
    const user = page.locator('input.email, input.username').first()
    await user.fill(email)
    await page.locator('input.password').fill(password)
    await Promise.all([
        page.waitForURL(url => !url.pathname.toLowerCase().includes('/login'), { timeout: 30_000 }),
        page.locator('.login-button').click()
    ])
    await expect(page, `login to the ${area} panel failed`).toHaveURL(new RegExp(`/${area}`, 'i'))
}

/**
 * Switches the signed-in user's working language through the storefront
 * /changelanguage/{code} route, which sets the same LanguageId user field the panels
 * read (Vendor has no SetLanguage action of its own). This is the only write the
 * smoke suite makes, and it happens only when a language code is configured.
 */
export async function useLanguage(page, panel, languageCode) {
    if (!languageCode) return
    const returnUrl = `/${PANELS[panel].area}`
    const response = await page.goto(`/changelanguage/${encodeURIComponent(languageCode)}?returnUrl=${encodeURIComponent(returnUrl)}`)
    expect(response?.status(), `language ${languageCode} exists`).toBeLessThan(400)
}
