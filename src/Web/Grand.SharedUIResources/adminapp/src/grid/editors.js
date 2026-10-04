import { getJson } from './transport.js'
import { param } from './param.js'
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
    //a tick in a row is the same control as a tick on a screen: Bootstrap 5 draws a checkbox
    //for form-check-input only, and grand-grid-checkbox was styled by nothing at all
    element.className = type === 'checkbox' ? 'form-check-input' : 'form-control form-control-sm'
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
    keepEscape(element)
}

/**
 * The Escape that cancels a cell stops at the cell. magnific-popup closes on the keyup of an
 * Escape anywhere in the document, so a grid in such a popup - the tier prices of a
 * combination - lost the whole popup, and whatever else was typed in it, to the key that was
 * only meant to leave one cell. The keyup cannot be stopped at the cell: the keydown has
 * already cancelled it, the cell is gone and the keyup arrives at the body. So the keydown
 * leaves word with the window, which takes the one keyup that follows before anything else.
 */
function keepEscape(element) {
    element.addEventListener('keydown', e => {
        if (e.key !== 'Escape') return
        const view = element.ownerDocument?.defaultView || globalThis
        const swallow = up => {
            if (up.key !== 'Escape') return
            up.stopPropagation()
            view.removeEventListener('keyup', swallow, true)
        }
        view.addEventListener('keyup', swallow, true)
        //a keyup that never comes (the window lost the focus) must not eat a later Escape
        view.setTimeout(() => view.removeEventListener('keyup', swallow, true), 1000)
    }, true)
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

/**
 * The display pattern of the field while it is not focused. Never a grouping one: what the
 * cell editor showed before the spinner was the raw number, and a group separator that is a
 * space in several cultures only makes the value harder to read back.
 */
function editPattern(decimals) {
    if (decimals == null) return '#.##########'
    return decimals > 0 ? `#.${'#'.repeat(decimals)}` : '#'
}

/**
 * The numeric control the forms use (ui/numeric.js), in the size of a row: a cell is edited
 * with the same field and the same two arrows as a screen. Like the date editor it is taken
 * off GrandAdmin when the cell opens rather than imported - admin.ui.js is loaded next to
 * admin.grid.js on every panel page, and an import would put a second copy of the control in
 * this bundle. Without it - a page that loads only the grid - the cell keeps the plain text
 * box it had.
 * What the row posts does not change either way: the value is still read out of the text the
 * person typed, rounded to the column decimals and serialized by serializeValue.
 */
function numberEditor(ctx, integer) {
    const { column, culture } = ctx
    const element = input(ctx.doc, 'text', column)
    element.inputMode = integer ? 'numeric' : 'decimal'
    element.autocomplete = 'off'
    const decimals = integer ? 0 : column.decimals
    element.value = typeof ctx.value === 'number' ? toServerNumber(ctx.value, null, culture) : (ctx.value ?? '')

    const factory = globalThis.GrandAdmin?.numeric?.create
    let holder = element
    let field = element
    if (typeof factory === 'function') {
        holder = ctx.doc.createElement('div')
        holder.className = 'grand-grid-numeric'
        holder.appendChild(element)
        const widget = factory(element, {
            culture,
            format: editPattern(decimals),
            decimals,
            min: column.min,
            max: column.max,
            step: column.step
        })
        //denser than on a screen: the control has to fit the cell, not widen the row
        widget.group.classList.add('input-group-sm')
        field = widget.text
        field.dataset.field = column.field
        if (column.required) field.required = true
        //the arrows write the value straight into the field, so the row is told the way
        //typing tells it - without this an inline draft would miss a click on an arrow
        const notify = () => field.dispatchEvent(new (ctx.doc.defaultView?.Event || Event)('input', { bubbles: true }))
        widget.spin.addEventListener('click', notify)
        //added after the widget's own handler, so the step has already been taken
        field.addEventListener('keydown', e => {
            if (e.key === 'ArrowUp' || e.key === 'ArrowDown') notify()
        })
    }
    keyHandlers(holder, ctx)

    const read = () => {
        const parsed = parseNumber(field.value, culture)
        if (parsed == null || Number.isNaN(parsed)) return parsed
        if (decimals != null) {
            const factor = Math.pow(10, decimals)
            return Math.round(parsed * factor) / factor
        }
        return parsed
    }
    return {
        element: holder,
        getValue: read,
        validate: () => {
            const value = read()
            let message = null
            if (value == null) message = column.required ? 'required' : null
            else if (Number.isNaN(value)) message = 'number'
            else if (integer && !Number.isInteger(value)) message = 'integer'
            else if (column.min != null && value < column.min) message = 'min'
            else if (column.max != null && value > column.max) message = 'max'
            return markValidity(field, message)
        },
        focus: () => field.focus()
    }
}

function checkboxEditor(ctx) {
    const element = input(ctx.doc, 'checkbox', ctx.column)
    element.checked = ctx.value === true || ctx.value === 'true'
    keyHandlers(element, ctx)
    return { element, getValue: () => element.checked, validate: () => null, focus: () => element.focus() }
}

const pad = n => String(n).padStart(2, '0')

/**
 * The picker the forms use (ui/datetime.js), so a date is edited the same way in a row as on
 * a screen and is read in the store's culture rather than the browser's locale. It is taken
 * off GrandAdmin at the moment the cell is opened rather than imported: admin.ui.js is loaded
 * next to admin.grid.js on every panel page, and importing it would put a second copy of the
 * picker in this bundle. Without it - a page that loads only the grid - the native input is
 * still what the cell gets, exactly as before.
 * What the row posts does not change either way: the ISO string the server sent goes back.
 */
function dateEditor(ctx, withTime) {
    const factory = globalThis.GrandAdmin?.dateInput?.create
    const raw = ctx.value == null ? '' : String(ctx.value)
    let iso = ''
    const match = /^(\d{4}-\d{2}-\d{2})(?:[T ](\d{2}:\d{2}))?/.exec(raw)
    if (match) iso = `${match[1]}T${match[2] || '00:00'}:00`
    else if (ctx.value instanceof Date) {
        const d = ctx.value
        iso = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:00`
    }

    if (typeof factory !== 'function') return nativeDateEditor(ctx, withTime, iso)

    const element = input(ctx.doc, 'text', ctx.column)
    const holder = ctx.doc.createElement('div')
    holder.className = 'grand-grid-date'
    holder.appendChild(element)
    const widget = factory(element, { culture: ctx.culture, mode: withTime ? 'datetime' : 'date', value: iso })
    //a row has no server-rendered text to preserve, so the seed is written out at once
    widget.value(iso === '' ? null : iso)
    keyHandlers(element, ctx)
    const isoValue = () => {
        const date = widget.value()
        if (!date) return null
        const day = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
        return withTime ? `${day}T${pad(date.getHours())}:${pad(date.getMinutes())}:00` : day
    }
    return {
        element: holder,
        getValue: isoValue,
        validate: () => markValidity(element, ctx.column.required && isoValue() == null ? 'required' : null),
        focus: () => element.focus()
    }
}

function nativeDateEditor(ctx, withTime, iso) {
    const element = input(ctx.doc, withTime ? 'datetime-local' : 'date', ctx.column)
    if (iso) element.value = withTime ? iso.slice(0, 16) : iso.slice(0, 10)
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

/**
 * URL of a server-filtered option list: the query a Kendo DropDownList with
 * filter: "startswith" and serverFiltering sends (read by DataSourceRequestFilterBinder).
 */
export function filteredOptionsUrl(url, { text, operator = 'startswith', field = 'Name' }) {
    if (!text) return url
    const query = param({ filter: { logic: 'and', filters: [{ value: text, operator, field, ignoreCase: true }] } })
    return url + (url.includes('?') ? '&' : '?') + query
}

/**
 * The <select> under a Select cell editor, with the list-filling the two editors share.
 * Whatever draws the list, this element is what holds the chosen value, so getValue and
 * getText read the same thing they have always read and the row posts what it posted.
 */
function buildSelect(ctx) {
    const { column, doc } = ctx
    const element = doc.createElement('select')
    //Bootstrap 5 draws a select's chevron for form-select only; form-control leaves it bare
    element.className = 'form-select form-select-sm'
    element.dataset.field = column.field
    if (column.required) element.required = true
    const stored = ctx.value == null ? '' : String(ctx.value)
    const fill = options => {
        //keep what is selected now (the stored value, or a choice made before filtering)
        const selectedValue = element.options.length ? element.value : stored
        const selectedText = element.selectedIndex >= 0 ? element.options[element.selectedIndex].textContent : null
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
            if (option.value === selectedValue) {
                node.selected = true
                found = true
            }
            element.appendChild(node)
        }
        if (!found && selectedValue !== '') {
            //keep the value selectable even when the (filtered) list does not contain it
            const node = doc.createElement('option')
            node.value = selectedValue
            node.textContent = selectedValue === stored ? (ctx.item?.[column.textField] ?? selectedText ?? stored) : (selectedText ?? selectedValue)
            node.selected = true
            element.insertBefore(node, column.optionLabel != null ? element.options[1] || null : element.firstChild)
        } else if (!found) {
            element.value = ''
        }
    }
    const textField = column.optionTextField || 'Name'
    let loading = 0
    const load = text => {
        const request = ++loading
        element.disabled = true
        return ctx.loadOptions(filteredOptionsUrl(column.optionsUrl, { text, operator: column.optionsFilter, field: textField }), { textField, valueField: column.optionValueField })
            .then(options => { if (request === loading) fill(options) })
            .catch(() => { })
            .finally(() => { if (request === loading) element.disabled = false })
    }
    return { element, fill, load, textField }
}

/**
 * The list the forms use (ui/select.js): what is typed goes into the field itself, a column
 * with options-filter asks the server for the rows as they are typed, and the placeholder and
 * the "no records" line are the texts a screen shows. Like the date and the numeric editor it
 * is taken off GrandAdmin at the moment the cell opens rather than imported - admin.ui.js is
 * loaded next to admin.grid.js on every panel page, and an import would put a second copy of
 * the list control in this bundle. Without it - a page that loads only the grid - the cell
 * keeps the plain select with a search box above it that it had.
 * What the row posts does not change either way: the same <select> underneath holds the value.
 */
function selectEditor(ctx) {
    const parts = buildSelect(ctx)
    const factory = globalThis.GrandAdmin?.select?.create
    return typeof factory === 'function' ? panelSelectEditor(ctx, parts, factory) : nativeSelectEditor(ctx, parts)
}

//Lists whose cell has been closed. Tom Select hangs the dropdown of a cell on <body> so no
//table, card or modal can clip it, and the grid throws the cell away without telling anyone;
//the ones whose wrapper has left the page are taken down after the next cell opens.
//After, not while: a row is drawn off the page and put in whole, so while the second list of
//a row is being made the first one is not in the page yet either - a sweep at that moment
//took it down, and the store column of a tier price came up as a bare select.
const closedLists = new Set()

function sweepClosedLists() {
    for (const widget of closedLists) {
        if (widget.wrapper?.isConnected) continue
        closedLists.delete(widget)
        try {
            widget.destroy()
        } catch {
            //the cell is gone either way; a dropdown left hanging is hidden
        }
    }
}

function panelSelectEditor(ctx, { element, fill, textField }, factory) {
    const { column, doc } = ctx
    const timers = doc.defaultView || globalThis
    timers.setTimeout(sweepClosedLists, 0)
    const holder = doc.createElement('div')
    holder.className = 'grand-grid-select-editor'
    holder.appendChild(element)
    //a column that names an operator is filtered by the server as the person types, like the
    //search box did; one with fixed options - or a remote one that never filtered - is
    //searched in the browser over the rows it was given
    const serverFiltered = Boolean(column.optionsUrl && column.optionsFilter)
    fill(column.options || [])

    const widget = factory(element, {
        mode: 'single',
        placeholder: column.optionLabel || undefined,
        //a cell edits a value, so an empty option with a text of its own ("All customer
        //groups") is a value like any other, not the "nothing chosen" of a filter
        emptyIsChoice: true,
        //the list of a cell hangs on <body>: inside the cell the table, a card or a modal
        //body would cut it off at its own edge
        dropdownParent: 'body',
        ...(serverFiltered
            ? { url: column.optionsUrl, textField, valueField: column.optionValueField || 'Id', filter: column.optionsFilter }
            : {})
    })
    //the dropdown is on <body>, outside every stylesheet scoped to the grid, and a panel
    //modal sits above the z-index of the theme's
    widget.dropdown.classList.add('grand-grid-dropdown')
    closedLists.add(widget)
    //Tom Select follows a scroll of the window only; a grid in a popup scrolls the popup, and
    //the list would stay where the cell was. Scroll does not bubble, so it is caught on the
    //way down, from whichever box is scrolled, for as long as the list is open.
    const follow = () => { if (widget.isOpen) widget.positionDropdown() }
    widget.on('dropdown_open', () => doc.addEventListener('scroll', follow, true))
    widget.on('dropdown_close', () => doc.removeEventListener('scroll', follow, true))
    widget.on('destroy', () => doc.removeEventListener('scroll', follow, true))

    if (column.optionsUrl && !serverFiltered) {
        //one request for the whole list, the one this column has always made
        ctx.loadOptions(column.optionsUrl, { textField, valueField: column.optionValueField })
            .then(options => {
                for (const option of options) {
                    if (widget.options[option.value]) widget.updateOption(option.value, option)
                    else widget.addOption(option)
                }
                widget.refreshOptions(false)
            })
            .catch(() => { })
    }

    //Escape belongs to the cell, not to the list: Tom Select stops the event when it closes
    //its dropdown, so the cell is only reached from the capture phase. Enter is the other way
    //round - the list gets it first to take the row under the cursor, and what is left over
    //(the list closed, nothing to take) saves the row.
    holder.addEventListener('keydown', e => {
        if (e.key !== 'Escape') return
        e.preventDefault()
        e.stopPropagation()
        ctx.cancel?.()
    }, true)
    holder.addEventListener('keydown', e => {
        if (e.key !== 'Enter' || e.defaultPrevented) return
        e.preventDefault()
        ctx.commit?.()
    })

    //The cell hands the field its focus with the list closed, as the select it replaced did.
    //A list that opened by itself - on the focus, or when the rows of a remote column arrived -
    //covered the rows below, and the click a person then gave the field to open it is the one
    //Tom Select reads as "close" and blurs, which in a batch grid also commits the cell. It
    //stays closed until the person does something in the cell: a click, a letter, ArrowDown.
    //Closing it again from dropdown_open is not enough - Tom Select reopens it at once, round
    //and round - so the opening itself is held back.
    let quiet = false
    const open = widget.open.bind(widget)
    widget.open = () => { if (!quiet) open() }
    keepEscape(holder)
    const speak = () => { quiet = false }
    holder.addEventListener('keydown', speak, true)
    holder.addEventListener('mousedown', speak, true)

    const text = () => {
        const value = element.value
        if (value === '') return ''
        return widget.options[value]?.text ?? (element.selectedIndex >= 0 ? element.options[element.selectedIndex].textContent : '')
    }
    return {
        element: holder,
        select: element,
        widget,
        getValue: () => element.value,
        getText: text,
        validate: () => {
            const message = markValidity(element, column.required && element.value === '' ? 'required' : null)
            widget.wrapper.classList.toggle('is-invalid', Boolean(message))
            return message
        },
        focus: () => {
            quiet = true
            widget.focus()
        }
    }
}

function nativeSelectEditor(ctx, { element, fill, load }) {
    const { column, doc } = ctx
    if (column.options) {
        fill(column.options)
    } else if (column.optionsUrl) {
        fill([])
        load('')
    }
    keyHandlers(element, ctx)
    const editor = {
        element,
        getValue: () => element.value,
        /** Display text of the selected option (for the row field named by text-field). */
        getText: () => (element.selectedIndex >= 0 && element.value !== '' ? element.options[element.selectedIndex].textContent : ''),
        validate: () => markValidity(element, column.required && element.value === '' ? 'required' : null),
        focus: () => element.focus()
    }
    if (column.optionsUrl && column.optionsFilter) {
        //a search box above the list re-queries the server, like the Kendo filter input
        const wrapper = doc.createElement('div')
        wrapper.className = 'grand-grid-select-filter'
        const search = doc.createElement('input')
        search.type = 'search'
        search.className = 'form-control form-control-sm'
        search.autocomplete = 'off'
        if (ctx.texts?.filter) search.placeholder = ctx.texts.filter
        let timer = null
        search.addEventListener('input', e => {
            e.stopPropagation()
            clearTimeout(timer)
            timer = setTimeout(() => load(search.value.trim()), 300)
        })
        search.addEventListener('keydown', e => {
            if (e.key === 'Enter') {
                e.preventDefault()
                clearTimeout(timer)
                load(search.value.trim())
            } else if (e.key === 'Escape') {
                e.preventDefault()
                ctx.cancel?.()
            }
        })
        wrapper.appendChild(search)
        wrapper.appendChild(element)
        editor.element = wrapper
        editor.select = element
        editor.search = search
        editor.focus = () => element.focus()
    }
    return editor
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
            //factories registered by views receive the same context as the built-in editors
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
