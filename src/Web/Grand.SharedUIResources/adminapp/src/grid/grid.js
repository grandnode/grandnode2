import { DataSource } from './datasource.js'
import { createEditor, serializeValue } from './editors.js'
import { compileCondition, readPath } from './expression.js'
import { formatValue, normalizeCulture } from './format.js'
import { renderPager } from './pager.js'
import { serializeFields } from './param.js'
import { compileTemplate } from './template.js'
import { createKendoApi } from './kendo-api.js'

//GrandGrid: the adapter between the <admin-grid> configuration and Tabulator. Tabulator
//renders the rows (formatters, column visibility, RTL, variable row heights); this class
//owns what Tabulator does not model the way the admin needs it - the server-paged data
//source, Kendo-style full-row inline editing, the checkbox selection that survives
//paging, detail rows with nested grids, and the Kendo-compatible instance API.

const NEW_ROW = Symbol('grandNewRow')

/** Resolves "name" or "Namespace.name" to a global function, or null. */
export function resolveGlobalFunction(name) {
    if (!name || typeof name !== 'string' || !/^[A-Za-z_$][\w$]*(\.[A-Za-z_$][\w$]*)*$/.test(name)) return null
    let target = window
    for (const part of name.split('.')) {
        if (target == null) return null
        target = target[part]
    }
    return typeof target === 'function' ? target : null
}

/** Additional read data from a search container, first value per name like the MVC binder. */
export function searchFormData(selector, doc = document) {
    const container = selector ? doc.querySelector(selector) : null
    const data = {}
    for (const { name, value } of serializeFields(container)) {
        if (name === '__RequestVerificationToken') continue
        if (name.endsWith('[]')) {
            (data[name] = data[name] || []).push(value)
        } else if (!(name in data)) {
            data[name] = value
        }
    }
    return data
}

export function escapeHtml(text) {
    return String(text ?? '').replace(/[&<>"']/g, ch => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[ch])
}

function el(doc, tag, className, text) {
    const node = doc.createElement(tag)
    if (className) node.className = className
    if (text != null) node.textContent = text
    return node
}

function iconButton(doc, className, icon, text, onClick) {
    const button = el(doc, 'button', className)
    button.type = 'button'
    if (icon) {
        const i = el(doc, 'i', icon)
        i.setAttribute('aria-hidden', 'true')
        button.appendChild(i)
    }
    if (text) button.appendChild(doc.createTextNode(icon ? ' ' + text : text))
    button.addEventListener('click', e => {
        e.preventDefault()
        e.stopPropagation()
        onClick(e)
    })
    return button
}

export class GrandGrid {
    /**
     * @param {HTMLElement} element
     * @param {object} config parsed data-grand-grid JSON
     * @param {{Tabulator: Function, parent?: GrandGrid}} deps
     */
    constructor(element, config, deps) {
        this.element = element
        this.config = config
        this.deps = deps
        this.doc = element.ownerDocument
        this.parent = deps.parent || null
        const inherited = this.parent ? this.parent.config : {}
        this.texts = { ...(inherited.texts || {}), ...(config.texts || {}) }
        this.culture = normalizeCulture(config.culture || inherited.culture)
        this.rtl = config.rtl ?? inherited.rtl ?? false
        this.templateRoot = deps.templateRoot || this.doc
        this.key = config.key || 'Id'
        this.columns = (config.columns || []).map((column, index) => this._prepareColumn(column, index))
        this.editMode = config.editMode || 'None'
        this.commands = config.commands || null
        this.selectable = config.selectable === 'Checkbox'
        this.detail = config.detail || null
        this._selected = new Set()
        this._edit = null
        this._expanded = new Map()
        this._detailGrids = new Map()
        this._headerCheckbox = null
        this._resizeHandler = null
        this._commandVisible = this.commands?.visibleIf ? compileCondition(this.commands.visibleIf) : null

        this.dataSource = new DataSource({
            transport: config.transport || {},
            key: this.key,
            pageSize: config.pageSize || null,
            additionalData: () => this._additionalData(),
            serializeItem: (item, operation) => this._serializeItem(item, operation),
            onRequestStart: e => this._fire('requestStart', { ...e, sender: this.api.dataSource }, this.api.dataSource),
            onRequestEnd: e => this._fire('requestEnd', { ...e, sender: this.api.dataSource }, this.api.dataSource),
            onError: e => this._fire('error', { ...e, sender: this.api.dataSource }, this.api.dataSource),
            onChange: () => this._render(),
            //set by the Kendo shim only; <admin-grid> configurations are JSON
            parameterMap: typeof config.parameterMap === 'function' ? config.parameterMap : null,
            schema: config.schema || null
        })
        this.api = createKendoApi(this)
        this._build()
    }

    _prepareColumn(column, index) {
        const prepared = { ...column, index }
        if (column.template) {
            const node = this.templateRoot.getElementById
                ? this.templateRoot.getElementById(column.template)
                : this.templateRoot.querySelector('#' + CSS.escape(column.template))
            if (node && node.tagName === 'TEMPLATE') prepared.renderer = compileTemplate(node)
            else console.warn(`[admin-grid] template #${column.template} was not found`)
        }
        if (column.options) {
            prepared.optionText = new Map(column.options.map(o => [String(o.value), o.text]))
        }
        return prepared
    }

    _additionalData() {
        const data = {}
        if (this.config.searchForm) Object.assign(data, searchFormData(this.config.searchForm, this.doc))
        const transportData = this.config.transportData?.read
        if (typeof transportData === 'function') Object.assign(data, transportData.call(this.api.dataSource) || {})
        if (this.config.additionalData) {
            const fn = resolveGlobalFunction(this.config.additionalData)
            if (fn) Object.assign(data, fn.call(this.api) || {})
            else console.warn(`[admin-grid] additional-data function "${this.config.additionalData}" is not defined`)
        }
        return data
    }

    _serializeItem(item, operation) {
        const fields = {}
        for (const name of Object.keys(item)) {
            const value = item[name]
            if (typeof value === 'function') continue
            fields[name] = value
        }
        for (const column of this.columns) {
            //postRaw: Kendo shim columns post values untouched, like the Kendo transports did
            if (column.field && column.editor && !column.postRaw && Object.prototype.hasOwnProperty.call(fields, column.field)) {
                fields[column.field] = serializeValue(column, fields[column.field], this.culture)
            }
        }
        if (operation === 'create' && this.key in fields && fields[this.key] == null) fields[this.key] = ''
        const transportData = this.config.transportData?.[operation]
        if (typeof transportData === 'function') return { ...(transportData.call(this.api.dataSource, fields) || {}), ...fields }
        return fields
    }

    _fire(name, e, thisArg) {
        //names of global functions from <admin-grid on-*>, functions from the Kendo shim
        const handlerName = this.config.events?.[name]
        const handler = typeof handlerName === 'function' ? handlerName : resolveGlobalFunction(handlerName)
        if (handler) handler.call(thisArg || this.api, e)
        return e
    }

    _build() {
        const { doc, element } = this
        element.classList.add('grand-grid')
        element.setAttribute('data-role', 'grid')
        if (this.rtl) element.setAttribute('dir', 'rtl')
        element.textContent = ''

        if (this.config.toolbar?.create && this.editMode === 'Inline' && this.config.transport?.create) {
            const toolbar = el(doc, 'div', 'grand-grid-toolbar')
            toolbar.appendChild(iconButton(doc, 'btn btn-sm btn-success grand-grid-add', 'fa fa-plus', this.config.toolbar.create, () => this.addRow()))
            element.appendChild(toolbar)
        }
        this.tableElement = el(doc, 'div', 'grand-grid-table')
        element.appendChild(this.tableElement)
        this.pagerElement = el(doc, 'div', 'grand-grid-pager')
        element.appendChild(this.pagerElement)

        element.addEventListener('click', e => this._onClick(e))

        const { Tabulator } = this.deps
        this.table = new Tabulator(this.tableElement, {
            index: this.key,
            layout: 'fitColumns',
            renderVertical: 'basic',
            dataLoader: false,
            //Tabulator inserts the placeholder as HTML
            placeholder: escapeHtml(this.texts.noRecords || ''),
            textDirection: this.rtl ? 'rtl' : 'ltr',
            columns: this._tabulatorColumns(),
            rowFormatter: row => this._formatRow(row),
            data: []
        })
        this.ready = new Promise(resolve => this.table.on('tableBuilt', resolve))
        this.ready.then(() => {
            this._applyResponsive()
            if (this.columns.some(c => c.minScreenWidth)) {
                let timer = null
                this._resizeHandler = () => {
                    clearTimeout(timer)
                    timer = setTimeout(() => this._applyResponsive(), 100)
                }
                window.addEventListener('resize', this._resizeHandler)
            }
        })
        renderPager(this.pagerElement, this._pagerOptions())
    }

    _pagerOptions() {
        return {
            mode: this.config.pager || 'Full',
            dataSource: this.dataSource,
            pageSizes: this.config.pageSizes,
            texts: this.texts,
            culture: this.culture,
            doc: this.doc
        }
    }

    _tabulatorColumns() {
        const columns = []
        const averageWidth = Math.round(this.columns.filter(c => c.width).reduce((sum, c, _, all) => sum + c.width / all.length, 0)) || 150

        if (this.detail) {
            columns.push({
                title: '',
                width: 36,
                minWidth: 36,
                hozAlign: 'center',
                cssClass: 'grand-grid-detail-cell',
                formatter: cell => this._detailToggle(cell.getRow())
            })
        }
        if (this.selectable) {
            columns.push({
                title: '',
                width: 44,
                minWidth: 44,
                hozAlign: 'center',
                headerHozAlign: 'center',
                cssClass: 'grand-grid-select-cell',
                titleFormatter: () => this._createHeaderCheckbox(),
                formatter: cell => this._rowCheckbox(cell.getRow())
            })
        }
        for (const column of this.columns) {
            const definition = {
                title: column.title || '',
                //Tabulator inserts titles as HTML; localized titles are text
                titleFormatter: () => column.titleHtml != null ? this._html(column.titleHtml) : this.doc.createTextNode(column.title || ''),
                //no field: Tabulator would write it into the row data, which is posted back
                //the tag helper serializes Left/Center/Right, Tabulator wants lowercase
                hozAlign: column.align ? column.align.toLowerCase() : undefined,
                headerHozAlign: (column.headerAlign || column.align) ? (column.headerAlign || column.align).toLowerCase() : undefined,
                visible: !column.hidden,
                minWidth: Math.min(column.width || averageWidth, 60),
                widthGrow: Math.max(1, Math.round((column.width || averageWidth) / 10)),
                formatter: cell => this._formatCell(cell.getRow(), column)
            }
            definition.cssClass = `grand-grid-column-${column.index}` + (column.cssClass ? ' ' + column.cssClass : '')
            columns.push(definition)
        }
        if (this.commands && (this.commands.edit || this.commands.destroy || this.commands.custom?.length)) {
            columns.push({
                title: this.commands.title || '',
                titleFormatter: () => this.doc.createTextNode(this.commands.title || ''),
                minWidth: this.commands.width || 160,
                widthGrow: Math.max(1, Math.round((this.commands.width || 200) / 10)),

                cssClass: 'grand-grid-commands',
                formatter: cell => this._formatCommands(cell.getRow())
            })
        }
        return columns
    }

    _isEditing(row) {
        return this._edit != null && this._edit.row === row.getData()
    }

    _formatCell(row, column) {
        const item = row.getData()
        if (this._isEditing(row) && column.field && column.editable !== false && column.editor && column.editor !== 'None') {
            return this._editorFor(column, item)
        }
        if (column.renderHtml) {
            //Kendo shim only: a compiled Kendo template returns HTML like it did in Kendo
            return this._html(column.renderHtml(item))
        }
        if (column.renderer) {
            const holder = el(this.doc, 'div', 'grand-grid-template')
            holder.appendChild(column.renderer.render(item, { texts: this.texts, culture: this.culture }))
            return holder
        }
        if (!column.field) return ''
        let value = readPath(item, column.field)
        if (column.textField) value = readPath(item, column.textField)
        else if (column.optionText && value != null) value = column.optionText.get(String(value)) ?? value
        if (column.editor === 'Checkbox' && typeof value === 'boolean' && !column.format) {
            const icon = el(this.doc, 'i', value ? 'fa fa-check grand-grid-true' : 'fa fa-times grand-grid-false')
            icon.setAttribute('aria-label', String(value))
            return icon
        }
        const text = formatValue(value, column.format, this.culture)
        if (column.encoded === false) {
            //opt-in raw HTML for fields the server builds as markup (Kendo encoded:false)
            return this._html(text)
        }
        return el(this.doc, 'span', null, text)
    }

    _html(html) {
        const holder = el(this.doc, 'div', 'grand-grid-html')
        const template = this.doc.createElement('template')
        template.innerHTML = html
        holder.appendChild(template.content)
        return holder
    }

    _editorFor(column, item) {
        const edit = this._edit
        const value = Object.prototype.hasOwnProperty.call(edit.draft, column.field) ? edit.draft[column.field] : readPath(item, column.field)
        const editor = createEditor({
            column,
            item,
            value,
            culture: this.culture,
            grid: this.api,
            doc: this.doc,
            commit: () => this.saveRow(),
            cancel: () => this.cancelEdit()
        })
        edit.editors.set(column.field, editor)
        const sync = () => {
            edit.draft[column.field] = editor.getValue()
        }
        editor.element.addEventListener('input', sync)
        editor.element.addEventListener('change', sync)
        return editor.element
    }

    _formatCommands(row) {
        const { doc, texts } = this
        const item = row.getData()
        const holder = el(doc, 'div', 'grand-grid-command-buttons')
        if (this._isEditing(row)) {
            holder.appendChild(iconButton(doc, 'btn btn-sm btn-primary grand-grid-update', 'fa fa-check', texts.update, () => this.saveRow()))
            holder.appendChild(iconButton(doc, 'btn btn-sm btn-default grand-grid-cancel', 'fa fa-ban', texts.cancel, () => this.cancelEdit()))
            return holder
        }
        const visible = this._commandVisible ? this._commandVisible(item, { texts }) : true
        if (visible && this.commands.edit && this.editMode === 'Inline') {
            holder.appendChild(iconButton(doc, 'btn btn-sm btn-default grand-grid-edit', 'fa fa-pencil', texts.edit, () => this.editRow(item)))
        }
        if (visible && this.commands.destroy) {
            holder.appendChild(iconButton(doc, 'btn btn-sm btn-default grand-grid-delete', 'fa fa-trash-o', texts.delete, () => this.destroyRow(item)))
        }
        for (const command of this.commands.custom || []) {
            if (command.visibleIf && !compileCondition(command.visibleIf)(item, { texts })) continue
            holder.appendChild(iconButton(doc, `btn btn-sm ${command.className || 'btn-default'} grand-grid-command`, command.icon, command.text, e => {
                const fn = typeof command.click === 'function' ? command.click : resolveGlobalFunction(command.click)
                if (fn) fn.call(this.api, item, e, this.api)
                else console.warn(`[admin-grid] command function "${command.click}" is not defined`)
            }))
        }
        return holder
    }

    _createHeaderCheckbox() {
        const input = this.doc.createElement('input')
        input.type = 'checkbox'
        input.className = 'grand-grid-select-all'
        if (this.texts.selectAll) input.setAttribute('aria-label', this.texts.selectAll)
        input.addEventListener('click', e => e.stopPropagation())
        input.addEventListener('change', () => this._selectPage(input.checked))
        this._headerCheckbox = input
        return input
    }

    _rowCheckbox(row) {
        const item = row.getData()
        const id = String(item[this.key])
        const input = this.doc.createElement('input')
        input.type = 'checkbox'
        input.className = 'grand-grid-select checkboxGroups'
        input.value = id
        input.checked = this._selected.has(id)
        if (this.texts.selectRow) input.setAttribute('aria-label', this.texts.selectRow)
        input.addEventListener('change', () => {
            if (input.checked) this._selected.add(id)
            else this._selected.delete(id)
            this._selectionChanged()
        })
        return input
    }

    _selectPage(checked) {
        for (const item of this.dataSource.data()) {
            const id = String(item[this.key])
            if (checked) this._selected.add(id)
            else this._selected.delete(id)
        }
        this.tableElement.querySelectorAll('input.grand-grid-select').forEach(input => {
            input.checked = checked
        })
        this._selectionChanged()
    }

    _updateHeaderCheckbox() {
        if (!this._headerCheckbox) return
        const items = this.dataSource.data()
        const selectedOnPage = items.filter(item => this._selected.has(String(item[this.key]))).length
        this._headerCheckbox.checked = items.length > 0 && selectedOnPage === items.length
        this._headerCheckbox.indeterminate = selectedOnPage > 0 && selectedOnPage < items.length
    }

    _selectionChanged() {
        this._updateHeaderCheckbox()
        const selectedIds = this.selectedIds
        this._fire('change', { sender: this.api, selectedIds })
        this.element.dispatchEvent(new CustomEvent('grand-grid:selection', { bubbles: true, detail: { grid: this.api, selectedIds } }))
    }

    /** Ids ticked in the checkbox column, across pages. */
    get selectedIds() {
        return Array.from(this._selected)
    }

    clearSelection() {
        this._selected.clear()
        this.tableElement.querySelectorAll('input.grand-grid-select').forEach(input => {
            input.checked = false
        })
        this._selectionChanged()
    }

    _detailToggle(row) {
        const item = row.getData()
        const expanded = this._expanded.has(item)
        const button = iconButton(this.doc, 'grand-grid-detail-toggle', expanded ? 'fa fa-minus-square-o' : 'fa fa-plus-square-o', null, () => this.toggleDetail(item))
        button.setAttribute('aria-expanded', String(expanded))
        if (this.texts.toggleDetail) {
            button.title = this.texts.toggleDetail
            button.setAttribute('aria-label', this.texts.toggleDetail)
        }
        return button
    }

    _formatRow(row) {
        const item = row.getData()
        const rowElement = row.getElement()
        rowElement.classList.toggle('grand-grid-edit-row', this._isEditing(row))
        const holder = this._expanded.get(item)
        if (holder) rowElement.appendChild(holder)
    }

    _findRow(item) {
        return this.table.getRows().find(row => row.getData() === item) || null
    }

    toggleDetail(item) {
        const row = this._findRow(item)
        if (!row) return
        if (this._expanded.has(item)) {
            const holder = this._expanded.get(item)
            holder.remove()
            this._expanded.delete(item)
            this._detailGrids.get(item)?.destroy()
            this._detailGrids.delete(item)
        } else {
            const holder = el(this.doc, 'div', 'grand-grid-detail')
            this._expanded.set(item, holder)
            if (this.detail?.columns) {
                const params = {}
                for (const { name, field } of this.detail.params || []) params[name] = readPath(item, field)
                const childElement = el(this.doc, 'div')
                holder.appendChild(childElement)
                const childConfig = {
                    ...this.detail,
                    transport: { ...(this.detail.transport || {}), read: DataSource.urlWithParams(this.detail.transport.read, params) }
                }
                const child = new GrandGrid(childElement, childConfig, { ...this.deps, parent: this })
                this._detailGrids.set(item, child)
                child.ready.then(() => child.dataSource.read())
            }
            this._fire('detailInit', { sender: this.api, data: item, detailCell: holder, masterRow: row.getElement() })
        }
        row.reformat()
    }

    _onClick(e) {
        const target = e.target.closest('[data-grid-click]')
        if (!target || target.closest('.grand-grid') !== this.element) return
        const row = target.closest('.tabulator-row')
        const item = row ? this.dataItem(row) : null
        const fn = resolveGlobalFunction(target.getAttribute('data-grid-click'))
        if (!fn) {
            console.warn(`[admin-grid] data-grid-click function "${target.getAttribute('data-grid-click')}" is not defined`)
            return
        }
        e.preventDefault()
        fn.call(this.api, item, e, this.api)
    }

    _applyResponsive() {
        const width = window.innerWidth
        const tabulatorColumns = this.table.getColumns()
        let changed = false
        for (const column of this.columns) {
            if (!column.minScreenWidth || column.hidden) continue
            const marker = `grand-grid-column-${column.index}`
            const component = tabulatorColumns.find(c => (c.getDefinition().cssClass || '').split(' ').includes(marker))
            if (!component) continue
            const visible = width >= column.minScreenWidth
            if (component.isVisible() === visible) continue
            if (visible) component.show()
            else component.hide()
            changed = true
        }
        //row widths and wrapped cell heights are measured once; measure again
        if (changed) this.table.redraw(true)
    }

    async _render() {
        await this.ready
        this._edit = null
        this._detailGrids.forEach(grid => grid.destroy())
        this._detailGrids.clear()
        this._expanded.clear()
        await this.table.setData(this.dataSource.data())
        renderPager(this.pagerElement, this._pagerOptions())
        this._updateHeaderCheckbox()
        this._fire('dataBound', { sender: this.api })
        this.element.dispatchEvent(new CustomEvent('grand-grid:databound', { bubbles: true, detail: { grid: this.api } }))
    }

    /** Returns the data item of a row element (or any element inside it). */
    dataItem(rowElement) {
        const node = rowElement && rowElement.jquery ? rowElement[0] : rowElement
        const target = node?.closest ? node.closest('.tabulator-row') : null
        if (!target) return undefined
        const row = this.table.getRows().find(r => r.getElement() === target)
        return row ? row.getData() : undefined
    }

    /** Starts inline edit of an item (a data item, id or row element). */
    editRow(itemOrRow) {
        if (this.editMode !== 'Inline') return
        let item = itemOrRow
        if (typeof itemOrRow === 'string' || typeof itemOrRow === 'number') item = this.dataSource.get(itemOrRow)
        else if (itemOrRow && (itemOrRow.nodeType === 1 || itemOrRow.jquery)) item = this.dataItem(itemOrRow)
        if (!item) return
        if (this._edit) this.cancelEdit()
        const row = this._findRow(item)
        if (!row) return
        this._edit = { row: item, draft: {}, editors: new Map(), isNew: item[NEW_ROW] === true }
        row.reformat()
        const first = this._edit.editors.values().next().value
        first?.focus?.()
        const model = { ...item, isNew: () => this._edit?.isNew === true }
        this._fire('edit', { sender: this.api, model, container: row.getElement() })
    }

    cancelEdit() {
        const edit = this._edit
        if (!edit) return
        this._edit = null
        const row = this._findRow(edit.row)
        if (!row) return
        if (edit.isNew) {
            row.delete()
        } else {
            row.reformat()
        }
    }

    async addRow() {
        if (this.editMode !== 'Inline') return
        await this.ready
        if (this._edit) this.cancelEdit()
        const item = { [NEW_ROW]: true }
        for (const column of this.columns) {
            if (column.field && column.defaultValue !== undefined) item[column.field] = column.defaultValue
        }
        Object.assign(item, this.config.newItem || {})
        if (!(this.key in item)) item[this.key] = ''
        await this.table.addRow(item, true)
        this.editRow(item)
    }

    /** Validates the editors and posts create or update. */
    async saveRow() {
        const edit = this._edit
        if (!edit) return false
        let invalid = null
        for (const [field, editor] of edit.editors) {
            edit.draft[field] = editor.getValue()
            if (editor.validate() && !invalid) invalid = editor
        }
        if (invalid) {
            invalid.focus?.()
            return false
        }
        const values = { ...edit.draft }
        const model = { ...edit.row, ...values }
        delete model[NEW_ROW]
        const saveEvent = this._fire('save', { sender: this.api, model, values, defaultPrevented: false, preventDefault() { this.defaultPrevented = true } })
        if (saveEvent.defaultPrevented) return false
        const operation = edit.isNew ? 'create' : 'update'
        const ok = await this.dataSource.save(operation, model)
        if (!ok) return false
        //an edit started meanwhile belongs to someone else
        if (this._edit === edit) this._edit = null
        if (edit.isNew || this.config.reloadAfterSave !== false) {
            await this.dataSource.read()
        } else {
            Object.assign(edit.row, values)
            this._findRow(edit.row)?.reformat()
        }
        return true
    }

    async destroyRow(itemOrRow) {
        let item = itemOrRow
        if (itemOrRow && (itemOrRow.nodeType === 1 || itemOrRow.jquery)) item = this.dataItem(itemOrRow)
        if (!item) return false
        if (this.config.confirmDestroy && !window.confirm(this.texts.deleteConfirmation || this.texts.areYouSure || 'Are you sure you want to delete this record?')) return false
        const ok = await this.dataSource.save('destroy', item)
        if (!ok) return false
        const index = this.dataSource.indexOf(item)
        if (index >= 0) {
            this.dataSource._data.splice(index, 1)
            this.dataSource._total = Math.max(0, this.dataSource._total - 1)
        }
        if (this.config.reloadAfterDestroy) await this.dataSource.read()
        else await this._render()
        return true
    }

    /** Re-renders the current data without a request. */
    refresh() {
        return this._render()
    }

    destroy() {
        if (this._resizeHandler) window.removeEventListener('resize', this._resizeHandler)
        this._detailGrids.forEach(grid => grid.destroy())
        this._detailGrids.clear()
        this.table.destroy()
        if (window.jQuery) window.jQuery.removeData(this.element, 'kendoGrid')
    }
}
