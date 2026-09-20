/*
 * Codemod stage 1 (read-only): inventory and classify every kendoGrid initialisation.
 *
 *   npm run analyze:grids [-- --root <repo root>] [-- --out <dir>]
 *
 * Scans every .cshtml under src/Web and src/Plugins (bin, obj and node_modules
 * excluded), writes reports/kendo-grids.csv and reports/kendo-grids.summary.md and
 * prints a short summary. Nothing outside the reports directory is written.
 */
import { readdir, readFile, mkdir, writeFile } from 'node:fs/promises'
import { join, relative, resolve, sep } from 'node:path'
import { fileURLToPath } from 'node:url'
import { analyzeCshtml } from './lib/kendo-grid-analysis.mjs'

const here = fileURLToPath(new URL('.', import.meta.url))
//__tests__ holds the analyzer's own .cshtml fixtures
const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '.git', '.vs', '__tests__'])

function parseArgs(argv) {
    const args = { root: resolve(here, '../../../../../..'), out: resolve(here, '../../reports') }
    for (let i = 0; i < argv.length; i++) {
        if (argv[i] === '--root') args.root = resolve(argv[++i])
        else if (argv[i] === '--out') args.out = resolve(argv[++i])
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

export function projectOf(relPath) {
    const parts = relPath.split('/')
    if (parts[0] === 'src' && parts[1] === 'Plugins') return `Plugins/${parts[2]}`
    if (parts[0] === 'src') return parts[2]
    return parts[0]
}

const CSV_COLUMNS = [
    'file', 'line', 'grid', 'project', 'class', 'reasons', 'parseError',
    'editMode', 'inlineEdit', 'transports', 'crud', 'toolbarCreate', 'columns',
    'templatesEncoded', 'templatesRaw', 'templatesRawNonTrivial', 'templateFunctions',
    'commands', 'customCommandClick', 'detail', 'selectable', 'checkbox', 'minScreenWidth', 'formats',
    'events', 'editors', 'parameterMap', 'additionalData', 'sortable', 'filterable',
    'razorExpressions', 'razorBlocks', 'razorPartials', 'externalDataKendoGridInFile'
]

const csvCell = v => {
    const s = Array.isArray(v) ? v.join(';') : v === undefined || v === null ? '' : String(v)
    return /[",\n;]/.test(s) ? `"${s.replace(/"/g, '""')}"` : s
}

export function toRow(file, grid, external) {
    const a = grid.analysis
    const f = a.features
    const transports = f.transports ?? []
    return {
        file,
        line: grid.line,
        grid: grid.name,
        project: projectOf(file),
        class: a.classification,
        reasons: [...a.reasonsC, ...a.reasonsB],
        parseError: grid.parseError ?? '',
        editMode: f.editMode ?? '',
        inlineEdit: f.editMode === 'inline',
        transports,
        crud: transports.some(t => t !== 'read'),
        toolbarCreate: f.toolbarCreate ?? '',
        columns: f.columns ?? '',
        templatesEncoded: f.templatesEncoded ?? '',
        templatesRaw: f.templatesRaw ?? '',
        templatesRawNonTrivial: f.templatesRawNonTrivial ?? '',
        templateFunctions: f.templateFunctions ?? '',
        commands: f.commands ?? [],
        customCommandClick: a.reasonsB.includes('custom-command-click'),
        detail: f.detail ?? '',
        selectable: f.selectable ?? '',
        checkbox: f.checkbox ?? '',
        minScreenWidth: f.minScreenWidth ?? '',
        formats: f.formats ?? [],
        events: f.events ?? [],
        editors: f.editors ?? '',
        parameterMap: f.parameterMap ?? '',
        additionalData: f.additionalData ?? '',
        sortable: f.sortable ?? '',
        filterable: f.filterable ?? '',
        razorExpressions: f.razorExpressions ?? '',
        razorBlocks: f.razorBlocks ?? '',
        razorPartials: f.razorPartials ?? '',
        externalDataKendoGridInFile: external.count
    }
}

const pct = (n, total) => (total ? `${Math.round((n / total) * 100)}%` : '0%')

export function summarize(rows, files) {
    const total = rows.length
    const byClass = { A: 0, B: 0, C: 0 }
    const byProject = {}
    const reasons = {}
    const formats = {}
    let inline = 0, crud = 0, create = 0, detail = 0, selectable = 0, checkbox = 0, minScreen = 0
    let encoded = 0, raw = 0, rawNonTrivial = 0, editors = 0, parameterMap = 0, dataBound = 0

    for (const r of rows) {
        byClass[r.class]++
        byProject[r.project] ??= { A: 0, B: 0, C: 0, files: new Set() }
        byProject[r.project][r.class]++
        byProject[r.project].files.add(r.file)
        for (const reason of r.reasons) reasons[reason] = (reasons[reason] ?? 0) + 1
        for (const fmt of r.formats) formats[fmt] = (formats[fmt] ?? 0) + 1
        if (r.inlineEdit) inline++
        if (r.crud) crud++
        if (r.toolbarCreate === true) create++
        if (r.detail === true) detail++
        if (r.selectable === true) selectable++
        if (r.checkbox === true) checkbox++
        if (r.minScreenWidth) minScreen += r.minScreenWidth
        if (r.templatesEncoded) encoded += r.templatesEncoded
        if (r.templatesRaw) raw += r.templatesRaw
        if (r.templatesRawNonTrivial) rawNonTrivial += r.templatesRawNonTrivial
        if (r.editors) editors += r.editors
        if (r.parameterMap === true) parameterMap++
        if (r.events.includes('dataBound')) dataBound++
    }

    const filesWithGrids = new Set(rows.map(r => r.file)).size
    const external = files.filter(f => f.external.count > 0)
    const externalTotal = external.reduce((n, f) => n + f.external.count, 0)
    const externalMethods = {}
    for (const f of external) {
        for (const [m, n] of Object.entries(f.external.methods)) externalMethods[m] = (externalMethods[m] ?? 0) + n
    }

    return {
        total, byClass, byProject, reasons, formats, filesWithGrids, filesScanned: files.length,
        external, externalTotal, externalMethods,
        counts: { inline, crud, create, detail, selectable, checkbox, minScreen, encoded, raw, rawNonTrivial, editors, parameterMap, dataBound }
    }
}

const sortDesc = obj => Object.entries(obj).sort((a, b) => b[1] - a[1])

export function renderSummary(s, rows) {
    const lines = []
    lines.push('# kendoGrid inventory', '')
    lines.push('Generated by `npm run analyze:grids` (codemod stage 1, read-only).', '')
    lines.push(`- .cshtml files scanned: ${s.filesScanned}`)
    lines.push(`- files with kendoGrid: ${s.filesWithGrids}`)
    lines.push(`- kendoGrid initialisations: **${s.total}**`, '')
    lines.push('## Classification', '')
    lines.push('| class | grids | share |', '|---|---|---|')
    for (const c of ['A', 'B', 'C']) lines.push(`| ${c} | ${s.byClass[c]} | ${pct(s.byClass[c], s.total)} |`)
    lines.push('', 'A = clean, B = convert + review, C = manual.', '')
    lines.push('## Per project', '')
    lines.push('| project | files | A | B | C | total |', '|---|---|---|---|---|---|')
    for (const [name, p] of Object.entries(s.byProject).sort()) {
        lines.push(`| ${name} | ${p.files.size} | ${p.A} | ${p.B} | ${p.C} | ${p.A + p.B + p.C} |`)
    }
    lines.push('', '## Features', '')
    const c = s.counts
    lines.push('| feature | count |', '|---|---|')
    lines.push(`| inline edit grids | ${c.inline} |`)
    lines.push(`| grids with create/update/destroy transport | ${c.crud} |`)
    lines.push(`| toolbar create | ${c.create} |`)
    lines.push(`| detail grids (detailInit/detailTemplate) | ${c.detail} |`)
    lines.push(`| selectable | ${c.selectable} |`)
    lines.push(`| checkbox column | ${c.checkbox} |`)
    lines.push(`| columns with minScreenWidth | ${c.minScreen} |`)
    lines.push(`| template outputs encoded (#: #) | ${c.encoded} |`)
    lines.push(`| template outputs raw (#= #) | ${c.raw} |`)
    lines.push(`| raw outputs of non-trivial fields | ${c.rawNonTrivial} |`)
    lines.push(`| column editor functions | ${c.editors} |`)
    lines.push(`| parameterMap | ${c.parameterMap} |`)
    lines.push(`| dataBound handlers | ${c.dataBound} |`)
    lines.push('', '## Column formats', '')
    lines.push('| format | columns |', '|---|---|')
    for (const [fmt, n] of sortDesc(s.formats)) lines.push(`| \`${fmt}\` | ${n} |`)
    lines.push('', '## Reasons (B and C)', '')
    lines.push('| reason | grids |', '|---|---|')
    for (const [reason, n] of sortDesc(s.reasons)) lines.push(`| ${reason} | ${n} |`)
    lines.push('', '## External `.data("kendoGrid")` usage', '')
    lines.push(`${s.externalTotal} usages in ${s.external.length} files.`, '')
    lines.push('| member | usages |', '|---|---|')
    for (const [m, n] of sortDesc(s.externalMethods)) lines.push(`| ${m} | ${n} |`)
    lines.push('', '| file | usages |', '|---|---|')
    for (const f of [...s.external].sort((a, b) => b.external.count - a.external.count)) {
        lines.push(`| ${f.file} | ${f.external.count} |`)
    }
    lines.push('', '## Class C grids', '')
    lines.push('| file | line | grid | reasons |', '|---|---|---|---|')
    for (const r of rows.filter(r => r.class === 'C')) {
        lines.push(`| ${r.file} | ${r.line} | ${r.grid} | ${r.reasons.join(', ')}${r.parseError ? ` (${r.parseError})` : ''} |`)
    }
    lines.push('')
    return lines.join('\n')
}

async function main() {
    const args = parseArgs(process.argv.slice(2))
    const files = []
    const rows = []
    for (const base of ['src/Web', 'src/Plugins']) {
        for await (const path of walk(join(args.root, base))) {
            const rel = relative(args.root, path).split(sep).join('/')
            const src = (await readFile(path, 'utf8')).replace(/^\uFEFF/, '')
            const { grids, external } = analyzeCshtml(src)
            files.push({ file: rel, external })
            for (const grid of grids) rows.push(toRow(rel, grid, external))
        }
    }
    rows.sort((a, b) => a.file.localeCompare(b.file) || a.line - b.line)

    const summary = summarize(rows, files)
    await mkdir(args.out, { recursive: true })
    const csv = [CSV_COLUMNS.join(','), ...rows.map(r => CSV_COLUMNS.map(c => csvCell(r[c])).join(','))].join('\n')
    await writeFile(join(args.out, 'kendo-grids.csv'), csv + '\n')
    await writeFile(join(args.out, 'kendo-grids.summary.md'), renderSummary(summary, rows))

    const { A, B, C } = summary.byClass
    console.log(`Scanned ${summary.filesScanned} .cshtml files, ${summary.filesWithGrids} with kendoGrid.`)
    console.log(`kendoGrid initialisations: ${summary.total}`)
    console.log(`  A (clean)       ${A} (${pct(A, summary.total)})`)
    console.log(`  B (review)      ${B} (${pct(B, summary.total)})`)
    console.log(`  C (manual)      ${C} (${pct(C, summary.total)})`)
    console.log(`External .data("kendoGrid") usages: ${summary.externalTotal} in ${summary.external.length} files`)
    console.log(`Reports: ${relative(process.cwd(), args.out) || '.'}${sep}kendo-grids.csv, kendo-grids.summary.md`)
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    main().catch(err => {
        console.error(err)
        process.exit(1)
    })
}
