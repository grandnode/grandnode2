//The request culture of the panel, emitted once per page by the <admin-culture> tag helper
//(Grand.Web.Common/TagHelpers/Admin/AdminCultureTagHelper.cs) as a JSON island. It is the
//same GridCulture shape the <admin-grid> tag helper puts in data-grand-grid, so the widgets
//and the grid format numbers and dates identically. Replaces kendo.culture(...).

import { normalizeCulture } from '../grid/format.js'

let cached = null
let cachedTexts = null

function read(doc) {
    const element = doc?.getElementById?.('grand-admin-culture')
    if (!element) return null
    try {
        return JSON.parse(element.textContent)
    } catch (error) {
        console.error('[admin-ui] invalid culture data', error)
        return null
    }
}

/** Reads the page culture, falling back to the invariant defaults when the island is absent. */
export function pageCulture(doc = globalThis.document) {
    if (cached) return cached
    cached = normalizeCulture(read(doc))
    return cached
}

/**
 * The widget texts the island carries (today, clear, cancel, the calendar's labels), by the
 * name the tag helper writes them under. A resource an installation never imported is simply
 * missing, and the caller falls back to its own neutral default.
 */
export function pageTexts(doc = globalThis.document) {
    if (cachedTexts) return cachedTexts
    const data = read(doc)
    cachedTexts = data && typeof data.texts === 'object' && data.texts ? data.texts : {}
    return cachedTexts
}

/** Test seam: forgets the culture read from the document. */
export function resetCulture() {
    cached = null
    cachedTexts = null
}

/** The elements under a root that match a selector, the root itself included. */
export function collect(root, selector) {
    const scope = root?.querySelectorAll ? root : globalThis.document
    const found = []
    if (scope.matches?.(selector)) found.push(scope)
    found.push(...scope.querySelectorAll(selector))
    return found
}
