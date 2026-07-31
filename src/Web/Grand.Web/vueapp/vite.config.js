import { defineConfig } from 'vite'
import { fileURLToPath, URL } from 'node:url'

const resolve = p => fileURLToPath(new URL(p, import.meta.url))

export default defineConfig({
    //assets are served from /bundles/, so url() references inside libs.css
    //(bootstrap-icons fonts) have to be rewritten against that prefix
    base: '/bundles/',
    resolve: {
        alias: [
            //Templates are the Razor markup itself, parsed out of the DOM at runtime,
            //so the build that includes the template compiler is the one we need -
            //the default runtime-only build would render nothing.
            { find: /^vue$/, replacement: resolve('./node_modules/vue/dist/vue.esm-bundler.js') }
        ]
    },
    build: {
        outDir: resolve('../wwwroot/bundles'),
        //style.min.css, style.rtl.min.css and the committed fonts live in the same
        //directory and are NOT produced here. vue-cli emptied the directory on every
        //build and silently deleted the two production stylesheets referenced by
        //Head.cshtml; this must stay false.
        emptyOutDir: false,
        cssCodeSplit: false,
        //the bundle is loaded by a plain <script src> and assigns window.Vue,
        //window.bootstrap etc., so it must not be an ES module
        rollupOptions: {
            input: resolve('./src/main.js'),
            output: {
                format: 'iife',
                entryFileNames: 'app.runtime.bundle.js',
                assetFileNames: info => {
                    const name = info.names?.[0] ?? info.name ?? ''
                    return name.endsWith('.css') ? 'libs.css' : 'fonts/[name][extname]'
                }
            }
        }
    }
})
