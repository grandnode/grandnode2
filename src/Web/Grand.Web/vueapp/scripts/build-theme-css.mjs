/*
 * Builds the production theme stylesheets.
 *
 * Head.cshtml loads the six theme stylesheets raw in Development and a single
 * concatenated, minified file in Production. Nothing used to generate that file:
 * it was edited by hand and committed, so it drifted whenever someone changed a
 * source and forgot it (commit bc5b81b34 changed header.css and left style.min.css
 * behind), and `vue-cli-service build` wiped it outright because it emptied
 * wwwroot/bundles first.
 *
 * The order below is the cascade order Head.cshtml uses - do not reorder.
 */
import { build } from 'esbuild'
import { mkdtemp, writeFile, rm } from 'node:fs/promises'
import { tmpdir } from 'node:os'
import { join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const here = fileURLToPath(new URL('.', import.meta.url))
const cssRoot = resolve(here, '../../wwwroot/theme/css')
const outRoot = resolve(here, '../../wwwroot/bundles')

const parts = ['common', 'header', 'catalog', 'product', 'customer', 'cart']

async function bundle(suffix, outFile) {
    //esbuild needs a single entry point, so hand it a generated file that @imports
    //the parts in order; it inlines them and drops the @import statements
    const dir = await mkdtemp(join(tmpdir(), 'grand-theme-css-'))
    const entry = join(dir, 'entry.css')
    const imports = parts
        .map(p => `@import "${join(cssRoot, p, `${p}${suffix}.css`).replace(/\\/g, '/')}";`)
        .join('\n')
    await writeFile(entry, imports)

    try {
        await build({
            entryPoints: [entry],
            bundle: true,
            minify: true,
            //the sources contain no url() references, so there is nothing to rewrite
            //and no assets to emit alongside
            loader: { '.css': 'css' },
            outfile: join(outRoot, outFile),
            logLevel: 'warning'
        })
    } finally {
        await rm(dir, { recursive: true, force: true })
    }
    console.log(`  ${outFile}`)
}

console.log('building theme stylesheets...')
await bundle('', 'style.min.css')
await bundle('.rtl', 'style.rtl.min.css')
