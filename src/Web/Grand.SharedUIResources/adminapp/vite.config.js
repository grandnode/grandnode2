import { defineConfig } from 'vite'
import { fileURLToPath, URL } from 'node:url'

const resolve = p => fileURLToPath(new URL(p, import.meta.url))

export default defineConfig({
    resolve: {
        alias: [
            //Admin templates will be the Razor markup itself, parsed out of the DOM at
            //runtime, so the build that includes the template compiler is the one we
            //need - the default runtime-only build would render nothing.
            { find: /^vue$/, replacement: resolve('./node_modules/vue/dist/vue.esm-bundler.js') }
        ]
    },
    build: {
        outDir: resolve('../wwwroot/administration/bundles'),
        //wwwroot/administration holds the vendored libraries the panels load today;
        //the bundles directory must never be emptied on build, same as the storefront
        emptyOutDir: false,
        cssCodeSplit: false,
        //loaded by a plain <script src> from HeadAdmin/HeadStore/HeadVendor and assigns
        //window.GrandAdmin, so it must not be an ES module
        rollupOptions: {
            input: {
                'admin.core': resolve('./src/admin.core.js')
            },
            output: {
                format: 'iife',
                entryFileNames: '[name].js'
            }
        }
    },
    test: {
        include: ['scripts/**/*.test.mjs', 'src/**/*.test.js'],
        environment: 'node'
    }
})
