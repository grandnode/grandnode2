/*
 * Read-only analysis of kendoGrid({ ... }) initialisations in Razor views.
 *
 * Each grid is classified for the kendoGrid -> <admin-grid> codemod:
 *   A  clean - the codemod can convert it without review
 *   B  convertible, but needs a reviewer (editors, event handlers, raw template output...)
 *   C  manual - partials inject configuration, config is dynamic, or it does not parse
 */
import { parseExpressionAt } from 'acorn'
import { maskCshtml, lineOf } from './razor-mask.mjs'

const GRID_OPTIONS = new Set([
    'dataSource', 'columns', 'pageable', 'editable', 'toolbar', 'scrollable', 'sortable', 'filterable',
    'selectable', 'autoBind', 'height', 'navigatable', 'noRecords',
    'detailInit', 'detailTemplate', 'dataBound', 'dataBinding', 'edit', 'save', 'change', 'remove', 'cancel', 'saveChanges',
    'groupable', 'columnMenu', 'reorderable', 'resizable'
])
const GRID_EVENTS = ['dataBound', 'dataBinding', 'edit', 'save', 'change', 'remove', 'cancel', 'saveChanges']
const UNSUPPORTED_FEATURES = ['groupable', 'columnMenu', 'reorderable', 'resizable']
const DATASOURCE_OPTIONS = new Set([
    'transport', 'schema', 'pageSize', 'serverPaging', 'serverFiltering', 'serverSorting', 'batch',
    'requestStart', 'requestEnd', 'error', 'change', 'data', 'sort', 'filter', 'type'
])
const COLUMN_OPTIONS = new Set([
    'field', 'title', 'width', 'template', 'headerTemplate', 'format', 'attributes', 'headerAttributes',
    'minScreenWidth', 'hidden', 'editor', 'command', 'type', 'encoded', 'sortable', 'filterable'
])
const TRANSPORT_OPS = ['read', 'create', 'update', 'destroy']
const STANDARD_COMMANDS = new Set(['edit', 'destroy'])

const STANDARD_ERROR_HANDLER = /^function\s*\(\s*(\w+)\s*\)\s*\{\s*display_kendoui_grid_error\(\s*\1\s*\);?\s*(\/\/[^\n]*\s*)?(this\.cancelChanges\(\);?)?\s*\}$/
const STANDARD_REQUEST_END = /^function\s*\(\s*(\w+)\s*\)\s*\{\s*if\s*\(\s*\1\.type\s*===?\s*["'](create|update)["']\s*\|\|\s*\1\.type\s*===?\s*["'](create|update)["']\s*\)\s*\{\s*this\.read\(\);?\s*\}\s*\}$/

const normalize = code => code.replace(/\/\/[^\n]*/g, '').replace(/\s+/g, ' ').trim()

function keyName(prop) {
    if (prop.type !== 'Property') return null
    if (prop.key.type === 'Identifier') return prop.key.name
    if (prop.key.type === 'Literal') return String(prop.key.value)
    return null
}

function getProp(obj, name) {
    if (!obj || obj.type !== 'ObjectExpression') return undefined
    for (const p of obj.properties) if (keyName(p) === name) return p.value
    return undefined
}

/** Resolves a string literal or a +concatenation of literals; parts that are not literal are reported. */
export function staticString(node) {
    if (!node) return { value: null, dynamic: false }
    if (node.type === 'Literal' && typeof node.value === 'string') return { value: node.value, dynamic: false }
    if (node.type === 'TemplateLiteral') {
        return { value: node.quasis.map(q => q.value.cooked).join('${}'), dynamic: node.expressions.length > 0 }
    }
    if (node.type === 'BinaryExpression' && node.operator === '+') {
        const l = staticString(node.left)
        const r = staticString(node.right)
        if (l.value === null && r.value === null) return { value: null, dynamic: true }
        return { value: (l.value ?? '') + (r.value ?? ''), dynamic: l.dynamic || r.dynamic || l.value === null || r.value === null }
    }
    return { value: null, dynamic: true }
}

const TRIVIAL_RAW = [/^[\w.]*Id$/, /^kendo\.toString\(/, /^kendo\.htmlEncode\(/, /^(true|false|\d+)$/]
const SIMPLE_CODE = /^(if\s*\(.*\)\s*\{|\}\s*else\s*if\s*\(.*\)\s*\{|\}\s*else\s*\{|\}|else\s*\{|\{)$/

/** Splits a Kendo template into #: #, #= # and # code # parts. */
export function analyzeKendoTemplate(template) {
    const result = { encoded: 0, raw: 0, rawNonTrivial: [], code: 0, complexCode: [], balanced: true }
    const parts = []
    let buf = ''
    let inside = false
    for (let i = 0; i < template.length; i++) {
        const c = template[i]
        if (c === '\\' && template[i + 1] === '#') { buf += '#'; i++; continue }
        if (c === '#') {
            if (inside) parts.push(buf)
            buf = ''
            inside = !inside
            continue
        }
        buf += c
    }
    if (inside) result.balanced = false
    for (const part of parts) {
        if (part.startsWith(':')) { result.encoded++; continue }
        if (part.startsWith('=')) {
            const expr = part.slice(1).trim()
            if (/^kendo\.htmlEncode\(/.test(expr)) { result.encoded++; continue }
            result.raw++
            if (!TRIVIAL_RAW.some(re => re.test(expr))) result.rawNonTrivial.push(expr)
            continue
        }
        result.code++
        const code = part.trim()
        if (!SIMPLE_CODE.test(code)) result.complexCode.push(code)
    }
    return result
}

/**
 * Analyses one kendoGrid configuration object.
 * `razor` is the list of Razor constructs whose masked position falls inside the object.
 */
export function analyzeGridConfig(config, source, razor = []) {
    const B = new Set()
    const C = new Set()
    const f = {
        columns: 0,
        editMode: 'none',
        transports: [],
        toolbarCreate: false,
        templatesEncoded: 0,
        templatesRaw: 0,
        templatesRawNonTrivial: 0,
        templateFunctions: 0,
        commands: [],
        detail: false,
        selectable: false,
        checkbox: false,
        minScreenWidth: 0,
        formats: [],
        events: [],
        editors: 0,
        parameterMap: false,
        additionalData: false,
        sortable: false,
        filterable: false,
        razorExpressions: 0,
        razorBlocks: 0,
        razorPartials: 0
    }
    const src = node => normalize(source.slice(node.start, node.end))

    if (!config || config.type !== 'ObjectExpression') {
        C.add('config-not-object-literal')
        return { features: f, reasonsB: [...B], reasonsC: [...C], classification: 'C' }
    }

    for (const r of razor) {
        if (r.kind === 'expression') f.razorExpressions++
        else if (r.kind === 'partial') { f.razorPartials++; C.add('razor-partial') }
        else if (r.kind === 'block') {
            f.razorBlocks++
            if (r.keyword === 'if') B.add('razor-conditional')
            else C.add(`razor-${r.keyword}`)
            if (r.alternativeMarkup) B.add('razor-else-branch')
        } else if (r.kind === 'code') { f.razorBlocks++; C.add('razor-code-block') }
    }

    for (const p of config.properties) {
        if (p.type !== 'Property') { C.add('config-spread'); continue }
        const name = keyName(p)
        if (!GRID_OPTIONS.has(name)) B.add(`unknown-option:${name}`)
    }

    //data source
    const ds = getProp(config, 'dataSource')
    if (ds && ds.type !== 'ObjectExpression') C.add('dynamic-datasource')
    if (ds && ds.type === 'ObjectExpression') {
        for (const p of ds.properties) {
            const name = keyName(p)
            if (p.type !== 'Property') C.add('datasource-spread')
            else if (!DATASOURCE_OPTIONS.has(name)) B.add(`datasource-option:${name}`)
        }
        const transport = getProp(ds, 'transport')
        if (!transport && getProp(ds, 'data')) B.add('local-data')
        if (transport && transport.type !== 'ObjectExpression') C.add('dynamic-transport')
        if (transport && transport.type === 'ObjectExpression') {
            for (const op of TRANSPORT_OPS) {
                const t = getProp(transport, op)
                if (!t) continue
                f.transports.push(op)
                if (t.type !== 'ObjectExpression') { B.add('transport-function'); continue }
                const url = getProp(t, 'url')
                if (url && staticString(url).value === null) B.add('transport-url-dynamic')
                const data = getProp(t, 'data')
                if (data) {
                    if (data.type === 'Identifier' && data.name !== 'addAntiForgeryToken') f.additionalData = true
                    else if (data.type === 'FunctionExpression' || data.type === 'ArrowFunctionExpression') B.add('transport-data-inline-function')
                }
            }
            if (getProp(transport, 'parameterMap')) { f.parameterMap = true; B.add('parameterMap') }
        }
        const schema = getProp(ds, 'schema')
        if (schema && getProp(schema, 'parse')) B.add('schema-parse')
        if (getProp(ds, 'batch')?.value === true) B.add('batch')
        for (const ev of ['requestStart', 'requestEnd', 'change', 'error']) {
            const handler = getProp(ds, ev)
            if (!handler) continue
            const code = src(handler)
            const standard = (ev === 'error' && STANDARD_ERROR_HANDLER.test(code)) ||
                (ev === 'requestEnd' && STANDARD_REQUEST_END.test(code))
            if (!standard) { f.events.push(`dataSource.${ev}`); B.add(`datasource-event:${ev}`) }
        }
    }

    //editing
    const editable = getProp(config, 'editable')
    if (editable) {
        if (editable.type === 'Literal') {
            f.editMode = editable.value === true ? 'incell' : editable.value === false ? 'none' : String(editable.value)
        } else if (editable.type === 'ObjectExpression') {
            const mode = getProp(editable, 'mode')
            f.editMode = mode?.type === 'Literal' ? String(mode.value) : 'incell'
        } else {
            f.editMode = 'dynamic'
        }
        if (f.editMode !== 'none' && f.editMode !== 'inline') B.add(`edit-mode:${f.editMode}`)
    }

    const toolbar = getProp(config, 'toolbar')
    if (toolbar) {
        const items = toolbar.type === 'ArrayExpression' ? toolbar.elements : [toolbar]
        for (const item of items) {
            const name = item?.type === 'Literal' ? item.value : getProp(item, 'name')?.value
            if (name === 'create' && !getProp(item, 'template')) f.toolbarCreate = true
            else B.add(`toolbar:${name ?? 'template'}`)
        }
    }

    //events
    for (const ev of GRID_EVENTS) {
        if (getProp(config, ev)) { f.events.push(ev); B.add(`event:${ev}`) }
    }
    if (getProp(config, 'detailInit') || getProp(config, 'detailTemplate')) { f.detail = true; B.add('detail') }
    for (const feature of UNSUPPORTED_FEATURES) {
        const v = getProp(config, feature)
        if (v && !(v.type === 'Literal' && v.value === false)) B.add(`unsupported:${feature}`)
    }
    const selectable = getProp(config, 'selectable')
    f.selectable = !!selectable && !(selectable.type === 'Literal' && selectable.value === false)
    const sortable = getProp(config, 'sortable')
    f.sortable = !!sortable && !(sortable.type === 'Literal' && sortable.value === false)
    const filterable = getProp(config, 'filterable')
    f.filterable = !!filterable && !(filterable.type === 'Literal' && filterable.value === false)

    //columns
    const columns = getProp(config, 'columns')
    if (!columns) {
        C.add('no-columns')
    } else if (columns.type !== 'ArrayExpression') {
        C.add('dynamic-columns')
    } else {
        for (const col of columns.elements) {
            if (!col || col.type !== 'ObjectExpression') { C.add('dynamic-column'); continue }
            f.columns++
            for (const p of col.properties) {
                const name = keyName(p)
                if (p.type !== 'Property') C.add('column-spread')
                else if (!COLUMN_OPTIONS.has(name)) B.add(`column-option:${name}`)
            }
            if (getProp(col, 'editor')) { f.editors++; B.add('column-editor') }
            if (getProp(col, 'minScreenWidth')) f.minScreenWidth++
            const format = staticString(getProp(col, 'format')).value
            if (format) f.formats.push(format)
            if (getProp(col, 'encoded')?.value === false) B.add('column-encoded-false')

            for (const which of ['template', 'headerTemplate']) {
                const t = getProp(col, which)
                if (!t) continue
                const s = staticString(t)
                if (s.value === null) { f.templateFunctions++; B.add('template-function'); continue }
                if (s.dynamic) B.add('template-concatenates-code')
                if (/type\s*=\s*\\?["']checkbox|mastercheckbox|checkboxGroups/i.test(s.value)) f.checkbox = true
                const kt = analyzeKendoTemplate(s.value)
                f.templatesEncoded += kt.encoded
                f.templatesRaw += kt.raw
                f.templatesRawNonTrivial += kt.rawNonTrivial.length
                if (!kt.balanced) B.add('template-unbalanced')
                if (kt.rawNonTrivial.length) B.add('raw-template-output')
                if (kt.complexCode.length) B.add('template-code')
            }

            const command = getProp(col, 'command')
            if (command) {
                const items = command.type === 'ArrayExpression' ? command.elements : [command]
                for (const item of items) {
                    const name = item?.type === 'Literal' ? item.value : staticString(getProp(item, 'name')).value
                    f.commands.push(name ?? '?')
                    if (getProp(item, 'click')) B.add('custom-command-click')
                    else if (!STANDARD_COMMANDS.has(name)) B.add(`command:${name ?? 'dynamic'}`)
                }
            }
        }
    }

    const classification = C.size ? 'C' : B.size ? 'B' : 'A'
    return { features: f, reasonsB: [...B], reasonsC: [...C], classification }
}

const GRID_CALL = /\.kendoGrid\s*\(/g
const EXTERNAL_USAGE = /\.data\(\s*["']kendoGrid["']\s*\)(?:\s*\.\s*([A-Za-z_][\w]*(?:\s*\.\s*[A-Za-z_]\w*)*))?/g

function isCommentedOut(text, index) {
    const lineStart = text.lastIndexOf('\n', index) + 1
    const before = text.slice(lineStart, index)
    return /(^|[^:"'\\])\/\//.test(before)
}

function gridName(text, index) {
    const before = text.slice(Math.max(0, index - 200), index)
    const selector = before.match(/\$\(\s*(["'])([^"']+)\1\s*\)\s*$/)
    if (selector) return selector[2]
    if (/appendTo\([^)]*detailCell[^)]*\)\s*$/.test(before)) return '(detail grid)'
    return '(unknown)'
}

/**
 * Counts $(..).data("kendoGrid") calls and the members used on them, either chained
 * (.data("kendoGrid").dataSource.read()) or through a variable
 * (var grid = $(..).data("kendoGrid"); grid.dataSource.page(1)).
 */
export function externalUsages(src) {
    const methods = {}
    const add = name => { methods[name] = (methods[name] ?? 0) + 1 }
    let count = 0
    let m
    EXTERNAL_USAGE.lastIndex = 0
    while ((m = EXTERNAL_USAGE.exec(src))) {
        count++
        if (m[1]) { add(m[1].replace(/\s+/g, '')); continue }
        const lineStart = src.lastIndexOf('\n', m.index) + 1
        const assigned = src.slice(lineStart, m.index).match(/([A-Za-z_$][\w$]*)\s*=\s*[^=;\n]*$/)
        if (!assigned) { add('(instance)'); continue }
        //members used on the variable until the end of the enclosing script block
        const scopeEnd = src.indexOf('</script', m.index)
        const scope = src.slice(m.index + m[0].length, scopeEnd < 0 ? undefined : scopeEnd)
        const member = new RegExp(`(?<![\\w$.])${assigned[1].replace(/\$/g, '\\$')}\\s*\\.\\s*([A-Za-z_]\\w*(?:\\s*\\.\\s*[A-Za-z_]\\w*)*)`, 'g')
        const used = new Set()
        let u
        while ((u = member.exec(scope))) used.add(u[1].replace(/\s+/g, ''))
        if (used.size === 0) add('(instance)')
        for (const name of used) add(name)
    }
    return { count, methods }
}

/** Finds and analyses every kendoGrid initialisation in a .cshtml file. */
export function analyzeCshtml(src) {
    const masked = maskCshtml(src)
    const text = masked.text
    const grids = []
    const found = new Set()

    for (const script of masked.scripts) {
        const region = text.slice(script.outStart, script.outEnd)
        GRID_CALL.lastIndex = 0
        let m
        while ((m = GRID_CALL.exec(region))) {
            const callIndex = script.outStart + m.index
            if (isCommentedOut(text, callIndex)) continue
            const origIndex = masked.toOriginal(callIndex)
            found.add(origIndex)
            const grid = { name: gridName(text, callIndex), line: lineOf(src, origIndex) }
            const argStart = callIndex + m[0].length
            let config
            try {
                config = parseExpressionAt(text, argStart, { ecmaVersion: 'latest' })
            } catch (err) {
                const origPos = masked.toOriginal(err.pos ?? argStart)
                grid.parseError = `${err.message.replace(/\s*\(\d+:\d+\)$/, '')} at line ${lineOf(src, origPos)}`
                const razor = masked.constructs.filter(c => c.outStart >= argStart && c.outStart < script.outEnd)
                grid.analysis = {
                    features: { razorPartials: razor.filter(r => r.kind === 'partial').length },
                    reasonsB: [],
                    reasonsC: ['parse-error', ...(razor.some(r => r.kind === 'partial') ? ['razor-partial'] : [])],
                    classification: 'C'
                }
                grids.push(grid)
                continue
            }
            const razor = masked.constructs.filter(c => c.outStart >= config.start && c.outStart < config.end)
            grid.analysis = analyzeGridConfig(config, text, razor)
            grids.push(grid)
        }
    }

    //grids hidden in an alternative Razor branch (@else) are not in the masked text
    const raw = /\.kendoGrid\s*\(/g
    let r
    for (const script of masked.scripts) {
        raw.lastIndex = script.origStart
        while ((r = raw.exec(src)) && r.index < script.origEnd) {
            if (found.has(r.index) || isCommentedOut(src, r.index)) continue
            const inAlternative = masked.constructs.some(c => c.kind !== 'expression' && r.index > c.origStart && r.index < c.origEnd)
            if (!inAlternative) continue
            grids.push({
                name: gridName(src, r.index),
                line: lineOf(src, r.index),
                analysis: { features: {}, reasonsB: [], reasonsC: ['inside-alternative-razor-branch'], classification: 'C' }
            })
        }
    }

    grids.sort((a, b) => a.line - b.line)
    return { grids, external: externalUsages(src) }
}
