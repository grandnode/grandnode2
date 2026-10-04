import js from '@eslint/js'
import globals from 'globals'

export default [
    {
        ignores: ['node_modules/**', 'reports/**', 'e2e/har/**', 'e2e/test-results/**', 'e2e/playwright-report/**']
    },
    js.configs.recommended,
    {
        //bundled browser code
        files: ['src/**/*.js'],
        languageOptions: {
            ecmaVersion: 'latest',
            sourceType: 'module',
            globals: {
                ...globals.browser
            }
        }
    },
    {
        //build scripts, codemods and their tests run in Node
        files: ['scripts/**/*.mjs', 'e2e/**/*.js', '*.config.js'],
        languageOptions: {
            ecmaVersion: 'latest',
            sourceType: 'module',
            globals: {
                ...globals.node
            }
        }
    },
    {
        //Playwright page.evaluate callbacks run inside the admin page, where jQuery
        //and Kendo are globals loaded by HeadAdmin/HeadStore/HeadVendor
        files: ['e2e/**/*.js'],
        languageOptions: {
            globals: {
                ...globals.browser,
                $: 'readonly',
                jQuery: 'readonly',
                kendo: 'readonly'
            }
        }
    }
]
