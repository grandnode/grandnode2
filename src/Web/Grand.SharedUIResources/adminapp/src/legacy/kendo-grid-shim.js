import { compileKendoTemplate } from './kendo-template.js'

//$.fn.kendoGrid shim: maps the subset of the Kendo grid configuration the panels and
//plugins use onto GrandGrid, so a plugin view that still calls $('#x').kendoGrid({...})
//keeps working once kendo.grid.js is removed. It registers only when $.fn.kendoGrid does
//not exist and warns about every option it does not support.
//
//Supported: dataSource.transport.read/create/update/destroy (url string or function,
//data object or function), parameterMap, schema {data, total, errors, model {id, fields}},
//pageSize, requestStart/requestEnd/error/change; pageable, editable {mode, confirmation},
//toolbar create, autoBind, scrollable/sortable/filterable (accepted and ignored: the server does not sort);
//columns field/title/width/template/headerTemplate/format/attributes/headerAttributes/
//minScreenWidth/hidden/encoded/editor(container, options)/command; events dataBound,
//edit, save, change, detailInit.

const supportedGridOptions = new Set(['dataSource', 'columns', 'pageable', 'editable', 'toolbar', 'autoBind', 'scrollable', 'sortable', 'filterable', 'dataBound', 'edit', 'save', 'change', 'detailInit', 'navigatable', 'resizable', 'height'])
const supportedDataSourceOptions = new Set(['transport', 'schema', 'pageSize', 'serverPaging', 'serverFiltering', 'serverSorting', 'requestStart', 'requestEnd', 'error', 'change', 'batch', 'page'])
const supportedColumnOptions = new Set(['field', 'title', 'width', 'template', 'headerTemplate', 'format', 'attributes', 'headerAttributes', 'minScreenWidth', 'hidden', 'encoded', 'editor', 'command', 'filterable', 'sortable', 'type'])

function warn(what) {
    console.warn(`[GrandGrid shim] unsupported: ${what}`)
}

function checkOptions(object, allowed, prefix) {
    for (const name of Object.keys(object || {})) {
        if (!allowed.has(name)) warn(prefix + name)
    }
}

function alignFromAttributes(attributes) {
    const style = attributes?.style || ''
    const match = /text-align\s*:\s*(left|center|right)/i.exec(style)
    return match ? match[1].toLowerCase() : undefined
}

function toWidth(width) {
    if (typeof width === 'number') return width
    const parsed = parseInt(width, 10)
    return Number.isFinite(parsed) ? parsed : undefined
}

function transportUrl(entry) {
    if (!entry) return undefined
    if (typeof entry === 'string') return entry
    return entry.url
}

function transportData(entry) {
    if (!entry || typeof entry === 'string' || entry.data == null) return null
    return typeof entry.data === 'function' ? entry.data : () => entry.data
}

function editorForField(fieldType) {
    switch (fieldType) {
        case 'number': return 'Numeric'
        case 'boolean': return 'Checkbox'
        case 'date': return 'Date'
        default: return 'Text'
    }
}

function commandsFrom(command, kendoApi) {
    const list = Array.isArray(command) ? command : [command]
    const result = { edit: false, destroy: false, custom: [], texts: {} }
    for (const entry of list) {
        const name = typeof entry === 'string' ? entry : entry.name
        if (name === 'edit') {
            result.edit = true
            if (entry.text && typeof entry.text === 'object') Object.assign(result.texts, entry.text)
            else if (typeof entry.text === 'string') result.texts.edit = entry.text
        } else if (name === 'destroy') {
            result.destroy = true
            if (typeof entry.text === 'string') result.texts.delete = entry.text
        } else if (entry && typeof entry.click === 'function') {
            const click = entry.click
            result.custom.push({
                name,
                text: typeof entry.text === 'string' ? entry.text : name,
                className: entry.className,
                //Kendo calls click with the event and the grid as this; views then call
                //this.dataItem($(e.currentTarget).closest("tr"))
                click: (item, event) => click.call(kendoApi(), event)
            })
        } else {
            warn(`columns.command "${name}"`)
        }
    }
    return result
}

/**
 * Converts a Kendo grid configuration to a GrandGrid configuration.
 * @param {object} options kendoGrid options
 * @param {{kendoApi: () => object, $?: Function}} context
 */
export function mapKendoGridOptions(options, context) {
    checkOptions(options, supportedGridOptions, '')
    const dataSourceOptions = options.dataSource || {}
    if (Array.isArray(dataSourceOptions) || Array.isArray(dataSourceOptions.data)) warn('local dataSource data')
    checkOptions(dataSourceOptions, new Set([...supportedDataSourceOptions, 'data']), 'dataSource.')
    if (dataSourceOptions.batch) warn('dataSource.batch')
    const transport = dataSourceOptions.transport || {}
    const schema = dataSourceOptions.schema || {}
    const model = schema.model || {}
    const fields = model.fields || {}

    const editable = options.editable
    let editMode = 'None'
    let confirmDestroy = false
    if (editable === true || editable === 'inline') {
        editMode = 'Inline'
        confirmDestroy = true
    } else if (editable && typeof editable === 'object') {
        if (editable.mode && editable.mode !== 'inline') warn(`editable.mode "${editable.mode}"`)
        editMode = 'Inline'
        confirmDestroy = editable.confirmation !== false
        if (typeof editable.confirmation === 'string') context.texts.deleteConfirmation = editable.confirmation
    } else if (typeof editable === 'string') {
        warn(`editable "${editable}"`)
    }

    const pageable = options.pageable
    let pager = 'None'
    let pageSizes = null
    if (pageable === true) {
        pager = 'Full'
    } else if (pageable && typeof pageable === 'object') {
        pager = pageable.numeric === false && pageable.previousNext === false ? 'Compact' : 'Full'
        if (Array.isArray(pageable.pageSizes)) pageSizes = pageable.pageSizes
    }

    const columns = []
    let commands = null
    for (const column of options.columns || []) {
        checkOptions(column, supportedColumnOptions, 'columns.')
        if (column.command) {
            commands = commandsFrom(column.command, context.kendoApi)
            commands.width = toWidth(column.width)
            commands.title = column.title
            Object.assign(context.texts, commands.texts)
            continue
        }
        const field = column.field
        const fieldDefinition = field ? fields[field] || {} : {}
        const mapped = {
            field,
            title: column.title,
            width: toWidth(column.width),
            format: column.format,
            align: alignFromAttributes(column.attributes),
            headerAlign: alignFromAttributes(column.headerAttributes),
            minScreenWidth: column.minScreenWidth,
            hidden: column.hidden === true,
            encoded: column.encoded,
            editable: field && fieldDefinition.editable !== false && model.id !== field,
            defaultValue: fieldDefinition.defaultValue,
            postRaw: true
        }
        if (column.template) {
            const template = compileKendoTemplate(column.template)
            mapped.renderHtml = item => template(item)
        } else if (column.encoded === false) {
            mapped.encoded = false
        }
        if (column.headerTemplate) {
            mapped.titleHtml = typeof column.headerTemplate === 'function' ? column.headerTemplate() : column.headerTemplate
        }
        if (typeof column.editor === 'function') {
            mapped.editor = 'Custom'
            mapped.editorName = context.registerColumnEditor(column.editor, field)
        } else if (mapped.editable) {
            mapped.editor = editorForField(fieldDefinition.type)
            if (fieldDefinition.type === 'number' && column.format) {
                const decimals = /\{0:[nNcC](\d+)\}/.exec(column.format)
                if (decimals) mapped.decimals = Number(decimals[1])
            }
        }
        columns.push(mapped)
    }

    const events = {}
    for (const name of ['dataBound', 'edit', 'save', 'change', 'detailInit']) {
        if (typeof options[name] === 'function') events[name] = options[name]
    }
    for (const [kendoName, name] of [['requestStart', 'requestStart'], ['requestEnd', 'requestEnd'], ['error', 'error']]) {
        if (typeof dataSourceOptions[kendoName] === 'function') events[name] = dataSourceOptions[kendoName]
    }
    if (typeof dataSourceOptions.change === 'function') warn('dataSource.change')

    const toolbar = {}
    for (const entry of options.toolbar || []) {
        const name = typeof entry === 'string' ? entry : entry.name
        if (name === 'create') toolbar.create = entry.text || context.texts.addNewRecord || 'Add new record'
        else warn(`toolbar "${name}"`)
    }

    return {
        key: model.id || 'Id',
        transport: {
            read: transportUrl(transport.read),
            create: transportUrl(transport.create),
            update: transportUrl(transport.update),
            destroy: transportUrl(transport.destroy)
        },
        transportData: {
            read: transportData(transport.read),
            create: transportData(transport.create),
            update: transportData(transport.update),
            destroy: transportData(transport.destroy)
        },
        parameterMap: typeof transport.parameterMap === 'function' ? transport.parameterMap : null,
        schema: { data: schema.data || 'Data', total: schema.total || 'Total', errors: schema.errors || 'Errors' },
        pageSize: dataSourceOptions.pageSize || null,
        pager,
        pageSizes,
        editMode,
        confirmDestroy,
        reloadAfterSave: false,
        toolbar,
        columns,
        commands,
        events,
        autoBind: options.autoBind !== false,
        texts: context.texts
    }
}

/** A Custom editor adapter that runs a Kendo column editor(container, options) function. */
function kendoEditorFactory($, editorFunction, field) {
    return ({ item, value }) => {
        const container = document.createElement('div')
        container.className = 'grand-grid-kendo-editor'
        const model = { ...item, [field]: value }
        model.get = name => model[name]
        model.set = (name, v) => { model[name] = v }
        editorFunction($(container), { field, model })
        const find = () => container.querySelector(`[name="${CSS.escape(field)}"], [data-bind*="value:${field}"]`)
        const bindInitial = () => {
            const input = find()
            if (!input) return
            const widget = $(input).data('kendoNumericTextBox') || $(input).data('kendoDropDownList')
            if (widget) widget.value(value)
            else if (input.type === 'checkbox') input.checked = Boolean(value)
            else input.value = value ?? ''
        }
        bindInitial()
        return {
            element: container,
            getValue: () => {
                const input = find()
                if (!input) return model[field]
                const widget = $(input).data('kendoNumericTextBox') || $(input).data('kendoDropDownList')
                if (widget) return widget.value()
                if (input.type === 'checkbox') return input.checked
                return input.value
            },
            focus: () => find()?.focus()
        }
    }
}

/**
 * Registers $.fn.kendoGrid when kendo.grid.js is not loaded.
 * @returns {boolean} whether the shim was installed
 */
export function installKendoGridShim(win) {
    const $ = win.jQuery
    if (!$ || $.fn.kendoGrid) return false
    let editorCount = 0

    $.fn.kendoGrid = function (options) {
        return this.each(function () {
            if ($.data(this, 'kendoGrid')) return
            const element = this
            const grids = win.GrandAdmin?.grids
            if (!grids) {
                console.error('[GrandGrid shim] admin.grid.js must be loaded before admin.legacy.js')
                return
            }
            let api = null
            const texts = { ...grids.defaults.texts }
            const config = mapKendoGridOptions(options || {}, {
                texts,
                kendoApi: () => api,
                registerColumnEditor: (fn, field) => {
                    const name = `kendoShimEditor${++editorCount}`
                    grids.editors.register(name, kendoEditorFactory($, fn, field))
                    return name
                }
            })
            config.culture = grids.defaults.culture
            const grid = grids.construct(element, config)
            api = grid.api
            element.grandGrid = grid
            $.data(element, 'kendoGrid', api)
            if (config.autoBind) grid.ready.then(() => grid.dataSource.read())
        })
    }
    return true
}
