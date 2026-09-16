import { defineConfig } from 'vite'
import { fileURLToPath, URL } from 'node:url'

const resolve = p => fileURLToPath(new URL(p, import.meta.url))

//Every bundle is its own IIFE (Rollup cannot split an IIFE build across several inputs),
//so scripts/build.mjs runs one build per entry and passes the entry name in ADMIN_ENTRY.
export const entries = {
    'admin.core': './src/admin.core.js',
    'admin.grid': './src/admin.grid.js',
    'admin.ui': './src/admin.ui.js',
    'admin.legacy': './src/admin.legacy.js'
}

//Entries a panel loads; only these are written by a plain `npm run build`. The others
//are built on request (`npm run build -- admin.legacy`) and must not be committed until
//a Head* partial references them.
export const shippedEntries = ['admin.grid', 'admin.ui']

export default defineConfig(() => {
    const entry = process.env.ADMIN_ENTRY || 'admin.grid'
    return {
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
                    [entry]: resolve(entries[entry])
                },
                output: {
                    format: 'iife',
                    entryFileNames: '[name].js',
                    //one stylesheet per bundle, named after it (admin.grid.css)
                    assetFileNames: asset => (asset.names?.[0] || asset.name || '').endsWith('.css') ? `${entry}.css` : '[name][extname]'
                }
            }
        },
        test: {
            include: ['scripts/**/*.test.mjs', 'src/**/*.test.js'],
            environment: 'node'
        }
    }
})
