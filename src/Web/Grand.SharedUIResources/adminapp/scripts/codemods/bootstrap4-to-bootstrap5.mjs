/*
 * Bootstrap 4 -> Bootstrap 5 codemod for the Razor views of the three panels.
 *
 *   npm run codemod:bs5 -- --path Grand.Web.Vendor            (dry run: prints a diff)
 *   npm run codemod:bs5 -- --path Grand.Web.Vendor --write    (rewrites the views)
 *
 * Options:
 *   --path <text>   only files whose repository path contains the text (repeatable)
 *   --write         write the converted files; without it a unified diff is printed
 *   --root <dir>    repository root (default: four levels above adminapp)
 *   --quiet         print only the summary
 *
 * Run it in batches and read the diff: class attributes that build a class name out of
 * Razor are listed under "check by hand" and are not rewritten.
 */
import { readdir, readFile, writeFile } from 'node:fs/promises'
import { join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import { convertView } from './lib/bootstrap4-to-bootstrap5.mjs'
import { unifiedDiff } from './lib/diff.mjs'

const here = fileURLToPath(new URL('.', import.meta.url))
const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', '__tests__'])

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

/**
 * True for a view one of the three panels renders. The storefront has its own Vite app
 * and has been on Bootstrap 5 for some time, so Grand.Web and the storefront half of a
 * plugin (everything a plugin keeps outside Areas/) must not be touched.
 */
export function isPanelView(rel) {
    if (rel.startsWith('src/Web/Grand.Web/')) return false
    if (rel.startsWith('src/Plugins/')) {
        if (/\/Areas\/(Admin|Store|Vendor)\//.test(rel)) return true
        //the discount rule configuration screens are rendered inside the Admin discount
        //editor, but the plugin keeps them outside Areas/
        return rel.startsWith('src/Plugins/DiscountRules.Standard/Views/')
    }
    return true
}

async function main() {
    const args = parseArgs(process.argv.slice(2))
    const totals = { files: 0, classes: 0, attributes: 0, dropped: 0 }
    const notes = []
    for (const base of ['src/Web', 'src/Plugins']) {
        for await (const path of walk(join(args.root, base))) {
            const rel = relative(args.root, path).split(sep).join('/')
            if (args.paths.length && !args.paths.some(p => rel.includes(p))) continue
            if (!isPanelView(rel)) continue
            const src = await readFile(path, 'utf8')
            const { output, stats, notes: fileNotes } = convertView(src)
            for (const note of fileNotes) notes.push(`${rel}: ${note}`)
            if (output === src) continue
            totals.files++
            totals.classes += stats.classes
            totals.attributes += stats.attributes
            totals.dropped += stats.dropped
            if (args.write) await writeFile(path, output, 'utf8')
            else if (!args.quiet) process.stdout.write(unifiedDiff(src, output, rel))
        }
    }
    console.log(`\n${args.write ? 'rewrote' : 'would rewrite'} ${totals.files} views: ` +
        `${totals.classes} class attributes, ${totals.attributes} tags with Bootstrap data attributes, ` +
        `${totals.dropped} class names dropped`)
    if (notes.length) {
        console.log(`\ncheck by hand - ${notes.length} class attributes built with Razor:`)
        for (const note of notes) console.log(`  ${note}`)
    }
}

if (process.argv[1] && import.meta.url.endsWith(process.argv[1].replace(/\\/g, '/').split('/').pop())) {
    main().catch(error => { console.error(error); process.exit(1) })
}
