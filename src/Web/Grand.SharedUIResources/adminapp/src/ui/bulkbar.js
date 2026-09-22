//The bulk bar of a list screen: the strip that appears above a grid once rows are ticked and
//carries what can be done to them. The panels used to show those buttons at all times, next
//to "Add new", where they read as page actions and did nothing until a row was selected.
//
//The bar is rendered by <admin-bulkbar> as
//  <div class="grand-bulkbar" data-grand-bulkbar="products-grid">
//      <span class="grand-bulkbar__count" data-bulkbar-count="{0} selected">…</span>
//      <button data-bulkbar-action>…</button>
//and driven by the grid's own selection event (grid.js fires `grand-grid:selection` with
//`detail.selectedIds`), so it knows nothing about Tabulator.

const COUNT_PLACEHOLDER = '{0}'

/** The bar's grid: the id in data-grand-bulkbar, or the first grid after it. */
function findGrid(bar, doc) {
    const id = bar.getAttribute('data-grand-bulkbar')
    if (id) return doc.getElementById(id)
    return bar.parentElement?.querySelector('[data-role="grid"]') || null
}

export class BulkBar {
    constructor(element, options = {}) {
        this.element = element
        this.doc = options.doc || element.ownerDocument || document
        this.countElement = element.querySelector('[data-bulkbar-count]')
        this.template = this.countElement?.getAttribute('data-bulkbar-count') || ''
        this.selectedIds = []
        this.render()

        //the event bubbles, so listening on the document keeps working when the grid is
        //rebuilt (a tab shown, a page changed) and the element the bar saw is gone
        this._onSelection = event => {
            const grid = findGrid(this.element, this.doc)
            if (grid && event.target !== grid && !grid.contains(event.target)) return
            this.selectedIds = event.detail?.selectedIds || []
            this.render()
        }
        this.doc.addEventListener('grand-grid:selection', this._onSelection)
    }

    render() {
        const count = this.selectedIds.length
        this.element.classList.toggle('is-active', count > 0)
        if (this.countElement && this.template)
            this.countElement.textContent = this.template.replace(COUNT_PLACEHOLDER, String(count))
    }

    destroy() {
        this.doc.removeEventListener('grand-grid:selection', this._onSelection)
        delete this.element.grandBulkBar
    }
}

export function createBulkBar(element, options) {
    if (element.grandBulkBar) return element.grandBulkBar
    const widget = new BulkBar(element, options)
    element.grandBulkBar = widget
    return widget
}

export function getBulkBar(element) {
    return element?.grandBulkBar || null
}

export function initBulkBars(scope = document) {
    const elements = []
    if (scope.matches?.('[data-grand-bulkbar]')) elements.push(scope)
    elements.push(...(scope.querySelectorAll?.('[data-grand-bulkbar]') || []))
    for (const element of elements) createBulkBar(element)
    return elements.length
}
