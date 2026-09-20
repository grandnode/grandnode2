// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { GrandGrid } from './grid.js'
import { compileTemplate } from './template.js'
import { filteredOptionsUrl, createEditor } from './editors.js'

//A stand-in for Tabulator with the part of its API the adapter uses: rows rendered as
//.tabulator-row elements with one .tabulator-cell per column formatter.
class FakeTabulator {
    constructor(element, options) {
        this.element = element
        this.options = options
        this.rows = []
        this.handlers = {}
        this.columns = options.columns.map(definition => ({
            getDefinition: () => definition,
            isVisible: () => true,
            show() { },
            hide() { }
        }))
        setTimeout(() => this.handlers.tableBuilt?.(), 0)
    }

    on(name, handler) {
        this.handlers[name] = handler
    }

    getColumns() {
        return this.columns
    }

    getRows() {
        return this.rows
    }

    redraw() { }

    destroy() {
        this.element.textContent = ''
    }

    _row(data) {
        const element = document.createElement('div')
        element.className = 'tabulator-row'
        const row = {
            getData: () => data,
            getElement: () => element,
            reformat: () => this._format(row),
            delete: () => {
                this.rows.splice(this.rows.indexOf(row), 1)
                element.remove()
            }
        }
        return row
    }

    _format(row) {
        const element = row.getElement()
        element.textContent = ''
        for (const definition of this.options.columns) {
            const cell = document.createElement('div')
            cell.className = `tabulator-cell ${definition.cssClass || ''}`.trim()
            const content = definition.formatter({ getRow: () => row })
            if (content instanceof Node) cell.appendChild(content)
            else if (content != null) cell.textContent = String(content)
            element.appendChild(cell)
        }
        this.options.rowFormatter(row)
    }

    async setData(data) {
        this.handlers.renderComplete?.()
        this.rows.forEach(row => row.getElement().remove())
        this.rows = data.map(item => this._row(item))
        for (const row of this.rows) {
            this._format(row)
            this.element.appendChild(row.getElement())
        }
    }

    async addRow(data, top) {
        const row = this._row(data)
        if (top) this.rows.unshift(row)
        else this.rows.push(row)
        this._format(row)
        this.element.prepend(row.getElement())
    }
}

function createGrid(config, post) {
    const element = document.createElement('div')
    document.body.appendChild(element)
    const grid = new GrandGrid(element, { culture: { name: 'en-US' }, texts: {}, ...config }, { Tabulator: FakeTabulator })
    if (post) grid.dataSource._post = post
    return grid
}

const alerts = []
window.alert = message => alerts.push(message)

const rows = grid => Array.from(grid.tableElement.querySelectorAll('.tabulator-row'))

afterEach(() => {
    const shown = alerts.splice(0)
    expect(shown).toEqual([])
    document.body.textContent = ''
    vi.restoreAllMocks()
})

describe('local data', () => {
    it('renders rows given in the configuration without a request', async () => {
        const post = vi.fn()
        const grid = createGrid({ data: [{ Id: '1', Email: 'a@b.c' }, { Id: '2', Email: 'd@e.f' }], columns: [{ field: 'Email' }] }, post)
        await grid.ready
        await grid.dataSource.read()
        await Promise.resolve()
        expect(post).not.toHaveBeenCalled()
        expect(rows(grid).map(r => r.textContent)).toEqual(['a@b.c', 'd@e.f'])
        expect(grid.dataSource.total()).toBe(2)
    })
})

describe('row selection', () => {
    it('selects one row on click and exposes it through select()', async () => {
        const change = vi.fn()
        window.onRowChange = change
        const grid = createGrid({
            data: [{ Id: 'a', Name: 'A' }, { Id: 'b', Name: 'B' }],
            selectable: 'Row',
            events: { change: 'onRowChange' },
            columns: [{ field: 'Name' }]
        })
        await grid.ready
        await grid.dataSource.read()
        rows(grid)[1].querySelector('.tabulator-cell').click()
        expect(change).toHaveBeenCalledTimes(1)
        expect(grid.api.dataItem(grid.api.select()[0])).toEqual({ Id: 'b', Name: 'B' })
        expect(rows(grid)[1].classList.contains('grand-grid-selected')).toBe(true)
        expect(rows(grid)[0].classList.contains('grand-grid-selected')).toBe(false)
        delete window.onRowChange
    })

    it('names the checkbox inputs when selection-name is set', async () => {
        const grid = createGrid({ data: [{ Id: 'p1' }], selectable: 'Checkbox', selectionName: 'SelectedProductIds', columns: [{ field: 'Id' }] })
        await grid.ready
        await grid.dataSource.read()
        const input = grid.tableElement.querySelector('input.grand-grid-select')
        expect(input.name).toBe('SelectedProductIds')
        expect(input.value).toBe('p1')
    })
})

describe('detail grids', () => {
    it('hides the expander where visible-if does not hold and passes master parameters to every operation', async () => {
        const post = vi.fn(async () => ({ Data: [{ Id: 'm1', Type: 1 }, { Id: 'm2', Type: 4 }], Total: 2 }))
        const grid = createGrid({
            transport: { read: '/list' },
            columns: [{ field: 'Id' }],
            detail: {
                visibleIf: 'Type != 4',
                params: [{ name: 'productAttributeMappingId', field: 'Id' }],
                transport: { read: '/values', destroy: '/valueDelete' },
                columns: [{ field: 'Name' }]
            }
        }, post)
        await grid.ready
        await grid.dataSource.read()
        const toggles = rows(grid).map(r => r.querySelector('.grand-grid-detail-toggle'))
        expect(toggles[0]).not.toBeNull()
        expect(toggles[1]).toBeNull()

        const init = vi.fn()
        grid.config.events = { detailInit: init }
        window.jQuery = { data: vi.fn(), removeData: vi.fn() }
        vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: true, text: async () => '{"Data":[],"Total":0}' })
        grid.toggleDetail(grid.dataSource.data()[0])
        const child = grid._detailGrids.get(grid.dataSource.data()[0])
        expect(child.config.transport.read).toBe('/values?productAttributeMappingId=m1')
        expect(child.config.transport.destroy).toBe('/valueDelete?productAttributeMappingId=m1')
        expect(window.jQuery.data).toHaveBeenCalledWith(child.element, 'kendoGrid', child.api)
        expect(init.mock.calls[0][0].detailGrid).toBe(child.api)
        expect(init.mock.calls[0][0].detailElement).toBe(child.element)
        await child.ready
        await vi.waitFor(() => expect(globalThis.fetch).toHaveBeenCalledWith('/values?productAttributeMappingId=m1', expect.anything()))
        delete window.jQuery
    })

    it('expands the rows of a recursive detail to the same detail, one level further down', async () => {
        const post = vi.fn(async () => ({ Data: [{ Id: 'c1', Children: 1 }, { Id: 'c2', Children: 0 }], Total: 2 }))
        const grid = createGrid({
            transport: { read: '/nodes' },
            texts: { category: 'Category' },
            columns: [{ field: 'Id' }],
            detail: {
                recursive: true,
                visibleIf: 'Children > 0',
                params: [{ name: 'parentId', field: 'Id' }],
                transport: { read: '/nodes' },
                columns: [{ field: 'Id' }]
            }
        }, post)
        await grid.ready
        await grid.dataSource.read()
        expect(rows(grid).map(r => r.querySelector('.grand-grid-detail-toggle') !== null)).toEqual([true, false])

        vi.spyOn(globalThis, 'fetch').mockImplementation(async url => ({
            ok: true,
            text: async () => JSON.stringify(url.endsWith('parentId=c1')
                ? { Data: [{ Id: 'c1a', Children: 2 }, { Id: 'c1b', Children: 0 }], Total: 2 }
                : { Data: [{ Id: 'c1a-x', Children: 0 }], Total: 1 })
        }))
        grid.toggleDetail(grid.dataSource.data()[0])
        const child = grid._detailGrids.get(grid.dataSource.data()[0])
        await child.ready
        await vi.waitFor(() => expect(child.dataSource.data().length).toBe(2))
        //the rows of the detail grid get the expander of the same detail, by the same condition
        expect(child.detail).toBe(grid.detail)
        expect(rows(child).map(r => r.querySelector('.grand-grid-detail-toggle') !== null)).toEqual([true, false])
        expect(child.texts.category).toBe('Category')

        child.toggleDetail(child.dataSource.data()[0])
        const grandchild = child._detailGrids.get(child.dataSource.data()[0])
        //the parameters of one level replace, not add to, the ones of the level above
        expect(grandchild.config.transport.read).toBe('/nodes?parentId=c1a')
        expect(grandchild.texts.category).toBe('Category')
        expect(grandchild.culture.name).toBe('en-US')
        await grandchild.ready
        await vi.waitFor(() => expect(grandchild.dataSource.data().map(x => x.Id)).toEqual(['c1a-x']))
    })

    it('does not nest a detail that is not recursive', async () => {
        const post = vi.fn(async () => ({ Data: [{ Id: 'm1' }], Total: 1 }))
        const grid = createGrid({
            transport: { read: '/list' },
            columns: [{ field: 'Id' }],
            detail: { params: [{ name: 'id', field: 'Id' }], transport: { read: '/values' }, columns: [{ field: 'Id' }] }
        }, post)
        await grid.ready
        await grid.dataSource.read()
        vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: true, text: async () => '{"Data":[{"Id":"v1"}],"Total":1}' })
        grid.toggleDetail(grid.dataSource.data()[0])
        const child = grid._detailGrids.get(grid.dataSource.data()[0])
        await child.ready
        await vi.waitFor(() => expect(child.dataSource.data().length).toBe(1))
        expect(child.detail).toBeNull()
        expect(rows(child)[0].querySelector('.grand-grid-detail-toggle')).toBeNull()
    })
})

describe('batch editing', () => {
    function bulkGrid(post, extra = {}) {
        return createGrid({
            transport: { read: '/select', update: '/update', destroy: '/delete' },
            editMode: 'Batch',
            batchPrefix: 'products',
            toolbar: { save: 'Save changes', cancel: 'Cancel changes' },
            commands: { destroy: true },
            columns: [
                { field: 'Name', editor: 'Text' },
                { field: 'Price', editor: 'Numeric', decimals: 4 },
                { field: 'Published', editor: 'Checkbox' }
            ],
            ...extra
        }, post)
    }

    const data = () => ({ Data: [{ Id: '1', Name: 'A', Price: 10, Published: true }, { Id: '2', Name: 'B', Price: 25, Published: false }, { Id: '3', Name: 'C', Price: 50, Published: true }], Total: 3 })

    it('edits a cell, marks it changed and posts the changed rows as products[i] in one request', async () => {
        const post = vi.fn(async url => (url === '/select' ? data() : ''))
        const grid = bulkGrid(post)
        await grid.ready
        await grid.dataSource.read()

        const priceCell = rows(grid)[0].querySelector('.grand-grid-column-1')
        priceCell.click()
        const input = rows(grid)[0].querySelector('.grand-grid-column-1 input')
        input.value = '12.5'
        expect(grid.commitCell()).toBe(true)
        expect(grid.dataSource.data()[0].Price).toBe(12.5)
        expect(rows(grid)[0].querySelector('.grand-grid-column-1 .grand-grid-dirty')).not.toBeNull()

        grid.editCell(grid.dataSource.data()[2], grid.columns[0])
        rows(grid)[2].querySelector('.grand-grid-column-0 input').value = 'C2'
        grid.commitCell()

        grid.element.querySelector('.grand-grid-save-changes').click()
        await vi.waitFor(() => expect(post).toHaveBeenCalledWith('/update', expect.anything()))
        const [, payload] = post.mock.calls.find(([url]) => url === '/update')
        expect(payload).toMatchObject({
            'products[0].Id': '1',
            'products[0].Price': '12.5000',
            'products[0].Published': true,
            'products[1].Id': '3',
            'products[1].Name': 'C2',
            'products[1].Price': '50.0000'
        })
        expect(Object.keys(payload).some(k => k.startsWith('products[2]'))).toBe(false)
        await vi.waitFor(() => expect(post.mock.calls.filter(([url]) => url === '/select')).toHaveLength(2))
        expect(grid.hasChanges()).toBe(false)
    })

    it('keeps deleted rows pending until save and restores everything on cancel', async () => {
        const post = vi.fn(async url => (url === '/select' ? data() : ''))
        vi.spyOn(window, 'confirm').mockReturnValue(true)
        const grid = bulkGrid(post, { confirmDestroy: true })
        await grid.ready
        await grid.dataSource.read()

        await grid.destroyRow(grid.dataSource.data()[1])
        expect(post).toHaveBeenCalledTimes(1)
        expect(grid.dataSource.data()).toHaveLength(2)

        grid.editCell(grid.dataSource.data()[0], grid.columns[0])
        rows(grid)[0].querySelector('.grand-grid-column-0 input').value = 'changed'
        grid.commitCell()
        await grid.cancelChanges()
        expect(grid.dataSource.data().map(x => x.Name)).toEqual(['A', 'B', 'C'])
        expect(grid.hasChanges()).toBe(false)

        await grid.destroyRow(grid.dataSource.data()[2])
        await grid.saveChanges()
        const [, payload] = post.mock.calls.find(([url]) => url === '/delete')
        expect(payload['products[0].Id']).toBe('3')
    })

    it('without batch-prefix posts one request per changed row with plain fields', async () => {
        const post = vi.fn(async url => (url === '/select' ? data() : ''))
        const grid = bulkGrid(post, { batchPrefix: undefined })
        await grid.ready
        await grid.dataSource.read()
        grid.editCell(grid.dataSource.data()[0], grid.columns[1])
        rows(grid)[0].querySelector('.grand-grid-column-1 input').value = '11'
        grid.commitCell()
        grid.editCell(grid.dataSource.data()[1], grid.columns[1])
        rows(grid)[1].querySelector('.grand-grid-column-1 input').value = '22'
        grid.commitCell()
        await grid.saveChanges()
        const updates = post.mock.calls.filter(([url]) => url === '/update').map(([, p]) => p)
        expect(updates.map(p => [p.Id, p.Price])).toEqual([['1', '11.0000'], ['2', '22.0000']])
    })

    it('does not open editors for columns without one', async () => {
        const post = vi.fn(async () => data())
        const grid = bulkGrid(post, { columns: [{ field: 'Name' }, { field: 'Price', editor: 'Numeric' }] })
        await grid.ready
        await grid.dataSource.read()
        rows(grid)[0].querySelector('.grand-grid-column-0').click()
        expect(grid._cellEdit).toBeNull()
    })
})

describe('select editor', () => {
    it('builds the Kendo DropDownList server filter query', () => {
        expect(filteredOptionsUrl('/Admin/Search/Category', { text: 'Note' }))
            .toBe('/Admin/Search/Category?filter%5Blogic%5D=and&filter%5Bfilters%5D%5B0%5D%5Bvalue%5D=Note&filter%5Bfilters%5D%5B0%5D%5Boperator%5D=startswith&filter%5Bfilters%5D%5B0%5D%5Bfield%5D=Name&filter%5Bfilters%5D%5B0%5D%5BignoreCase%5D=true')
        expect(filteredOptionsUrl('/x?a=1', { text: '' })).toBe('/x?a=1')
    })

    it('reloads remote options for the typed text and keeps the stored value', async () => {
        vi.useFakeTimers()
        const loadOptions = vi.fn(async url => (url.includes('filter') ? [{ value: 'c2', text: 'Notebooks' }] : [{ value: 'c1', text: 'Computers' }]))
        const editor = createEditor({
            column: { field: 'CategoryId', editor: 'Select', optionsUrl: '/Search/Category', optionsFilter: 'startswith', textField: 'Category', required: true, optionLabel: 'Select category...' },
            item: { CategoryId: 'c9', Category: 'Old one' },
            value: 'c9',
            loadOptions
        })
        await vi.runAllTimersAsync()
        const select = editor.element.querySelector('select')
        expect(Array.from(select.options).map(o => o.textContent)).toEqual(['Select category...', 'Old one', 'Computers'])
        expect(editor.getValue()).toBe('c9')

        const search = editor.element.querySelector('input[type=search]')
        search.value = 'Note'
        search.dispatchEvent(new Event('input'))
        await vi.advanceTimersByTimeAsync(350)
        expect(loadOptions).toHaveBeenLastCalledWith(expect.stringContaining('filter%5Bfilters%5D%5B0%5D%5Bvalue%5D=Note'), expect.anything())
        select.value = 'c2'
        expect(editor.getValue()).toBe('c2')
        expect(editor.getText()).toBe('Notebooks')
        vi.useRealTimers()
    })
})

describe('template lists', () => {
    it('prints arrays of strings comma separated', () => {
        const host = document.createElement('div')
        host.appendChild(compileTemplate('<span>{{ Warnings }}</span>').render({ Warnings: ['a', 'b'] }, {}))
        expect(host.textContent).toBe('a,b')
    })
})

describe('additional data', () => {
    it('leaves out fields whose value is undefined, like $.extend in the Kendo transport', async () => {
        window.gridSearch = () => ({ StartDate: '', CountryId: undefined, LoadNotShipped: false })
        const post = vi.fn(async () => ({ Data: [], Total: 0 }))
        const grid = createGrid({ transport: { read: '/list' }, additionalData: 'gridSearch', columns: [{ field: 'Id' }] }, post)
        await grid.ready
        await grid.dataSource.read()
        const payload = post.mock.calls[0][1]
        expect(payload).toMatchObject({ StartDate: '', LoadNotShipped: false })
        expect('CountryId' in payload).toBe(false)
        delete window.gridSearch
    })
})

describe('dataBound after Tabulator renders the rows again', () => {
    it('runs dataBound once per data change and again when the table re-renders on its own', async () => {
        const bound = vi.fn()
        window.onRowsBound = bound
        const grid = createGrid({ data: [{ Id: '1' }], events: { dataBound: 'onRowsBound' }, columns: [{ field: 'Id' }] })
        await grid.ready
        grid.table.handlers.renderComplete()
        expect(bound).not.toHaveBeenCalled()
        await grid.dataSource.read()
        await vi.waitFor(() => expect(bound).toHaveBeenCalledTimes(1))
        expect(bound.mock.calls[0][0].rerendered).toBe(false)
        //kendo.resize -> table.redraw(true) re-creates the cells
        grid.table.handlers.renderComplete()
        expect(bound).toHaveBeenCalledTimes(2)
        expect(bound.mock.calls[1][0].rerendered).toBe(true)
        delete window.onRowsBound
    })
})

describe('command visibility per button', () => {
    const buttons = row => Array.from(row.querySelectorAll('.grand-grid-command-buttons .btn'))
        .map(button => button.className.match(/grand-grid-(edit|delete|command)\b/)[1])

    function commandGrid(commands) {
        return createGrid({
            data: [
                { Id: '1', OpenQty: 2, Quantity: 2, IsShipEnabled: true },
                { Id: '2', OpenQty: 1, Quantity: 2, IsShipEnabled: true },
                { Id: '3', OpenQty: 0, Quantity: 2, IsShipEnabled: false },
                { Id: '4', OpenQty: 0, Quantity: 2, IsShipEnabled: true }
            ],
            editMode: 'Inline',
            texts: { edit: 'Edit', delete: 'Delete' },
            columns: [{ field: 'Quantity', editor: 'Integer' }],
            commands
        })
    }

    it('shows Edit and Delete by their own conditions, custom commands by theirs', async () => {
        const grid = commandGrid({
            edit: true,
            destroy: true,
            editVisibleIf: '!IsShipEnabled || OpenQty == Quantity',
            destroyVisibleIf: 'OpenQty > 0 && OpenQty == Quantity',
            custom: [{ name: 'cancelItem', text: 'Cancel', click: () => { }, visibleIf: 'OpenQty > 0' }]
        })
        await grid.ready
        await grid.dataSource.read()
        expect(rows(grid).map(buttons)).toEqual([
            ['edit', 'delete', 'command'],
            ['command'],
            ['edit'],
            []
        ])
    })

    it('keeps visible-if as the condition both Edit and Delete need', async () => {
        const grid = commandGrid({ edit: true, destroy: true, visibleIf: 'OpenQty > 0', editVisibleIf: 'OpenQty == Quantity' })
        await grid.ready
        await grid.dataSource.read()
        expect(rows(grid).map(buttons)).toEqual([['edit', 'delete'], ['delete'], [], []])
    })

    it('writes the empty text as text in a row that got no button, and only there', async () => {
        const grid = commandGrid({ edit: true, editVisibleIf: 'OpenQty > 0', emptyText: '<b>Shipped</b>' })
        await grid.ready
        await grid.dataSource.read()
        const empty = rows(grid).map(row => row.querySelector('.grand-grid-commands-empty'))
        expect(empty.map(node => node !== null)).toEqual([false, false, true, true])
        expect(empty[2].textContent).toBe('<b>Shipped</b>')
        expect(empty[2].querySelector('b')).toBeNull()
        expect(empty[2].className).toBe('text-muted small grand-grid-commands-empty')
    })

    it('leaves the cell empty without an empty text', async () => {
        const grid = commandGrid({ edit: true, editVisibleIf: 'OpenQty > 0' })
        await grid.ready
        await grid.dataSource.read()
        expect(rows(grid)[3].querySelector('.grand-grid-command-buttons').childNodes.length).toBe(0)
    })
})
