//npm run build: one Vite build per shipped bundle entry (IIFE output cannot be code-split).
//npm run build -- admin.legacy builds a named entry instead.
//
//After admin.bootstrap is built, its stylesheet is mirrored into admin.bootstrap.rtl.css
//with rtlcss. That file used to be a hand-maintained 7 382-line copy
//(wwwroot/administration/build/css/custom-rtl.css) which had drifted away from the
//left-to-right one; generating it means the two can no longer disagree.
import { build } from 'vite'
import { fileURLToPath } from 'node:url'
import { readFile, writeFile } from 'node:fs/promises'
import rtlcss from 'rtlcss'
import { entries, shippedEntries } from '../vite.config.js'

const configFile = fileURLToPath(new URL('../vite.config.js', import.meta.url))
const outDir = fileURLToPath(new URL('../../wwwroot/administration/bundles/', import.meta.url))
const rtlHead = fileURLToPath(new URL('../src/styles/_rtl.scss', import.meta.url))

//Entries whose stylesheet is mirrored after the build.
const RTL_ENTRIES = new Set(['admin.bootstrap'])

async function writeRtl(entry) {
    const source = await readFile(`${outDir}${entry}.css`, 'utf8')
    const mirrored = rtlcss.process(source, { useCalc: true })
    //what must differ in right-to-left beyond mirrored geometry: the Persian webfont the
    //old custom-rtl.css carried, compiled separately because rtlcss only moves geometry
    const extra = await compileRtlHead()
    await writeFile(`${outDir}${entry}.rtl.css`, `${extra}${mirrored}`, 'utf8')
    console.log(`built ${entry}.rtl.css`)
}

async function compileRtlHead() {
    const { compile } = await import('sass')
    const { css } = compile(rtlHead, { loadPaths: [fileURLToPath(new URL('../node_modules/', import.meta.url))], style: 'compressed' })
    return css ? `${css}\n` : ''
}

const only = process.argv.slice(2)
const selected = only.length > 0 ? only : shippedEntries
for (const entry of selected) {
    if (!entries[entry]) throw new Error(`unknown entry ${entry}`)
    process.env.ADMIN_ENTRY = entry
    await build({ configFile, logLevel: 'warn' })
    console.log(`built ${entry}`)
    if (RTL_ENTRIES.has(entry)) await writeRtl(entry)
}
