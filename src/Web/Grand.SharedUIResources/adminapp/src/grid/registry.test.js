// @vitest-environment jsdom
import { afterEach, describe, expect, it } from 'vitest'
import { createRegistry } from './registry.js'

//Just enough of Tabulator for a grid to be built and never shown.
class FakeTabulator {
    constructor(element, options) {
        this.options = options
        this.handlers = {}
        setTimeout(() => this.handlers.tableBuilt?.(), 0)
    }

    on(name, handler) {
        this.handlers[name] = handler
    }

    getColumns() {
        return []
    }

    getRows() {
        return []
    }

    async setData() { }

    redraw() { }

    destroy() { }
}

function gridElement(id) {
    const element = document.createElement('div')
    element.id = id
    element.setAttribute('data-grand-grid', JSON.stringify({ autoBind: false, columns: [{ field: 'Name' }] }))
    document.body.appendChild(element)
    return element
}

afterEach(() => {
    document.body.textContent = ''
    delete window.jQuery
})

describe('GrandAdmin.grids.get', () => {
    it('finds a grid by id, #id, selector, element and jQuery object', () => {
        const registry = createRegistry({ Tabulator: FakeTabulator })
        const element = gridElement('products-grid')
        element.setAttribute('data-id', 'p')
        const [api] = registry.init(document)

        expect(registry.get('products-grid')).toBe(api)
        expect(registry.get('#products-grid')).toBe(api)
        expect(registry.get('[data-id="p"]')).toBe(api)
        expect(registry.get(element)).toBe(api)
        expect(registry.get({ jquery: '3.7.1', 0: element, length: 1 })).toBe(api)
        expect(api.dataSource.read).toBeTypeOf('function')
    })

    it('answers undefined for what is not a grid', () => {
        const registry = createRegistry({ Tabulator: FakeTabulator })
        gridElement('products-grid')
        registry.init(document)
        const plain = document.createElement('div')
        document.body.appendChild(plain)

        expect(registry.get('missing-grid')).toBeUndefined()
        expect(registry.get(plain)).toBeUndefined()
        expect(registry.get(null)).toBeUndefined()
        expect(registry.get({ jquery: '3.7.1', length: 0 })).toBeUndefined()
    })

    it('no longer registers the grid under jQuery.data', () => {
        const stored = []
        window.jQuery = { data: (...args) => { stored.push(args); return undefined } }
        const registry = createRegistry({ Tabulator: FakeTabulator })
        gridElement('products-grid')
        registry.init(document)

        expect(stored.filter(args => args[1] === 'kendoGrid')).toEqual([])
    })
})
