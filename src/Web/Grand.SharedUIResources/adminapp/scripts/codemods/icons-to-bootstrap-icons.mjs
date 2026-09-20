/*
 * Font Awesome 4 and simple-line-icons -> bootstrap-icons, in the Razor views of the
 * three panels.
 *
 *   npm run codemod:icons -- --path Grand.Web.Vendor            (dry run: prints a diff)
 *   npm run codemod:icons -- --path Grand.Web.Vendor --write    (rewrites the views)
 *   npm run codemod:icons -- --report                           (what is used, and what maps to what)
 *
 * The mapping is a table, one entry per glyph, in lib/icons-to-bootstrap-icons.mjs.
 * --report lists any icon class in the views the table does not cover, so the table
 * can be completed before anything is written.
 */
import { readdir, readFile, writeFile } from 'node:fs/promises'
import { join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import { FA_MAP, SLI_MAP, FA_MODIFIERS, convertIconValue } from './lib/icons-to-bootstrap-icons.mjs'
import { isPanelView } from './bootstrap4-to-bootstrap5.mjs'
import { unifiedDiff } from './lib/diff.mjs'

const here = fileURLToPath(new URL('.', import.meta.url))
const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', '__tests__'])
const FA_BASE = new Set(['fa', 'fas', 'far', 'fab', 'fal'])
const KNOWN = new Set([...Object.keys(FA_MAP), ...Object.keys(SLI_MAP), ...Object.keys(FA_MODIFIERS), ...FA_BASE])

export function parseArgs(argv) {
    const args = { root: resolve(here, '../../../../../..'), paths: [], write: false, quiet: false, report: false }
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === '--root') args.root = resolve(argv[++i])
        else if (argv[i] === '--path') args.paths.push(argv[++i].replace(/\\/g, '/'))
        else if (argv[i] === '--write') args.write = true
        else if (argv[i] === '--quiet') args.quiet = true
        else if (argv[i] === '--report') args.report = true
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

/** Rewrites the class attributes of a view. Only values that carry an icon class change. */
export function convertView(src) {
    let out = ''
    let i = 0
    let changed = 0
    const manual = []
    for (const match of src.matchAll(/\bclass\s*=\s*"([^"]*)"/g)) {
        const value = match[1]
        const start = match.index + match[0].length - value.length - 1
        const result = convertIconValue(value)
        if (result.manual) { manual.push(value.trim()); continue }
        if (!result.changed) continue
        out += src.slice(i, start) + result.value
        i = start + value.length
        changed++
    }
    out += src.slice(i)
    return { output: out, changed, manual }
}

async function main() {
    const args = parseArgs(process.argv.slice(2))
    const totals = { files: 0, attributes: 0 }
    const used = new Map()
    const unknown = new Map()
    const manual = []
    for (const base of ['src/Web', 'src/Plugins']) {
        for await (const path of walk(join(args.root, base))) {
            const rel = relative(args.root, path).split(sep).join('/')
            if (!isPanelView(rel)) continue
            if (args.paths.length && !args.paths.some(p => rel.includes(p))) continue
            const src = await readFile(path, 'utf8')
            for (const match of src.matchAll(/\bclass\s*=\s*"([^"]*)"/g)) {
                for (const token of match[1].split(/\s+/)) {
                    if (!/^(fa|icon)-/.test(token)) continue
                    const bucket = KNOWN.has(token) ? used : unknown
                    bucket.set(token, (bucket.get(token) || 0) + 1)
                }
            }
            if (args.report) continue
            const { output, changed, manual: fileManual } = convertView(src)
            for (const value of fileManual) manual.push(`${rel}: ${value}`)
            if (output === src) continue
            totals.files++
            totals.attributes += changed
            if (args.write) await writeFile(path, output, 'utf8')
            else if (!args.quiet) process.stdout.write(unifiedDiff(src, output, rel))
        }
    }
    if (args.report) {
        const rows = [...used.entries()].sort((a, b) => b[1] - a[1])
        console.log(`${rows.length} mapped icon classes in the views:`)
        for (const [name, count] of rows) {
            console.log(`  ${String(count).padStart(5)}  ${name.padEnd(26)} -> ${FA_MAP[name] || SLI_MAP[name] || FA_MODIFIERS[name] || '(base class, dropped)'}`)
        }
    } else {
        console.log(`\n${args.write ? 'rewrote' : 'would rewrite'} ${totals.files} views, ${totals.attributes} class attributes`)
    }
    if (unknown.size) {
        console.log(`\nnot in the table - add them before writing:`)
        for (const [name, count] of [...unknown.entries()].sort((a, b) => b[1] - a[1])) console.log(`  ${count} ${name}`)
    }
    if (manual.length) {
        console.log(`\ncheck by hand - ${manual.length} class attributes that mix an icon with Razor:`)
        for (const value of manual) console.log(`  ${value}`)
    }
}

if (process.argv[1] && process.argv[1].replace(/\\/g, '/').endsWith('icons-to-bootstrap-icons.mjs')) {
    main().catch(error => { console.error(error); process.exit(1) })
}
