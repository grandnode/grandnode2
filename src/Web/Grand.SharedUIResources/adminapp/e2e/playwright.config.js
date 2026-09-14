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
            testIgnore: /har\.record\.spec\.js/,
            use: { locale: locale.browserLocale, languageCode: locale.languageCode, rtl: locale.rtl }
        })),
        {
            //records HAR files for later request parity checks; run explicitly with
            //npm run e2e:har
            name: 'har',
            testMatch: /har\.record\.spec\.js/,
            use: { locale: 'en-US', languageCode: env.languageCodes.en, rtl: false }
        }
    ]
})
