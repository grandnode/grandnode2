/*
 * The Vue 2 semantics the Razor templates still rely on.
 *
 * The `window` fall-through is on its last stretch. Grand.Web no longer needs it:
 * every view-model its templates name is declared on an island, and the globals
 * those templates genuinely call - document, window, location, bootstrap,
 * hideTooltip - are installed explicitly in main.js. Theme.Modern is not there
 * yet, so the fall-through stays and now *reports* each name it resolves.
 *
 * Removing it while Modern still depends on it is not a soft failure: an
 * undeclared view-model stops being silently fine and starts throwing
 * "Cannot read properties of undefined" out of the render function, which takes
 * the island down. Clear the warnings first, then delete this.
 */

/**
 * Rebuilds Vue 2's `with(this)` fall-through: an identifier the instance does not
 * have is looked up on `window`. `getOwnPropertyDescriptor` matters as much as
 * `get` - Vue tests for the key before it reads it.
 *
 * Every resolved name is reported once, with the island that needed it, so the
 * remaining work is a list rather than a guess. Silent while nothing uses it.
 */
const reported = new Set()

export function withWindowFallback(base) {
    const note = key => {
        if (reported.has(key)) return
        reported.add(key)
        console.warn(`[grand] "${key}" resolved through the window fall-through - ` +
            'declare it on the island (vue-island="' + key + '") or install it in main.js')
    }
    return new Proxy(base, {
        getOwnPropertyDescriptor(target, key) {
            const own = Reflect.getOwnPropertyDescriptor(target, key)
            if (own) return own
            if (typeof key === 'string' && key in window) {
                note(key)
                return { configurable: true, enumerable: false, value: window[key], writable: true }
            }
            return undefined
        },
        get(target, key) {
            if (key in target) return target[key]
            if (typeof key === 'string' && key in window) {
                note(key)
                return window[key]
            }
            return undefined
        }
    })
}

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
