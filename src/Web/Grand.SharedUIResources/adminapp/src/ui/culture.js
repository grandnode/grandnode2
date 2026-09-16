//The request culture of the panel, emitted once per page by the <admin-culture> tag helper
//(Grand.Web.Common/TagHelpers/Admin/AdminCultureTagHelper.cs) as a JSON island. It is the
//same GridCulture shape the <admin-grid> tag helper puts in data-grand-grid, so the widgets
//and the grid format numbers and dates identically. Replaces kendo.culture(...).

import { normalizeCulture } from '../grid/format.js'

let cached = null

/** Reads the page culture, falling back to the invariant defaults when the island is absent. */
export function pageCulture(doc = globalThis.document) {
    if (cached) return cached
    let data = null
    const element = doc?.getElementById?.('grand-admin-culture')
    if (element) {
        try {
            data = JSON.parse(element.textContent)
        } catch (error) {
            console.error('[admin-ui] invalid culture data', error)
        }
    }
    cached = normalizeCulture(data)
    return cached
}

/** Test seam: forgets the culture read from the document. */
export function resetCulture() {
    cached = null
}
