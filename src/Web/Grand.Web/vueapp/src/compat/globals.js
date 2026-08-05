/*
 * The two Vue 2 semantics the Razor templates still rely on.
 *
 * Both live here rather than in runtime/ because both are prostheses, not
 * architecture: they exist only because the templates are .cshtml files written
 * against Vue 2, and both can go the day the last of those templates becomes a
 * component. Nothing else in the bundle needs them.
 */

/**
 * Vue 2 compiled templates with `with(this)`, so an identifier the instance did
 * not have fell through to `window`. Around 65 .cshtml files address their
 * view-model by bare global name (`catalog.Model`, `vmorder.cart`), so the same
 * fall-through is rebuilt as a Proxy over `app.config.globalProperties`.
 *
 * `getOwnPropertyDescriptor` matters as much as `get`: the Vue 3 compiler tests
 * for the key before it reads it.
 */
export function withWindowFallback(base) {
    return new Proxy(base, {
        getOwnPropertyDescriptor(target, key) {
            const own = Reflect.getOwnPropertyDescriptor(target, key)
            if (own) return own
            if (typeof key === 'string' && key in window) {
                return { configurable: true, enumerable: false, value: window[key], writable: true }
            }
            return undefined
        },
        get(target, key) {
            if (key in target) return target[key]
            if (typeof key === 'string' && key in window) return window[key]
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
