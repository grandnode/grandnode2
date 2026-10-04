import { defineConfig, devices } from '@playwright/test'
import { env, LOCALES } from './support/env.js'

//Smoke suite for the Admin, Store and Vendor panels against a running GrandNode.
//Nothing here starts the application: set GRAND_ADMIN_URL to an instance that is
//already up (see README.md). Tests never save, delete or upload anything.
export default defineConfig({
    testDir: './specs',
    outputDir: './test-results',
    //every locale project changes the same user's working language, so projects and
    //tests must not run at the same time
    fullyParallel: false,
    workers: 1,
    retries: 0,
    timeout: 60_000,
    expect: { timeout: 15_000 },
    reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
    use: {
        ...devices['Desktop Chrome'],
        baseURL: env.baseUrl,
        ignoreHTTPSErrors: true,
        trace: 'retain-on-failure',
        screenshot: 'only-on-failure'
    },
    projects: [
        ...LOCALES.map(locale => ({
            name: locale.name,
            testIgnore: /(har|payload|visual)\.record\.spec\.js/,
            use: { locale: locale.browserLocale, languageCode: locale.languageCode, rtl: locale.rtl }
        })),
        {
            //records HAR files and form payloads for later request parity checks; run
            //explicitly with npm run e2e:har or npm run e2e:payloads
            name: 'har',
            testMatch: /(har|payload)\.record\.spec\.js/,
            use: { locale: 'en-US', languageCode: env.languageCodes.en, rtl: false }
        },
        {
            //The widgets, driven by a browser locale that is deliberately NOT the store's.
            //The account's working language is left alone, so whatever the store culture is,
            //the browser disagrees with it about how a date is written - which is the whole
            //point: nothing on the page may fall back to navigator.language. ar-SA also
            //writes its AM/PM designators in Arabic and its digits in a browser's own way.
            name: 'foreign-locale',
            testMatch: /widgets\.smoke\.spec\.js/,
            use: { locale: 'ar-SA', timezoneId: 'Asia/Riyadh', languageCode: undefined, rtl: false }
        },
        {
            //records a full-page screenshot per page for before/after comparison of a
            //stylesheet change; run explicitly with npm run e2e:visual
            name: 'visual',
            testMatch: /visual\.record\.spec\.js/,
            use: { locale: 'en-US', languageCode: env.languageCodes.en, rtl: false }
        }
    ]
})
