import { GrandGrid } from './grid.js'
import { registerEditor } from './editors.js'

//window.GrandAdmin.grids: finds <div data-grand-grid='{json}'> elements rendered by the
//<admin-grid> tag helper and creates one GrandGrid per element. A view reaches a grid's
//API with GrandAdmin.grids.get('#products-grid') - an id, a selector, an element or a
//jQuery object.

export function createRegistry({ Tabulator, doc = document }) {
    const grids = new Map()

    /** The element of a grid: 'id', '#id', any selector, an element or a jQuery object. */
    function elementOf(target) {
        if (!target) return null
        if (typeof target === 'string') {
            return /^#?[\w-]+$/.test(target) ? doc.getElementById(target.replace(/^#/, '')) : doc.querySelector(target)
        }
        if (target.jquery) return target[0] || null
        return target.nodeType === 1 ? target : null
    }

    function create(element, config) {
        if (element.grandGrid) return element.grandGrid.api
        const grid = new GrandGrid(element, config, { Tabulator })
        element.grandGrid = grid
        if (element.id) grids.set(element.id, grid)
        if (config.autoBind !== false) grid.ready.then(() => grid.dataSource.read())
        return grid.api
    }

    function init(root = doc) {
        const created = []
        const scope = root.querySelectorAll ? root : doc
        const elements = []
        if (scope.matches?.('[data-grand-grid]')) elements.push(scope)
        elements.push(...scope.querySelectorAll('[data-grand-grid]'))
        for (const element of elements) {
            if (element.grandGrid) continue
            let config
            try {
                config = JSON.parse(element.getAttribute('data-grand-grid'))
            } catch (error) {
                console.error(`[admin-grid] invalid configuration on #${element.id}`, error)
                continue
            }
            created.push(create(element, config))
        }
        return created
    }

    function observe() {
        if (typeof MutationObserver === 'undefined' || !doc.body) return
        //grids inside content added later (popups, appended tabs)
        new MutationObserver(mutations => {
            for (const mutation of mutations) {
                for (const node of mutation.addedNodes) {
                    if (node.nodeType === 1 && (node.matches('[data-grand-grid]') || node.querySelector('[data-grand-grid]'))) init(node)
                }
            }
        }).observe(doc.body, { childList: true, subtree: true })
    }

    return {
        /**
         * The API of a grid (dataSource.read/page, dataItem, select, saveChanges ...), found by
         * 'id', '#id', any selector, an element or a jQuery object; a detail grid too.
         */
        get: target => elementOf(target)?.grandGrid?.api,
        /** The GrandGrid instance (adapter internals) by element id. */
        instance: id => grids.get(id),
        all: () => Array.from(grids.values()).map(grid => grid.api),
        init,
        create,
        observe,
        editors: { register: registerEditor }
    }
}
