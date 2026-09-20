/*
 * Codemod stage 2: converts kendoGrid({ ... }) initialisations into <admin-grid> markup.
 *
 *   npm run codemod:grids -- --path Grand.Web.AdminShared/Views/AdminShared/Product   (dry run: diff)
 *   npm run codemod:grids -- --path Grand.Web.AdminShared --write                     (rewrite files)
 *
 * Options:
 *   --path <text>   only files whose repository path contains the text (repeatable)
 *   --write         write the converted files; without it a unified diff is printed
 *   --root <dir>    repository root (default: four levels above adminapp)
 *   --quiet         print only the summary
 *
 * Grids that convert cleanly (class A) need no review. Grids converted with
 * @* CODEMOD-REVIEW: ... *@ markers must be reviewed and the markers removed before
 * committing. Skipped grids stay on Kendo and are listed with the reasons.
 */
import { readdir, readFile, writeFile } from 'node:fs/promises'
import { join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import { convertCshtml } from './lib/kendo-grid-convert.mjs'
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

async function main() {
    const args = parseArgs(process.argv.slice(2))
    const totals = { converted: 0, review: 0, skipped: 0, files: 0 }
    const byClass = {}
    const skipped = []
    const review = []
    for (const base of ['src/Web', 'src/Plugins']) {
        for await (const path of walk(join(args.root, base))) {
            const rel = relative(args.root, path).split(sep).join('/')
            if (args.paths.length && !args.paths.some(p => rel.includes(p))) continue
            const src = await readFile(path, 'utf8')
            if (!/\.kendoGrid\s*\(/.test(src)) continue
            const { output, grids } = convertCshtml(src, { file: rel })
            for (const g of grids) {
                totals[g.action]++
                const key = `${g.class}:${g.action}`
                byClass[key] = (byClass[key] || 0) + 1
                if (g.action === 'skipped') skipped.push(g)
                if (g.action === 'review') review.push(g)
            }
            if (output === src) continue
            totals.files++
            if (args.write) await writeFile(path, output)
            else if (!args.quiet) process.stdout.write(unifiedDiff(src, output, rel))
        }
    }
    const lines = []
    lines.push('', `${args.write ? 'Rewrote' : 'Would rewrite'} ${totals.files} files.`)
    lines.push(`converted: ${totals.converted}, converted with review markers: ${totals.review}, skipped: ${totals.skipped}`)
    lines.push(`by analyzer class: ${Object.entries(byClass).sort().map(([k, v]) => `${k}=${v}`).join(' ')}`)
    if (review.length) {
        lines.push('', 'Review (remove every CODEMOD-REVIEW marker before committing):')
        for (const g of review) lines.push(`  ${g.file}:${g.line} ${g.grid}`, ...g.markers.map(m => `      - ${m}`))
    }
    if (skipped.length) {
        lines.push('', 'Skipped (still Kendo, convert by hand):')
        for (const g of skipped) lines.push(`  ${g.file}:${g.line} ${g.grid} [${g.class}] ${g.reasons.join('; ')}`)
    }
    console.log(lines.join('\n'))
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    main().catch(err => {
        console.error(err)
        process.exit(1)
    })
}
