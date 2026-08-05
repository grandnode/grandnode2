/*
 * The last Vue 2 prosthesis the Razor templates need.
 *
 * The `window` fall-through that used to live here is gone. Every view-model a
 * template names is declared on its island in both themes, and the globals those
 * templates genuinely call - document, window, location, localStorage, bootstrap,
 * hideTooltip - are installed explicitly in main.js. A name nothing provides is
 * now undefined rather than silently picked off `window`, which is what makes a
 * missing declaration visible instead of invisible.
 */

/*
 * Root-level props without a parent were plain reactive state in Vue 2; fold
 * them into data so Vue 3 does not treat them as props (it would warn and make
 * them read-only, and Theme.Modern's home.js declares its whole home-page model
 * that way).
 */
export function foldProps(options) {
    const opts = { ...options }
    if (!opts.props) return opts

    const extraData = {}
    const propsDef = opts.props
    if (Array.isArray(propsDef)) {
        propsDef.forEach(p => { extraData[p] = null })
    } else {
        Object.keys(propsDef).forEach(p => {
            const def = propsDef[p]
            extraData[p] = (def && typeof def === 'object' && 'default' in def) ? def.default : def ?? null
        })
    }
    const dataFn = opts.data
    opts.data = function () {
        const base = typeof dataFn === 'function' ? dataFn.call(this) : (dataFn || {})
        return { ...extraData, ...base }
    }
    delete opts.props
    return opts
}
