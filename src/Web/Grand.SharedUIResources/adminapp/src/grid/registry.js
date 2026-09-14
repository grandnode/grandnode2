import { GrandGrid } from './grid.js'
import { registerEditor } from './editors.js'

//window.GrandAdmin.grids: finds <div data-grand-grid='{json}'> elements rendered by the
//<admin-grid> tag helper, creates one GrandGrid per element and registers the Kendo
//compatible API under jQuery.data(el, 'kendoGrid') so existing callers keep working.

export function createRegistry({ Tabulator, doc = document }) {
    const grids = new Map()
    //culture and texts of the first <admin-grid> on the page, reused by shim grids
    const defaults = { culture: null, texts: {} }

    function create(element, config) {
        const existing = window.jQuery ? window.jQuery.data(element, 'kendoGrid') : null
        if (existing?.grandGrid) return existing
        const grid = new GrandGrid(element, config, { Tabulator })
        element.grandGrid = grid
        if (window.jQuery) window.jQuery.data(element, 'kendoGrid', grid.api)
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
            if (!defaults.culture && config.culture) {
                defaults.culture = config.culture
                defaults.texts = config.texts || {}
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
        /** Kendo-compatible API of a grid by element id. */
        get: id => grids.get(id)?.api,
        /** The GrandGrid instance (adapter internals) by element id. */
        instance: id => grids.get(id),
        all: () => Array.from(grids.values()).map(grid => grid.api),
        init,
        create,
        observe,
        defaults,
        /** Builds a GrandGrid without registering or loading it (used by the Kendo shim). */
        construct: (element, config) => new GrandGrid(element, config, { Tabulator }),
        editors: { register: registerEditor }
    }
}
