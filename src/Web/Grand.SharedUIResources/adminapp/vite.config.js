import { defineConfig } from 'vite'
import { fileURLToPath, URL } from 'node:url'

const resolve = p => fileURLToPath(new URL(p, import.meta.url))

//Every bundle is its own IIFE (Rollup cannot split an IIFE build across several inputs),
//so scripts/build.mjs runs one build per entry and passes the entry name in ADMIN_ENTRY.
export const entries = {
    'admin.core': './src/admin.core.js',
    'admin.bootstrap': './src/admin.bootstrap.js',
    'admin.grid': './src/admin.grid.js',
    'admin.ui': './src/admin.ui.js',
    'admin.legacy': './src/admin.legacy.js'
}

//Entries a panel loads; only these are written by a plain `npm run build`. The others
//are built on request (`npm run build -- admin.core`) and must not be committed until
//a Head* partial references them.
export const shippedEntries = ['admin.bootstrap', 'admin.grid', 'admin.ui', 'admin.legacy']

export default defineConfig(() => {
    const entry = process.env.ADMIN_ENTRY || 'admin.grid'
    return {
        //url() in the stylesheets resolves against the file that loads them, so the icon
        //font is found whatever Constants.WwwRoot prefixes the request with
        base: './',
        css: {
            preprocessorOptions: {
                //Bootstrap 5.3 and bootstrap-icons still use @import; silencing the two
                //deprecations keeps the build output readable until Bootstrap 6.
                scss: { silenceDeprecations: ['import', 'global-builtin', 'color-functions', 'mixed-decls'] }
            }
        },
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
                    //one stylesheet per bundle, named after it (admin.grid.css); fonts keep
                    //their own name in a fonts/ folder next to it, so a rebuild overwrites
                    //them rather than piling up hashed copies in the committed output
                    assetFileNames: asset => {
                        const name = asset.names?.[0] || asset.name || ''
                        if (name.endsWith('.css')) return `${entry}.css`
                        if (/\.(woff2?|ttf|eot|otf|svg)$/.test(name)) return 'fonts/[name][extname]'
                        return '[name][extname]'
                    }
                }
            }
        },
        test: {
            include: ['scripts/**/*.test.mjs', 'src/**/*.test.js'],
            environment: 'node'
        }
    }
})
