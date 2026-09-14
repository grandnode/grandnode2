import { getJson } from './transport.js'
import { parseNumber, toServerNumber } from './format.js'

//Inline editors for <admin-grid> columns. Each editor is created for one cell of the row
//being edited and exposes { element, getValue(), validate(), focus() }. getValue returns
//the typed value (numbers as numbers); serializeValue turns it into what the model binder
//reads in the request culture.

const customEditors = new Map()

/**
 * Registers a named editor for columns declared with editor="Custom" editor-name="name".
 * The factory receives { column, item, value, culture, grid, commit, cancel } and returns
 * { element, getValue, validate?, focus? }.
 */
export function registerEditor(name, factory) {
    if (typeof factory !== 'function') throw new TypeError('An editor factory must be a function')
    customEditors.set(name, factory)
}

function input(doc, type, column) {
    const element = doc.createElement('input')
    element.type = type
    element.className = type === 'checkbox' ? 'grand-grid-checkbox' : 'form-control form-control-sm'
    //no name: grids often sit inside the page's <form>, and an editor must never be posted
    //with it (a resource "Name" editor would overwrite the language's Name)
    element.dataset.field = column.field
    if (column.required) element.required = true
    return element
}

function keyHandlers(element, ctx) {
    element.addEventListener('keydown', e => {
        if (e.key === 'Enter') {
            e.preventDefault()
            ctx.commit?.()
        } else if (e.key === 'Escape') {
            e.preventDefault()
            ctx.cancel?.()
        }
    })
}

function markValidity(element, message) {
    element.classList.toggle('is-invalid', Boolean(message))
    if (typeof element.setCustomValidity === 'function') element.setCustomValidity(message || '')
    return message || null
}

function textEditor(ctx) {
    const element = input(ctx.doc, 'text', ctx.column)
    element.value = ctx.value ?? ''
    if (ctx.column.maxLength) element.maxLength = ctx.column.maxLength
    keyHandlers(element, ctx)
    return {
        element,
        getValue: () => element.value,
        validate: () => markValidity(element, ctx.column.required && element.value.trim() === '' ? 'required' : null),
        focus: () => element.focus()
    }
}

function numberEditor(ctx, integer) {
    const { column, culture } = ctx
    const element = input(ctx.doc, 'text', column)
    element.inputMode = integer ? 'numeric' : 'decimal'
    element.autocomplete = 'off'
    const decimals = integer ? 0 : column.decimals
    element.value = typeof ctx.value === 'number' ? toServerNumber(ctx.value, null, culture) : (ctx.value ?? '')
    keyHandlers(element, ctx)
    const read = () => {
        const parsed = parseNumber(element.value, culture)
        if (parsed == null || Number.isNaN(parsed)) return parsed
        if (decimals != null) {
            const factor = Math.pow(10, decimals)
            return Math.round(parsed * factor) / factor
        }
        return parsed
    }
    return {
        element,
        getValue: read,
        validate: () => {
            const value = read()
            let message = null
            if (value == null) message = column.required ? 'required' : null
            else if (Number.isNaN(value)) message = 'number'
            else if (integer && !Number.isInteger(value)) message = 'integer'
            else if (column.min != null && value < column.min) message = 'min'
            else if (column.max != null && value > column.max) message = 'max'
            return markValidity(element, message)
        },
        focus: () => element.focus()
    }
}

function checkboxEditor(ctx) {
    const element = input(ctx.doc, 'checkbox', ctx.column)
    element.checked = ctx.value === true || ctx.value === 'true'
    keyHandlers(element, ctx)
    return { element, getValue: () => element.checked, validate: () => null, focus: () => element.focus() }
}

const pad = n => String(n).padStart(2, '0')

function dateEditor(ctx, withTime) {
    const element = input(ctx.doc, withTime ? 'datetime-local' : 'date', ctx.column)
    const raw = ctx.value == null ? '' : String(ctx.value)
    //server values are ISO strings; the input wants yyyy-MM-dd or yyyy-MM-ddTHH:mm
    const match = /^(\d{4}-\d{2}-\d{2})(?:[T ](\d{2}:\d{2}))?/.exec(raw)
    if (match) element.value = withTime ? `${match[1]}T${match[2] || '00:00'}` : match[1]
    else if (ctx.value instanceof Date) {
        const d = ctx.value
        const date = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
        element.value = withTime ? `${date}T${pad(d.getHours())}:${pad(d.getMinutes())}` : date
    }
    keyHandlers(element, ctx)
    return {
        element,
        getValue: () => (element.value === '' ? null : (withTime ? `${element.value}:00` : element.value)),
        validate: () => markValidity(element, ctx.column.required && element.value === '' ? 'required' : null),
        focus: () => element.focus()
    }
}

const remoteOptionsCache = new Map()

/** Loads options for a remote Select editor; answers are DataSourceResult or plain arrays. */
export function loadRemoteOptions(url, { textField = 'Name', valueField = 'Id', fetchJson = getJson } = {}) {
    const cacheKey = `${url}|${textField}|${valueField}`
    if (!remoteOptionsCache.has(cacheKey)) {
        const promise = fetchJson(url).then(body => {
            const rows = Array.isArray(body) ? body : (body && Array.isArray(body.Data) ? body.Data : [])
            return rows.map(row => ({ value: String(row[valueField] ?? ''), text: String(row[textField] ?? '') }))
        }).catch(error => {
            remoteOptionsCache.delete(cacheKey)
            throw error
        })
        remoteOptionsCache.set(cacheKey, promise)
    }
    return remoteOptionsCache.get(cacheKey)
}

export function clearRemoteOptionsCache() {
    remoteOptionsCache.clear()
}

function selectEditor(ctx) {
    const { column, doc } = ctx
    const element = doc.createElement('select')
    element.className = 'form-control form-control-sm'
    element.dataset.field = column.field
    if (column.required) element.required = true
    const current = ctx.value == null ? '' : String(ctx.value)
    const fill = options => {
        element.textContent = ''
        if (column.optionLabel != null) {
            const empty = doc.createElement('option')
            empty.value = ''
            empty.textContent = column.optionLabel
            element.appendChild(empty)
        }
        let found = false
        for (const option of options) {
            const node = doc.createElement('option')
            node.value = option.value
            node.textContent = option.text
            if (option.value === current) {
                node.selected = true
                found = true
            }
            element.appendChild(node)
        }
        if (!found && current !== '') {
            //keep the stored value selectable even when the list does not contain it
            const node = doc.createElement('option')
            node.value = current
            node.textContent = ctx.item?.[column.textField] ?? current
            node.selected = true
            element.insertBefore(node, element.firstChild)
        }
    }
    if (column.options) {
        fill(column.options)
    } else if (column.optionsUrl) {
        fill([])
        element.disabled = true
        ctx.loadOptions(column.optionsUrl, { textField: column.optionTextField, valueField: column.optionValueField })
            .then(options => fill(options))
            .catch(() => { })
            .finally(() => { element.disabled = false })
    }
    keyHandlers(element, ctx)
    return {
        element,
        getValue: () => element.value,
        validate: () => markValidity(element, column.required && element.value === '' ? 'required' : null),
        focus: () => element.focus()
    }
}

/**
 * Creates the editor for a column.
 * @param {object} ctx { column, item, value, culture, grid, commit, cancel, doc?, loadOptions? }
 */
export function createEditor(ctx) {
    const context = { doc: globalThis.document, loadOptions: loadRemoteOptions, ...ctx }
    switch (context.column.editor) {
        case 'Numeric': return numberEditor(context, false)
        case 'Integer': return numberEditor(context, true)
        case 'Checkbox': return checkboxEditor(context)
        case 'Date': return dateEditor(context, false)
        case 'DateTime': return dateEditor(context, true)
        case 'Select': return selectEditor(context)
        case 'Custom': {
            const factory = customEditors.get(context.column.editorName)
            if (!factory) {
                console.warn(`[admin-grid] editor "${context.column.editorName}" is not registered; using a text editor`)
                return textEditor(context)
            }
            const editor = factory(context)
            return { validate: () => null, focus: () => editor.element?.focus?.(), ...editor }
        }
        default: return textEditor(context)
    }
}

/** Converts an edited value to the form value the MVC binder reads in the request culture. */
export function serializeValue(column, value, culture) {
    if (value == null) return value
    switch (column.editor) {
        case 'Numeric':
            return typeof value === 'number' ? toServerNumber(value, column.decimals ?? null, culture) : value
        case 'Integer':
            return typeof value === 'number' ? String(Math.trunc(value)) : value
        default:
            return value
    }
}
