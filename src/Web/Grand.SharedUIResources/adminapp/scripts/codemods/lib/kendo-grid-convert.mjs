/*
 * Codemod stage 2: kendoGrid({ ... }) in Razor views -> <admin-grid> markup.
 *
 * For every grid in a .cshtml file the converter
 *   1. masks Razor and parses the configuration with acorn (razor-mask.mjs),
 *   2. builds a grid model (transport URLs, pager, editing, columns, commands, detail grid),
 *   3. renders <admin-grid> markup in place of the grid's placeholder element
 *      (<div id="name"></div>) in the style of the hand-converted pilot views,
 *   4. removes the kendoGrid statement, a detailInit function it owned, and the
 *      $(document).ready / <script> wrappers left empty.
 *
 * What cannot be translated faithfully is reported: when the grid can still be converted,
 * the markup gets @* CODEMOD-REVIEW: reason *@ comments for a reviewer; when it cannot,
 * the grid is left on Kendo and the reasons are returned. Nothing is guessed silently.
 */
import { parseExpressionAt } from 'acorn'
import { maskCshtml, lineOf, scanBalanced } from './razor-mask.mjs'
import { analyzeGridConfig, staticString } from './kendo-grid-analysis.mjs'
import { convertKendoTemplate, escapeAttribute, HTML_LIKE_FIELD, pathOf } from './kendo-template.mjs'

const NL = String.fromCharCode(10)
export const MARKER = 'CODEMOD-REVIEW'
const PLACEHOLDER = /__RZ\d+__/g
const STANDARD_ERROR_HANDLER = /^function\s*\(\s*(\w+)\s*\)\s*\{\s*display_kendoui_grid_error\(\s*\1\s*\);?\s*(\/\/[^\n]*\s*)?(this\.cancelChanges\(\);?)?\s*\}$/
const RELOAD_REQUEST_END = /^function\s*\(\s*(\w+)\s*\)\s*\{\s*if\s*\(\s*\1\.type\s*===?\s*["'](create|update)["']\s*(\|\|\s*\1\.type\s*===?\s*["'](create|update)["']\s*)?\)\s*\{\s*this\.read\(\);?\s*\}\s*\}$/
const PROGRESS_HANDLER = /^function\s*\(\s*\w*\s*\)\s*\{\s*(kendo\.ui\.progress\([^;]*\);?\s*)?\}$/
const NEW_ROW_EDIT = /^function\s*\(\s*(\w+)\s*\)\s*\{\s*if\s*\(\s*\1\.model\.isNew\(\)\s*\)\s*\{\s*((?:\1\.model\.\w+\s*=\s*(?:""|''|\d+|true|false)\s*;?\s*)+)\}\s*\}$/

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

const literal = node => (node && node.type === 'Literal' ? node.value : undefined)

function camel(text) {
    const words = String(text).replace(/[^A-Za-z0-9]+/g, ' ').trim().split(' ').filter(Boolean)
    if (!words.length) return 'text'
    return words.map((w, i) => (i === 0 ? w[0].toLowerCase() + w.slice(1) : w[0].toUpperCase() + w.slice(1))).join('')
}

function pascal(text) {
    const c = camel(text)
    return c[0].toUpperCase() + c.slice(1)
}

/** Converts the Razor source of a masked construct into attribute/markup Razor. */
export function razorForMarkup(text) {
    const raw = /^@Html\.Raw\(\s*([\s\S]*)\)$/.exec(text)
    if (raw) {
        const inner = raw[1].trim()
        return /^Url\.Action\(/.test(inner) ? `@${inner}` : `@(${inner})`
    }
    return text
}

class GridConversionError extends Error {}

function createContext(masked, file) {
    const byPlaceholder = new Map()
    for (const c of masked.constructs) if (c.placeholder) byPlaceholder.set(c.placeholder, c)
    const restore = text => String(text).replace(PLACEHOLDER, p => {
        const construct = byPlaceholder.get(p)
        return construct ? razorForMarkup(construct.text) : p
    })
    return { masked, file, byPlaceholder, restore }
}

/** Loc resource key of a Razor construct like @Loc["Admin.Common.View"] or @Loc[$"{Scope...}.Common.View"]. */
function locKey(construct) {
    const m = construct && /^@Loc\[\s*\$?"([^"]*)"\s*\]$/.exec(construct.text)
    return m ? m[1] : null
}

function stringValue(node, ctx, what) {
    if (!node) return null
    const s = staticString(node)
    if (s.value === null || s.dynamic) throw new GridConversionError(`${what} is computed in script`)
    return ctx.restore(s.value)
}

function numberValue(node, ctx, what) {
    if (!node) return null
    if (node.type === 'Literal' && typeof node.value === 'number') return node.value
    if (node.type === 'Literal' && typeof node.value === 'string' && /^\d+(px)?$/.test(node.value)) return parseInt(node.value, 10)
    throw new GridConversionError(`${what} is not a number`)
}

function alignOf(attributes) {
    if (!attributes) return { align: null, other: false }
    if (attributes.type !== 'ObjectExpression') return { align: null, other: true }
    let align = null
    let other = false
    for (const p of attributes.properties) {
        const name = keyName(p)
        const value = literal(p.value)
        if (name === 'style' && typeof value === 'string') {
            const m = /^\s*text-align\s*:\s*(left|center|right)\s*;?\s*$/i.exec(value)
            if (m) align = m[1][0].toUpperCase() + m[1].slice(1).toLowerCase()
            else other = true
        } else {
            other = true
        }
    }
    return { align, other }
}

function schemaFields(ds) {
    const model = getProp(getProp(ds, 'schema'), 'model')
    const fields = getProp(model, 'fields')
    const result = new Map()
    if (fields && fields.type === 'ObjectExpression') {
        for (const p of fields.properties) {
            const name = keyName(p)
            if (!name || p.value.type !== 'ObjectExpression') continue
            result.set(name, {
                editable: literal(getProp(p.value, 'editable')),
                type: literal(getProp(p.value, 'type')),
                defaultValue: getProp(p.value, 'defaultValue')
            })
        }
    }
    return { id: literal(getProp(model, 'id')), fields: result, hasFields: !!fields }
}

function commandsOf(column, ctx, grid) {
    const command = getProp(column, 'command')
    const items = command.type === 'ArrayExpression' ? command.elements : [command]
    const commands = grid.commands || { edit: false, destroy: false, custom: [] }
    for (const item of items) {
        const name = item?.type === 'Literal' ? item.value : literal(getProp(item, 'name'))
        if (name === 'edit') commands.edit = true
        else if (name === 'destroy') commands.destroy = true
        else if (getProp(item, 'click')) {
            const click = getProp(item, 'click')
            commands.custom.push({ name, text: stringValue(getProp(item, 'text'), ctx, 'command text'), click: click.type === 'Identifier' ? click.name : null })
            grid.markers.push(`custom command "${name}": its click handler received a Kendo event (this.dataItem(tr)); <grid-command> calls (dataItem, event, grid)`)
        } else {
            throw new GridConversionError(`command "${name}" is not supported`)
        }
    }
    const width = getProp(column, 'width')
    if (width) commands.width = numberValue(width, ctx, 'command width')
    const title = getProp(column, 'title')
    if (title) commands.title = stringValue(title, ctx, 'command title')
    grid.commands = commands
}

function isCheckboxColumn(column) {
    const header = staticString(getProp(column, 'headerTemplate')).value || ''
    const template = staticString(getProp(column, 'template')).value || ''
    return /mastercheckbox/.test(header) && /checkboxGroups/.test(template)
}

function templateTexts(ctx, grid) {
    return placeholder => {
        const construct = ctx.byPlaceholder.get(placeholder)
        const key = locKey(construct)
        if (!key) return null
        const existing = grid.texts.find(t => t.razor === construct.text)
        if (existing) return `{{ $texts.${existing.name} }}`
        const base = camel(key.split('.').pop())
        let name = base
        for (let i = 2; grid.texts.some(t => t.name === name); i++) name = `${base}${i}`
        grid.texts.push({ name, razor: construct.text })
        return `{{ $texts.${name} }}`
    }
}

function columnOf(node, ctx, grid, fields, editing) {
    const column = { markers: [] }
    const field = getProp(node, 'field')
    column.field = field ? stringValue(field, ctx, 'column field') : null
    const title = getProp(node, 'title')
    column.title = title ? stringValue(title, ctx, 'column title') : null
    const width = getProp(node, 'width')
    if (width) column.width = numberValue(width, ctx, 'column width')
    const cell = alignOf(getProp(node, 'attributes'))
    const header = alignOf(getProp(node, 'headerAttributes'))
    if (cell.other || header.other) column.markers.push('column attributes/headerAttributes other than text-align were dropped')
    column.align = cell.align
    if (header.align && header.align !== cell.align) column.headerAlign = header.align
    const format = getProp(node, 'format')
    if (format) column.format = stringValue(format, ctx, 'column format')
    const minScreenWidth = getProp(node, 'minScreenWidth')
    if (minScreenWidth) column.minScreenWidth = numberValue(minScreenWidth, ctx, 'minScreenWidth')
    if (literal(getProp(node, 'hidden')) === true) column.hidden = true
    if (getProp(node, 'headerTemplate')) throw new GridConversionError('column headerTemplate is not supported')
    const encodedFalse = literal(getProp(node, 'encoded')) === false

    const template = getProp(node, 'template')
    if (template) {
        const s = staticString(template)
        if (s.value === null || s.dynamic) throw new GridConversionError(`template of column "${column.field}" is computed in script`)
        const trimmed = s.value.trim()
        const own = column.field && new RegExp(`^#\\s*(:\\s*|=\\s*(kendo\\.htmlEncode\\(\\s*)?)${column.field.replace(/\./g, '\\.')}\\s*\\)?\\s*#$`).exec(trimmed)
        if (own && !(own[1].startsWith('=') && !own[2] && encodedFalse)) {
            //the template only prints the column's own field: a plain column
            if (own[1].startsWith('=') && !own[2] && HTML_LIKE_FIELD.test(column.field)) {
                column.markers.push(`#=${column.field}# was raw HTML in Kendo and is encoded now; use encoded="false" only if the server builds it as markup`)
            }
        } else {
            const converted = convertKendoTemplate(s.value, { restore: ctx.restore, textFor: templateTexts(ctx, grid) })
            if (!converted.ok) throw new GridConversionError(`column "${column.field}": ${converted.markers.join('; ')}`)
            column.template = converted.lines
            column.markers.push(...converted.markers)
            if (/\son[a-z]+\s*=/i.test(converted.lines.join(' '))) {
                column.markers.push('inline on* handler in the template: placeholders are dropped from on* attributes; use data-grid-click="fn" with fn(dataItem, event, grid)')
            }
        }
    } else if (encodedFalse) {
        column.encoded = false
        column.markers.push(`encoded: false renders ${column.field} as HTML; keep only if the server builds it as markup`)
    }

    const editor = getProp(node, 'editor')
    const meta = column.field ? fields.fields.get(column.field) : null
    if (editing && column.field && (!meta || meta.editable !== false) && column.field !== grid.key) {
        const type = meta?.type
        if (type === 'number' || type === 'int') column.editor = /^\{0:0\}$|^n0$/.test(column.format || '') || column.field === 'DisplayOrder' || type === 'int' ? 'Integer' : 'Numeric'
        else if (type === 'boolean') column.editor = 'Checkbox'
        else if (type === 'date') {
            column.editor = 'Date'
            column.markers.push(`${column.field} is a date field; Kendo edited it with a DatePicker - check Date vs DateTime`)
        } else column.editor = 'Text'
        if (meta?.defaultValue) {
            column.defaultValue = String(literal(meta.defaultValue) ?? '')
        } else if (grid.transport.create && column.editor === 'Integer' || grid.transport.create && column.editor === 'Numeric') {
            column.defaultValue = '0'
        }
        if (editor) {
            const code = normalize(ctx.masked.text.slice(editor.start, editor.end))
            const numeric = /kendoNumericTextBox\(\{[^}]*decimals\s*:\s*(\d+)/.exec(code)
            if (numeric && !/kendoDropDownList/.test(code)) {
                column.editor = 'Numeric'
                column.decimals = Number(numeric[1])
            } else {
                column.markers.push(`custom Kendo editor (${editor.type === 'Identifier' ? editor.name : 'inline function'}) needs a Select/Custom editor by hand`)
            }
        }
        if (grid.decimals?.has(column.field)) {
            column.editor = 'Numeric'
            column.decimals = grid.decimals.get(column.field)
        }
    } else if (editor && editing) {
        column.markers.push('column has a Kendo editor but the field is not editable')
    }
    return column
}

function detailOf(ctx, grid, detailInit, scriptText) {
    if (detailInit.type !== 'Identifier') throw new GridConversionError('detailInit is not a named function')
    const name = detailInit.name
    const declaration = new RegExp(`function\\s+${name}\\s*\\(\\s*(\\w+)\\s*\\)\\s*\\{`, 'g')
    const text = ctx.masked.text
    const m = declaration.exec(scriptText.text)
    if (!m) throw new GridConversionError(`detailInit function ${name} was not found in the same script`)
    const fnStart = scriptText.start + m.index
    let fn
    try {
        //parse only the declaration: parseExpressionAt would read on past its closing brace
        const braceEnd = scanBalanced(text, fnStart + m[0].length - 1)
        if (braceEnd < 0) throw new Error('unbalanced')
        const padded = text.slice(0, braceEnd)
        fn = parseExpressionAt(padded, fnStart, { ecmaVersion: 'latest' })
    } catch {
        throw new GridConversionError(`detailInit function ${name} does not parse`)
    }
    const param = m[1]
    const body = fn.body.body
    if (body.length !== 1 || body[0].type !== 'ExpressionStatement') throw new GridConversionError(`detailInit ${name} does more than create a detail grid`)
    const call = body[0].expression
    if (call.type !== 'CallExpression' || call.callee.type !== 'MemberExpression' || call.callee.property.name !== 'kendoGrid') {
        throw new GridConversionError(`detailInit ${name} does more than create a detail grid`)
    }
    const target = normalize(text.slice(call.callee.object.start, call.callee.object.end))
    if (!new RegExp(`^\\$\\(\\s*["']<div\\s*/?>["']\\s*\\)\\.appendTo\\(\\s*${param}\\.detailCell\\s*\\)$`).test(target)) {
        throw new GridConversionError(`detailInit ${name} builds its detail element in a way the codemod does not know`)
    }
    const config = call.arguments[0]
    const ds = getProp(config, 'dataSource')
    const transport = getProp(ds, 'transport')
    const read = getProp(transport, 'read')
    for (const op of ['create', 'update', 'destroy']) if (getProp(transport, op)) throw new GridConversionError(`detail grid has a ${op} transport`)
    for (const option of ['dataBound', 'toolbar', 'detailInit', 'edit', 'change']) if (getProp(config, option)) throw new GridConversionError(`detail grid uses ${option}`)
    const url = getProp(read, 'url')
    //"@Url...?param=" + e.data.Field
    let readUrl = null
    const params = []
    if (url?.type === 'BinaryExpression' && url.operator === '+' && url.left.type === 'Literal' && pathOf(url.right)?.startsWith(`${param}.data.`)) {
        const m2 = /^(.*)\?(\w+)=$/.exec(url.left.value)
        if (!m2) throw new GridConversionError('detail read url does not end with ?param=')
        readUrl = ctx.restore(m2[1])
        params.push({ name: m2[2], field: pathOf(url.right).slice(`${param}.data.`.length) })
    } else {
        throw new GridConversionError('detail read url is not "url?param=" + e.data.Field')
    }
    const detail = { readUrl, params, columns: [], pager: pagerOf(config, ds, ctx, { markers: grid.markers }) }
    const columns = getProp(config, 'columns')
    const noFields = { fields: new Map() }
    detail.blocks = columnBlocks(ctx.masked, columns)
    for (const node of columns.elements) {
        const column = columnOf(node, ctx, grid, noFields, false)
        column.node = node
        detail.columns.push(column)
    }
    return { detail, removeRange: { start: fn.start, end: fn.end }, name }
}

/**
 * The one parameterMap shape the codemod translates: numbers posted with a fixed number of
 * decimals for create/update, which a Numeric editor with decimals="n" does in the request
 * culture.
 *   function (data, operation) { if (operation != "read") { data.Ratio = kendo.toString(data.Ratio, "n8"); return data; } else { return data; } }
 * @returns {Map<string, number>} field -> decimals
 */
export function parameterMapDecimals(code) {
    const m = /^function\s*\(\s*(\w+)\s*,\s*(\w+)\s*\)\s*\{\s*if\s*\(\s*\2\s*!==?\s*["']read["']\s*\)\s*\{\s*((?:\1\.\w+\s*=\s*kendo\.toString\(\s*\1\.\w+\s*,\s*["']n\d+["']\s*\)\s*;?\s*)+)return\s+\1\s*;?\s*\}\s*(?:else\s*\{\s*)?return\s+\1\s*;?\s*\}?\s*\}$/.exec(code)
    if (!m) throw new GridConversionError('parameterMap')
    const decimals = new Map()
    for (const assignment of m[3].matchAll(/\.(\w+)\s*=\s*kendo\.toString\(\s*\w+\.(\w+)\s*,\s*["']n(\d+)["']/g)) {
        if (assignment[1] !== assignment[2]) throw new GridConversionError('parameterMap')
        decimals.set(assignment[1], Number(assignment[3]))
    }
    return decimals
}

function pagerOf(config, ds, ctx, grid) {
    const pageable = getProp(config, 'pageable')
    const pageSize = getProp(ds, 'pageSize')
    const result = { pager: 'None', pageSize: null, pageSizes: null }
    if (!pageable || literal(pageable) === false) {
        if (pageSize) grid.markers.push('Kendo grid paged without a pager')
        return result
    }
    const numeric = literal(getProp(pageable, 'numeric'))
    const compact = pageable.type === 'ObjectExpression' && numeric === false && literal(getProp(pageable, 'previousNext')) === false
    if (compact) result.pager = 'Compact'
    else result.pager = pageSize ? 'Full' : 'Compact'
    if (!compact && !pageSize) grid.markers.push('Kendo numeric pager without pageSize (one page): converted to the compact pager')
    if (pageSize) {
        const construct = pageSize.type === 'Identifier' ? ctx.byPlaceholder.get(pageSize.name) : null
        if (construct && /DefaultGridPageSize/.test(construct.text)) result.pageSize = null
        else if (pageSize.type === 'Literal') result.pageSize = pageSize.value
        else result.pageSize = ctx.restore(normalize(ctx.masked.text.slice(pageSize.start, pageSize.end)))
        if (compact) grid.markers.push('compact pager with a page size')
    }
    const sizes = getProp(pageable, 'pageSizes')
    if (sizes && sizes.type === 'ArrayExpression') {
        const only = sizes.elements.length === 1 && sizes.elements[0].type === 'Identifier' ? ctx.byPlaceholder.get(sizes.elements[0].name) : null
        if (!(only && /GridPageSizes/.test(only.text))) {
            result.pageSizes = sizes.elements.map(e => (e.type === 'Literal' ? e.value : ctx.restore(e.name))).join(',')
        }
    } else if (result.pager === 'Full') {
        //a Kendo pager without pageSizes has no page size list; page-sizes="" turns off the default one
        result.pageSizes = ''
    }
    return result
}

/** True when the script declares `function name(` (a global the adapter can call by name). */
export function declaresFunction(text, name) {
    return new RegExp(`(^|[^\\w$.])function\\s+${name.replace(/\$/g, '\\$')}\\s*\\(`).test(text)
}

function eventHandler(node, ctx, name, grid, extracted, gridId) {
    if (node.type === 'Identifier') {
        if (!declaresFunction(ctx.masked.text, node.name)) throw new GridConversionError(`${name} handler ${node.name} is not a function declared in the view`)
        return node.name
    }
    if (node.type === 'FunctionExpression') {
        const fnName = `on${pascal(gridId)}${name[0].toUpperCase()}${name.slice(1)}`
        const params = node.params.map(p => ctx.masked.text.slice(p.start, p.end)).join(', ')
        extracted.push({ name: fnName, params, body: { start: node.body.start, end: node.body.end } })
        return fnName
    }
    throw new GridConversionError(`${name} handler is not a function`)
}

/** Builds the grid model from a parsed configuration. Throws GridConversionError. */
export function buildGridModel(config, ctx, { id, scriptText }) {
    const grid = {
        id,
        key: 'Id',
        transport: {},
        markers: [],
        texts: [],
        columns: [],
        events: {},
        extracted: [],
        remove: []
    }
    const ds = getProp(config, 'dataSource')
    if (!ds || ds.type !== 'ObjectExpression') throw new GridConversionError('dataSource is not an object literal')
    if (literal(getProp(ds, 'batch')) === true) throw new GridConversionError('batch editing')
    const transport = getProp(ds, 'transport')
    if (!transport) throw new GridConversionError(getProp(ds, 'data') ? 'local data' : 'no transport')
    const parameterMap = getProp(transport, 'parameterMap')
    if (parameterMap) grid.decimals = parameterMapDecimals(normalize(ctx.masked.text.slice(parameterMap.start, parameterMap.end)))
    for (const op of ['read', 'create', 'update', 'destroy']) {
        const t = getProp(transport, op)
        if (!t) continue
        if (t.type !== 'ObjectExpression') throw new GridConversionError(`${op} transport is a function`)
        grid.transport[op] = stringValue(getProp(t, 'url'), ctx, `${op} url`)
        const data = getProp(t, 'data')
        if (data && !(data.type === 'Identifier' && data.name === 'addAntiForgeryToken')) {
            //additional-data names a global function; a variable holding an object is not one
            if (op === 'read' && data.type === 'Identifier' && declaresFunction(ctx.masked.text, data.name)) grid.additionalData = data.name
            else throw new GridConversionError(`${op} transport data is not addAntiForgeryToken`)
        }
    }
    if (!grid.transport.read) throw new GridConversionError('no read transport')
    const fields = schemaFields(ds)
    if (fields.id && fields.id !== 'Id') grid.key = fields.id

    for (const event of ['requestStart', 'requestEnd', 'error', 'change']) {
        const handler = getProp(ds, event)
        if (!handler) continue
        const code = normalize(ctx.masked.text.slice(handler.start, handler.end))
        if (event === 'error' && STANDARD_ERROR_HANDLER.test(code)) continue
        if ((event === 'requestStart' || event === 'requestEnd') && PROGRESS_HANDLER.test(code)) continue
        if (event === 'requestEnd' && RELOAD_REQUEST_END.test(code)) {
            grid.reloadOnUpdate = true
            continue
        }
        throw new GridConversionError(`dataSource ${event} handler`)
    }

    Object.assign(grid, pagerOf(config, ds, ctx, grid))

    //editing
    const editable = getProp(config, 'editable')
    let mode = 'none'
    let confirmation = false
    if (editable) {
        if (editable.type === 'Literal') {
            mode = editable.value === true ? 'incell' : editable.value === false ? 'none' : String(editable.value)
            confirmation = editable.value !== false
        } else if (editable.type === 'ObjectExpression') {
            mode = literal(getProp(editable, 'mode')) ?? 'incell'
            const c = literal(getProp(editable, 'confirmation'))
            confirmation = c !== false && c != null
        }
    }
    if (mode === 'incell' || mode === 'popup') throw new GridConversionError(`${mode} editing`)

    const toolbar = getProp(config, 'toolbar')
    if (toolbar) {
        const items = toolbar.type === 'ArrayExpression' ? toolbar.elements : [toolbar]
        for (const item of items) {
            const name = item?.type === 'Literal' ? item.value : literal(getProp(item, 'name'))
            if (name === 'create' && !getProp(item, 'template')) {
                const text = item.type === 'ObjectExpression' ? stringValue(getProp(item, 'text'), ctx, 'toolbar text') : null
                grid.toolbarCreate = { text: text && text !== '@Loc["Admin.Common.AddNewRecord"]' ? text : null }
            } else {
                throw new GridConversionError(`toolbar ${name ?? 'template'}`)
            }
        }
    }

    const autoBind = getProp(config, 'autoBind')
    if (literal(autoBind) === false) grid.autoBind = false
    const selectable = getProp(config, 'selectable')
    if (selectable && literal(selectable) !== false) throw new GridConversionError('selectable rows')
    for (const unsupported of ['groupable', 'columnMenu', 'reorderable', 'resizable', 'detailTemplate', 'save', 'remove', 'cancel', 'dataBinding', 'saveChanges', 'height']) {
        const value = getProp(config, unsupported)
        if (value && literal(value) !== false) throw new GridConversionError(`${unsupported} option`)
    }

    const columns = getProp(config, 'columns')
    if (!columns || columns.type !== 'ArrayExpression') throw new GridConversionError('columns are not an array literal')

    //the command column decides whether the grid edits inline
    for (const node of columns.elements) {
        if (node?.type === 'ObjectExpression' && getProp(node, 'command')) commandsOf(node, ctx, grid)
    }
    const editing = mode === 'inline' && (grid.commands?.edit || !!grid.toolbarCreate)
    if (editing) grid.editMode = 'Inline'
    if (grid.commands?.destroy) {
        if (mode === 'none') grid.markers.push('Delete without editable: Kendo removed the row locally without calling the destroy URL')
        if (confirmation) grid.confirmDestroy = true
    }
    if ((grid.commands?.edit || grid.toolbarCreate) && mode !== 'inline') throw new GridConversionError('edit command without inline editing')
    if (grid.transport.update && grid.commands?.edit && !grid.reloadOnUpdate) grid.reloadAfterSave = false

    const edit = getProp(config, 'edit')
    const newRowDefaults = {}
    if (edit) {
        const m = NEW_ROW_EDIT.exec(normalize(ctx.masked.text.slice(edit.start, edit.end)))
        if (!m) throw new GridConversionError('edit event handler')
        for (const assignment of m[2].matchAll(/\.model\.(\w+)\s*=\s*(""|''|\d+|true|false)/g)) {
            newRowDefaults[assignment[1]] = assignment[2].replace(/^["']{2}$/, '')
        }
    }

    for (const node of columns.elements) {
        if (!node || node.type !== 'ObjectExpression') throw new GridConversionError('a column is not an object literal')
        if (getProp(node, 'command')) continue
        if (isCheckboxColumn(node)) {
            grid.selectable = 'Checkbox'
            //a named checkbox (SelectedProductIds) was posted with the popup form
            const named = /name\s*=\s*['"]([\w.]+)['"]/.exec(staticString(getProp(node, 'template')).value || '')
            if (named) grid.selectionName = named[1]
            grid.markers.push('checkbox column: rewire the #mastercheckbox/checkboxGroups script to on-change (e.selectedIds) and clearSelection()')
            continue
        }
        const column = columnOf(node, ctx, grid, fields, editing)
        if (column.field && Object.prototype.hasOwnProperty.call(newRowDefaults, column.field)) column.defaultValue = newRowDefaults[column.field]
        column.node = node
        grid.columns.push(column)
    }
    for (const field of Object.keys(newRowDefaults)) {
        if (!grid.columns.some(c => c.field === field)) grid.markers.push(`edit handler set ${field} on new rows, but no column edits ${field}`)
    }

    const detailInit = getProp(config, 'detailInit')
    if (detailInit) {
        const detail = detailOf(ctx, grid, detailInit, scriptText)
        grid.detail = detail.detail
        grid.remove.push(detail.removeRange)
    }

    for (const event of ['dataBound', 'change']) {
        const handler = getProp(config, event)
        if (!handler) continue
        grid.events[event] = eventHandler(handler, ctx, event, grid, grid.extracted, id)
        grid.markers.push(`${event} handler ${grid.events[event]}: check it does not rely on Kendo markup (k-* classes, tbody, data-uid)`)
    }
    return grid
}

function attr(name, value) {
    //Razor expressions (@Loc["..."], @Url.Action("...")) keep their quotes; Razor parses them
    const text = String(value)
    return `${name}="${text.includes('@') ? text : escapeAttribute(text)}"`
}

function columnLines(column, indent) {
    const lines = []
    const pad = ' '.repeat(indent)
    for (const marker of column.markers) lines.push(`${pad}@* ${MARKER}: ${marker} *@`)
    const attrs = []
    if (column.field) attrs.push(attr('field', column.field))
    if (column.title != null) attrs.push(attr('title', column.title))
    if (column.width != null) attrs.push(attr('width', column.width))
    if (column.align) attrs.push(attr('align', column.align))
    if (column.headerAlign) attrs.push(attr('header-align', column.headerAlign))
    if (column.format) attrs.push(attr('format', column.format))
    if (column.minScreenWidth != null) attrs.push(attr('min-screen-width', column.minScreenWidth))
    if (column.hidden) attrs.push('hidden="true"')
    if (column.editor) attrs.push(attr('editor', column.editor))
    if (column.decimals != null) attrs.push(attr('decimals', column.decimals))
    if (column.defaultValue != null) attrs.push(attr('default-value', column.defaultValue))
    if (column.encoded === false) attrs.push('encoded="false"')
    if (column.template) {
        lines.push(`${pad}<grid-column ${attrs.join(' ')}>`)
        lines.push(`${pad}    <cell-template>`)
        for (const line of column.template) lines.push(`${pad}        ${line}`)
        lines.push(`${pad}    </cell-template>`)
        lines.push(`${pad}</grid-column>`)
    } else {
        lines.push(`${pad}<grid-column ${attrs.join(' ')}/>`)
    }
    return lines
}

/** Column markup; columns inside a Razor @if block of the Kendo columns array are wrapped in it again. */
function renderColumns(columns, blocks, indent) {
    const pad = ' '.repeat(indent)
    const lines = []
    let openBlock = null
    const closeBlock = () => {
        if (!openBlock) return
        lines.push(`${pad}}`)
        openBlock = null
    }
    for (const column of columns) {
        const block = blocks.find(b => column.node && column.node.start >= b.outStart && column.node.end <= b.outEnd) || null
        if (block !== openBlock) {
            closeBlock()
            if (block) {
                lines.push(`${pad}${block.header}`)
                lines.push(`${pad}{`)
                openBlock = block
            }
        }
        lines.push(...columnLines(column, openBlock ? indent + 4 : indent))
    }
    closeBlock()
    return lines
}

/** Renders <admin-grid> markup; `indent` is the column of the replaced placeholder. */
export function renderAdminGrid(grid, indent, blocks = []) {
    const pad = ' '.repeat(indent)
    const attrs = [attr('id', grid.id)]
    if (grid.key !== 'Id') attrs.push(attr('key', grid.key))
    for (const op of ['read', 'create', 'update', 'destroy']) if (grid.transport[op]) attrs.push(attr(`${op}-url`, grid.transport[op]))
    if (grid.additionalData) attrs.push(attr('additional-data', grid.additionalData))
    if (grid.pager !== 'Full') attrs.push(attr('pager', grid.pager))
    if (grid.pageSize != null) attrs.push(attr('page-size', grid.pageSize))
    if (grid.pageSizes != null) attrs.push(attr('page-sizes', grid.pageSizes))
    if (grid.autoBind === false) attrs.push('auto-bind="false"')
    if (grid.editMode) attrs.push(attr('edit-mode', grid.editMode))
    if (grid.reloadAfterSave === false) attrs.push('reload-after-save="false"')
    if (grid.confirmDestroy) attrs.push('confirm-destroy="true"')
    if (grid.selectable) attrs.push(attr('selectable', grid.selectable))
    if (grid.selectionName) attrs.push(attr('selection-name', grid.selectionName))
    if (grid.events.dataBound) attrs.push(attr('on-data-bound', grid.events.dataBound))
    if (grid.events.change) attrs.push(attr('on-change', grid.events.change))

    const lines = []
    if (attrs.length <= 2) {
        lines.push(`${pad}<admin-grid ${attrs.join(' ')}>`)
    } else {
        const align = ' '.repeat(indent + '<admin-grid '.length)
        lines.push(`${pad}<admin-grid ${attrs[0]}`)
        attrs.slice(1).forEach((a, i) => lines.push(`${align}${a}${i === attrs.length - 2 ? '>' : ''}`))
    }
    const inner = indent + 4
    const innerPad = ' '.repeat(inner)
    for (const marker of grid.markers) lines.push(`${innerPad}@* ${MARKER}: ${marker} *@`)
    if (grid.toolbarCreate) lines.push(`${innerPad}<grid-toolbar-create${grid.toolbarCreate.text ? ' ' + attr('text', grid.toolbarCreate.text) : ''}/>`)
    for (const text of grid.texts) lines.push(`${innerPad}<grid-text ${attr('name', text.name)} ${attr('value', text.razor)}/>`)

    lines.push(...renderColumns(grid.columns, blocks, inner))
    if (grid.detail) {
        const d = grid.detail
        const dAttrs = [attr('read-url', d.readUrl), attr('param', d.params.map(p => `${p.name}:${p.field}`).join(','))]
        if (d.pager.pager !== 'Compact') dAttrs.push(attr('pager', d.pager.pager))
        lines.push(`${innerPad}<grid-detail ${dAttrs.join(' ')}>`)
        lines.push(...renderColumns(d.columns, d.blocks || [], inner + 4))
        lines.push(`${innerPad}</grid-detail>`)
    }
    if (grid.commands && (grid.commands.edit || grid.commands.destroy || grid.commands.custom.length)) {
        const cAttrs = []
        if (grid.commands.edit) cAttrs.push('edit="true"')
        if (grid.commands.destroy) cAttrs.push('destroy="true"')
        if (grid.commands.title) cAttrs.push(attr('title', grid.commands.title))
        if (grid.commands.width != null) cAttrs.push(attr('width', grid.commands.width))
        if (grid.commands.custom.length) {
            lines.push(`${innerPad}<grid-commands ${cAttrs.join(' ')}>`)
            for (const c of grid.commands.custom) {
                lines.push(`${innerPad}    <grid-command ${[attr('name', c.name), c.text ? attr('text', c.text) : null, c.click ? attr('click', c.click) : null].filter(Boolean).join(' ')}/>`)
            }
            lines.push(`${innerPad}</grid-commands>`)
        } else {
            lines.push(`${innerPad}<grid-commands ${cAttrs.join(' ')}/>`)
        }
    }
    lines.push(`${pad}</admin-grid>`)
    return lines.join('\n')
}

/** Razor @if blocks inside the columns array, with the C# header to re-emit. */
function columnBlocks(masked, columnsNode) {
    const blocks = []
    for (const c of masked.constructs) {
        if (c.outStart < columnsNode.start || c.outStart >= columnsNode.end) continue
        if (c.kind === 'block') {
            if (c.keyword !== 'if') throw new GridConversionError(`@${c.keyword} inside columns`)
            if (c.branches > 1) throw new GridConversionError('@if/else inside columns')
            const header = c.text.slice(0, c.text.indexOf('{')).trim()
            blocks.push({ ...c, header })
        } else if (c.kind === 'partial') {
            throw new GridConversionError('a partial view injects column configuration')
        } else if (c.kind === 'code') {
            throw new GridConversionError('a Razor code block inside the configuration')
        }
    }
    return blocks
}

function lineRange(src, start, end) {
    //expands [start, end) to whole lines when only whitespace surrounds it
    const lineStart = src.lastIndexOf('\n', start - 1) + 1
    let lineEnd = src.indexOf('\n', end)
    if (lineEnd < 0) lineEnd = src.length
    const before = src.slice(lineStart, start)
    const after = src.slice(end, lineEnd)
    if (/^\s*$/.test(before) && /^\s*;?\s*$/.test(after)) return { start: lineStart, end: Math.min(src.length, lineEnd + 1) }
    return { start, end }
}

/** Finds the kendoGrid statement: $("#id").kendoGrid({...}) [.data("kendoGrid")] ; */
function gridStatement(text, callIndex) {
    const before = text.slice(Math.max(0, callIndex - 200), callIndex)
    const m = /(?:(?:var|let|const)\s+[\w$]+\s*=\s*)?\$\(\s*(["'])#([\w-]+)\1\s*\)\s*$/.exec(before)
    if (!m) return null
    const start = callIndex - m[0].length
    const exprStart = start + m[0].indexOf('$(')
    let node
    try {
        node = parseExpressionAt(text, exprStart, { ecmaVersion: 'latest' })
    } catch {
        return null
    }
    let end = node.end
    const semicolon = /^\s*;/.exec(text.slice(end))
    if (semicolon) end += semicolon[0].length
    return { id: m[2], start, end, node, assigned: /^(var|let|const)\s/.test(m[0]) ? /^(?:var|let|const)\s+([\w$]+)/.exec(m[0])[1] : null }
}

function findConfig(node) {
    //$("#x").kendoGrid({...}) or $("#x").kendoGrid({...}).data("kendoGrid")
    let call = node
    if (call.type === 'CallExpression' && call.callee.type === 'MemberExpression' && call.callee.property.name === 'data') call = call.callee.object
    if (call.type !== 'CallExpression' || call.callee.type !== 'MemberExpression' || call.callee.property.name !== 'kendoGrid') return null
    return call.arguments[0]
}

function isCommentedOut(text, index) {
    const lineStart = text.lastIndexOf('\n', index) + 1
    return /(^|[^:"'\\])\/\//.test(text.slice(lineStart, index))
}

/**
 * Converts every kendoGrid in a .cshtml source.
 * @returns {{ output: string, grids: object[] }}
 */
export function convertCshtml(src, { file = '' } = {}) {
    const bom = src.startsWith('\uFEFF') ? '\uFEFF' : ''
    const source = bom ? src.slice(1) : src
    const masked = maskCshtml(source)
    const ctx = createContext(masked, file)
    const text = masked.text
    const found = []
    for (const script of masked.scripts) {
        const region = text.slice(script.outStart, script.outEnd)
        const re = /\.kendoGrid\s*\(/g
        let m
        while ((m = re.exec(region))) {
            const callIndex = script.outStart + m.index
            if (isCommentedOut(text, callIndex)) continue
            found.push({ callIndex, script })
        }
    }

    const results = []
    const edits = []
    const detailCalls = new Set()
    const ids = new Map()
    for (const f of found) {
        const statement = gridStatement(text, f.callIndex)
        if (statement) ids.set(statement.id, (ids.get(statement.id) || 0) + 1)
    }

    for (const f of found) {
        if (detailCalls.has(f.callIndex)) continue
        const line = lineOf(source, masked.toOriginal(f.callIndex))
        const statement = gridStatement(text, f.callIndex)
        const result = { file, line, grid: statement ? `#${statement.id}` : '(detail grid)', action: 'skipped', reasons: [], markers: [] }
        let config = null
        if (statement) config = findConfig(statement.node)
        let analysis = null
        try {
            const node = config || parseExpressionAt(text, f.callIndex + text.slice(f.callIndex).indexOf('(') + 1, { ecmaVersion: 'latest' })
            const razor = masked.constructs.filter(c => c.outStart >= node.start && c.outStart < node.end)
            analysis = analyzeGridConfig(node, text, razor)
            result.class = analysis.classification
        } catch (error) {
            result.class = 'C'
            result.reasons.push(`does not parse: ${error.message}`)
        }
        results.push(result)
        if (!statement) {
            if (!result.reasons.length) result.reasons.push('not a $("#id").kendoGrid(...) statement (detail grids are converted with their master grid)')
            result.pendingDetail = f.callIndex
            continue
        }
        if (!config) {
            result.reasons.push('configuration is not an object literal')
            continue
        }
        if (ids.get(statement.id) > 1) {
            result.reasons.push(`#${statement.id} is configured more than once in this file (Razor branches)`)
            continue
        }
        try {
            const scriptText = { start: f.script.outStart, text: text.slice(f.script.outStart, f.script.outEnd) }
            const grid = buildGridModel(config, ctx, { id: statement.id, scriptText })
            if (statement.assigned) {
                const rest = text.slice(f.script.outStart, f.script.outEnd).replace(text.slice(statement.start, statement.end), '')
                if (new RegExp(`\\b${statement.assigned.replace('$', '\\$')}\\b`).test(rest)) {
                    throw new GridConversionError(`the grid is kept in variable ${statement.assigned}, which the script uses`)
                }
            }
            const blocks = columnBlocks(masked, getProp(config, 'columns'))
            //placeholder element outside scripts
            const placeholder = new RegExp(`^([ \\t]*)<div\\s+id=(["'])${statement.id}\\2\\s*(?:/>|>\\s*</div>)[ \\t]*\\r?\\n?`, 'gm')
            const scriptRanges = masked.scripts.map(s => [s.origStart, s.origEnd])
            const matches = [...source.matchAll(placeholder)].filter(p => !scriptRanges.some(([a, b]) => p.index >= a && p.index < b))
            if (matches.length !== 1) throw new GridConversionError(matches.length ? `placeholder <div id="${statement.id}"> appears ${matches.length} times` : `placeholder <div id="${statement.id}"></div> was not found`)
            const indent = matches[0][1].replace(/\t/g, '    ').length
            const markup = renderAdminGrid(grid, indent, blocks)
            const newline = /\r\n/.test(matches[0][0]) ? '\r\n' : '\n'
            edits.push({ start: matches[0].index, end: matches[0].index + matches[0][0].length, text: markup.replace(/\n/g, newline) + (matches[0][0].endsWith('\n') ? newline : '') })

            const stmtStart = masked.toOriginal(statement.start)
            const stmtEnd = masked.toOriginal(statement.end - 1) + 1
            const extractedCode = grid.extracted.map(e => {
                const bodyStart = masked.toOriginal(e.body.start)
                const bodyEnd = masked.toOriginal(e.body.end - 1) + 1
                return `function ${e.name}(${e.params}) ${source.slice(bodyStart, bodyEnd)}`
            })
            edits.push({ ...lineRange(source, stmtStart, stmtEnd), text: '', appendToScript: extractedCode, script: f.script })
            for (const range of grid.remove) {
                const start = masked.toOriginal(range.start)
                const end = masked.toOriginal(range.end - 1) + 1
                edits.push({ ...lineRange(source, start, end), text: '' })
                for (const other of found) if (other.callIndex > range.start && other.callIndex < range.end) detailCalls.add(other.callIndex)
            }
            result.action = grid.markers.length || grid.columns.some(c => c.markers.length) || grid.detail?.columns.some(c => c.markers.length) ? 'review' : 'converted'
            result.markers = [...grid.markers, ...grid.columns.flatMap(c => c.markers), ...(grid.detail?.columns.flatMap(c => c.markers) || [])]
            result.reasons = []
        } catch (error) {
            if (!(error instanceof GridConversionError)) throw error
            result.reasons.push(error.message)
        }
    }
    //detail grids converted with their master grid are not separate results
    const filtered = results.filter(r => !(r.pendingDetail !== undefined && detailCalls.has(r.pendingDetail)))
    for (const r of filtered) delete r.pendingDetail

    let output = applyEdits(source, edits)
    output = cleanupScripts(output)
    if (edits.length && !/adminAreaSettings\./.test(output.replace(/@inject\s+AdminAreaSettings\s+adminAreaSettings[^\n]*\n?/, ''))) {
        output = output.replace(/^@inject\s+AdminAreaSettings\s+adminAreaSettings[ \t]*\r?\n/m, '')
    }
    return { output: bom + output, grids: filtered }
}

function applyEdits(source, edits) {
    const sorted = [...edits].sort((a, b) => b.start - a.start)
    let out = source
    const appendByScript = new Map()
    for (const edit of edits) {
        if (edit.appendToScript?.length) appendByScript.set(edit.script.origEnd, [...(appendByScript.get(edit.script.origEnd) || []), ...edit.appendToScript])
    }
    //insert extracted handlers at the end of their script, on the line of </script>, indented
    //like the first line of the script
    const inserts = [...appendByScript.entries()].map(([pos, fns]) => {
        const lineStart = source.lastIndexOf(NL, pos - 1) + 1
        const at = /^[ \t]*$/.test(source.slice(lineStart, pos)) ? lineStart : pos
        const scriptStart = source.lastIndexOf('<script', pos)
        const firstLine = /\n([ \t]*)\S/.exec(source.slice(scriptStart, pos))
        const pad = firstLine ? firstLine[1] : '    '
        const eol = source.includes('\r\n') ? '\r\n' : NL
        return { start: at, end: at, text: fns.map(fn => reindent(fn, pad).split(NL).join(eol) + eol).join('') }
    })
    for (const edit of [...sorted, ...inserts].sort((a, b) => b.start - a.start || (b.text ? 1 : -1))) {
        out = out.slice(0, edit.start) + edit.text + out.slice(edit.end)
    }
    return out
}

/** Re-indents an extracted function so its body sits one level inside `pad`. */
function reindent(code, pad) {
    const lines = code.split(/\r?\n/)
    if (lines.length === 1) return pad + code
    const body = lines.slice(1, -1)
    const indents = body.filter(l => l.trim()).map(l => /^\s*/.exec(l)[0].length)
    const min = indents.length ? Math.min(...indents) : 0
    return [
        pad + lines[0].trim(),
        ...body.map(l => (l.trim() ? pad + '    ' + l.slice(min) : '')),
        pad + lines[lines.length - 1].trim()
    ].join('\n')
}

/** Removes $(document).ready(function () { }); and <script></script> left empty. */
export function cleanupScripts(src) {
    let out = src
    let previous
    do {
        previous = out
        out = out.replace(/^[ \t]*\$\(\s*document\s*\)\.ready\(\s*function\s*\(\s*\)\s*\{\s*\}\s*\)\s*;?[ \t]*\r?\n/gm, '')
        out = out.replace(/^[ \t]*<script>\s*<\/script>[ \t]*\r?\n?/gm, '')
    } while (out !== previous)
    return out
}
