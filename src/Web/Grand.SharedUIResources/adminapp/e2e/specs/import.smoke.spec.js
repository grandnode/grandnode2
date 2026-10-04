import { test, expect } from '../support/fixtures.js'

//Every "import from a file" window in the Admin panel. The one thing that has to be true
//of all of them is that a click on the file field reaches the field: waitForEvent
//('filechooser') only fires when the browser itself opens the picker, which is what the
//Bootstrap 5 `.modal-dialog { pointer-events: none }` on these windows used to prevent.
//Nothing is picked and nothing is submitted, so the suite still uploads nothing.
const WINDOWS = [
    { name: 'brands', path: '/Admin/Brand/List', button: '#importexcel', file: '#importexcelfile' },
    { name: 'categories', path: '/Admin/Category/List', button: '#importexcel', file: '#importexcelfile' },
    { name: 'collections', path: '/Admin/Collection/List', button: '#importexcel', file: '#importexcelfile' },
    { name: 'products', path: '/Admin/Product/List', button: '#importexcel', file: '#importexcelfile' },
    { name: 'countries', path: '/Admin/Country/List', button: '#importexcel', file: '#importexcelfile' },
    { name: 'newsletter subscriptions', path: '/Admin/NewsLetterSubscription/List', button: '#importcsv', file: '#importcsvfile' },
    { name: 'plugins', path: '/Admin/Plugin/List', button: '#importfile', file: '#importfiledialog' }
]

test.use({ panel: 'admin' })

test.describe('import windows', () => {
    for (const window of WINDOWS) {
        test(`the file field of the ${window.name} import window opens the picker`, async ({ panelPage }) => {
            //A plugin logo is addressed against the store's own URL, which on an installation
            //reached through a different host than the store record names (a developer's
            //Kestrel port) fails to load and fills the console. That is the page, not this
            //window, so the picture is answered here rather than left to fail.
            if (window.path.includes('/Plugin/')) {
                await panelPage.route('**/Plugins/**/logo.jpg', route => route.fulfill({
                    status: 200,
                    contentType: 'image/gif',
                    body: Buffer.from('R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw==', 'base64')
                }))
            }
            await panelPage.goto(window.path)
            await panelPage.locator(window.button).click()

            const field = panelPage.locator(window.file)
            await expect(field).toBeVisible()

            const [chooser] = await Promise.all([
                panelPage.waitForEvent('filechooser'),
                field.click()
            ])
            expect(chooser.element()).toBeTruthy()
        })
    }

    test('the file field of the language resource import window opens the picker', async ({ panelPage }) => {
        await panelPage.goto('/Admin/Language/List')
        const href = await panelPage.locator('#languages-grid a[href*="Edit/"]').first().getAttribute('href')
        await panelPage.goto(new URL(href, new URL('/Admin/Language/List', panelPage.url())).pathname)

        await panelPage.locator('#importxml').click()
        const field = panelPage.locator('#importxmlfile')
        await expect(field).toBeVisible()

        const [chooser] = await Promise.all([
            panelPage.waitForEvent('filechooser'),
            field.click()
        ])
        expect(chooser.element()).toBeTruthy()
    })
})
