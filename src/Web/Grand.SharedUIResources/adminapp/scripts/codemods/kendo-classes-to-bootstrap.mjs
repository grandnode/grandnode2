/*
 * The Kendo class names the views still carry -> Bootstrap 5.
 *
 *   npm run codemod:kclasses -- --path Grand.Web.Vendor            (dry run: prints a diff)
 *   npm run codemod:kclasses -- --path Grand.Web.Vendor --write    (rewrites the views)
 *
 * Kendo stopped being loaded two commits before Bootstrap 5, and admin.legacy.css has
 * been reproducing what its stylesheets painted for the k-button, k-link, k-icon and
 * k-input classes around 800 elements still carry. This replaces those classes with the
 * Bootstrap 5 equivalents, which is what lets that stylesheet go.
 *
 * What is deliberately NOT renamed: the k-grid-*, k-detail-row, k-master-row,
 * k-state-selected and k-loading-mask names the grid adapter writes and reads. They are
 * part of the $(el).data('kendoGrid') contract the views and third-party plugins call
 * into, not decoration.
 */
import { readdir, readFile, writeFile } from 'node:fs/promises'
import { join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import { isPanelView } from './bootstrap4-to-bootstrap5.mjs'
import { unifiedDiff } from './lib/diff.mjs'

const here = fileURLToPath(new URL('.', import.meta.url))
const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', '__tests__'])

//One Kendo class for its Bootstrap 5 equivalent. A value of null drops the class.
export const CLASS_MAP = {
    //Kendo's button was a flat light button; the panel already writes btn-default for
    //exactly that (the grid command buttons), and btn-sm matches its metrics
    'k-button': 'btn btn-default btn-sm',
    //k-primary sat next to k-button and repainted it as the accent button
    'k-primary': null,
    //the icon-plus-text button; Bootstrap's .btn already spaces its children
    'k-button-icontext': null,
    //k-link only removed the underline of an anchor
    'k-link': 'text-decoration-none',
    //k-input sat next to form-control and gave it the Material colours
    'k-input': null,
    //the marker of the shown tab pane
    'k-state-active': 'active',
    'k-state-selected': 'active',
    //the icon element itself; the glyph class that follows carries the font
    'k-icon': 'bi'
}

//k-i-* -> bootstrap-icons, the same glyphs admin.legacy.css drew with Font Awesome.
export const ICON_MAP = {
    'k-i-edit': 'bi-pencil',
    'k-i-pencil': 'bi-pencil',
    'k-i-delete': 'bi-trash',
    'k-i-trash': 'bi-trash',
    'k-i-cancel': 'bi-x-lg',
    'k-i-close': 'bi-x-lg',
    'k-i-x': 'bi-x-lg',
    'k-i-save': 'bi-floppy',
    'k-i-floppy': 'bi-floppy',
    'k-i-eye': 'bi-eye',
    'k-i-preview': 'bi-eye',
    'k-i-email': 'bi-envelope',
    'k-i-envelop': 'bi-envelope',
    'k-i-notification': 'bi-bell',
    'k-i-bell': 'bi-bell',
    'k-i-style-builder': 'bi-palette',
    'k-i-cut': 'bi-scissors',
    'k-i-rotate': 'bi-arrow-clockwise',
    'k-i-reload': 'bi-arrow-clockwise',
    'k-i-refresh': 'bi-arrow-clockwise',
    'k-i-check-outline': 'bi-check-circle',
    'k-i-close-outline': 'bi-x-circle',
    'k-i-check': 'bi-check-lg',
    'k-i-plus': 'bi-plus-lg',
    'k-i-add': 'bi-plus-lg',
    'k-i-search': 'bi-search',
    'k-i-download': 'bi-download',
    'k-i-upload': 'bi-upload',
    'k-i-arrow-60-left': 'bi-chevron-left',
    'k-i-arrow-60-right': 'bi-chevron-right',
    'k-i-arrow-60-up': 'bi-chevron-up',
    'k-i-arrow-60-down': 'bi-chevron-down'
}

const PLAIN_CLASS = /^[A-Za-z][A-Za-z0-9_-]*$/

export function convertClassValue(value) {
    const parts = value.split(/(\s+)/)
    const tokens = parts.filter((_, i) => i % 2 === 0).filter(Boolean)
    if (!tokens.some(t => t in CLASS_MAP || t in ICON_MAP)) return { value, changed: false }
    if (!tokens.every(t => PLAIN_CLASS.test(t))) return { value, changed: false, manual: true }

    const out = []
    //k-primary k-button is one button, not two: the accent colour replaces the default one
    const accent = tokens.includes('k-primary') && tokens.includes('k-button')
    for (const token of tokens) {
        if (token === 'k-button' && accent) { out.push('btn', 'btn-primary', 'btn-sm'); continue }
        if (token in ICON_MAP) { out.push(ICON_MAP[token]); continue }
        if (token in CLASS_MAP) {
            const replacement = CLASS_MAP[token]
            if (replacement) out.push(...replacement.split(' '))
            continue
        }
        out.push(token)
    }
    const deduped = out.filter((token, index) => out.indexOf(token) === index)
    const leading = /^\s*/.exec(value)[0]
    const trailing = /\s*$/.exec(value)[0]
    const result = `${leading}${deduped.join(' ')}${trailing}`
    return { value: result, changed: result !== value }
}

/** Rewrites the class attributes of a view, in both quote styles the views use. */
export function convertView(src) {
    let out = ''
    let i = 0
    let changed = 0
    const manual = []
    for (const match of src.matchAll(/\bclass\s*=\s*(["'])([^"']*)\1/g)) {
        const value = match[2]
        const start = match.index + match[0].length - value.length - 1
        const result = convertClassValue(value)
        if (result.manual) { manual.push(value.trim()); continue }
        if (!result.changed) continue
        out += src.slice(i, start) + result.value
        i = start + value.length
        changed++
    }
    out += src.slice(i)
    return { output: out, changed, manual }
}

export function parseArgs(argv) {
    const args = { root: resolve(here, '../../../../../..'), paths: [], write: false, quiet: false }
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === '--root') args.root = resolve(argv[++i])
        else if (argv[i] === '--path') args.paths.push(argv[++i].replace(/\\/g, '/'))
        else if (argv[i] === '--write') args.write = true
        else if (argv[i] === '--quiet') args.quiet = true
        else throw new Error(`unknown option ${argv[i]}`)
    }
    return args
}

async function* walk(dir) {
    let entries
    try {
        entries = await readdir(dir, { withFileTypes: true })
    } catch {
        return
    }
    for (const entry of entries) {
        if (entry.isDirectory()) {
            if (!SKIP_DIRS.has(entry.name)) yield* walk(join(dir, entry.name))
        } else if (entry.name.endsWith('.cshtml')) {
            yield join(dir, entry.name)
        }
    }
}

async function main() {
    const args = parseArgs(process.argv.slice(2))
    const totals = { files: 0, attributes: 0 }
    const manual = []
    for (const base of ['src/Web', 'src/Plugins']) {
        for await (const path of walk(join(args.root, base))) {
            const rel = relative(args.root, path).split(sep).join('/')
            if (!isPanelView(rel)) continue
            if (args.paths.length && !args.paths.some(p => rel.includes(p))) continue
            const src = await readFile(path, 'utf8')
            const { output, changed, manual: fileManual } = convertView(src)
            for (const value of fileManual) manual.push(`${rel}: ${value}`)
            if (output === src) continue
            totals.files++
            totals.attributes += changed
            if (args.write) await writeFile(path, output, 'utf8')
            else if (!args.quiet) process.stdout.write(unifiedDiff(src, output, rel))
        }
    }
    console.log(`\n${args.write ? 'rewrote' : 'would rewrite'} ${totals.files} views, ${totals.attributes} class attributes`)
    if (manual.length) {
        console.log(`\ncheck by hand - ${manual.length} class attributes that mix a Kendo class with Razor:`)
        for (const value of manual) console.log(`  ${value}`)
    }
}

if (process.argv[1] && process.argv[1].replace(/\\/g, '/').endsWith('kendo-classes-to-bootstrap.mjs')) {
    main().catch(error => { console.error(error); process.exit(1) })
}
