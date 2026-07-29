/*
 * Vue 2 -> Vue 3 compatibility core.
 *
 * The storefront is a "hybrid" app: Razor renders in-DOM templates, footer
 * scripts create the root instance via `new Vue({ el: '#app', ... })` and many
 * views create *unmounted* `new Vue({ data, methods })` instances that act as
 * global reactive view-models referenced from the root template by their
 * global variable name (Vue 2 `with(this)` scope fell through to `window`).
 *
 * This module recreates those semantics on top of Vue 3:
 *  - `LegacyVue(options)` (callable with `new`) either mounts a real app
 *    (when `options.el` is given) or builds a reactive state view-model.
 *  - `LegacyVue.component()` collects global components registered by view
 *    scripts *before* the root app is created.
 *  - Template identifier lookup falls back to `window.*` via a Proxy placed
 *    over `app.config.globalProperties`.
 */
import { createApp, reactive, computed, watch, nextTick } from 'vue'

const pendingComponents = {}
const appInstalls = []
let rootVm = null

export function onAppCreate(fn) {
    appInstalls.push(fn)
}

export function getRootVm() {
    return rootVm
}

function windowFallbackGlobals(base) {
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

function makeApp(options) {
    // Root-level props without a parent were used as plain reactive state in
    // Vue 2; fold them into data so Vue 3 does not treat them as props.
    const opts = { ...options }
    if (opts.props) {
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
    }
    const app = createApp(opts)
    app.config.warnHandler = () => { /* keep console clean on legacy templates */ }
    Object.entries(pendingComponents).forEach(([name, def]) => app.component(name, def))
    appInstalls.forEach(fn => fn(app))
    app.config.globalProperties = windowFallbackGlobals(app.config.globalProperties)
    return app
}

/* A minimal re-implementation of an unmounted Vue 2 instance: reactive data,
 * bound methods, computed, watch and the created hook. Enough for the view
 * scripts which use these objects as global state + method containers. */
function makeStateVm(options) {
    const data = typeof options.data === 'function' ? options.data.call({}) : (options.data || {})
    const vm = reactive({ ...data })

    if (options.methods) {
        Object.entries(options.methods).forEach(([name, fn]) => {
            vm[name] = fn.bind(vm)
        })
    }
    if (options.computed) {
        Object.entries(options.computed).forEach(([name, def]) => {
            const c = typeof def === 'function' ? computed(def.bind(vm)) : computed({
                get: def.get.bind(vm),
                set: def.set ? def.set.bind(vm) : undefined
            })
            Object.defineProperty(vm, name, {
                get: () => c.value,
                set: v => { c.value = v },
                enumerable: true,
                configurable: true
            })
        })
    }

    Object.defineProperties(vm, {
        $mount: { value: () => vm, configurable: true },
        $nextTick: { value: fn => nextTick(fn), configurable: true },
        $refs: { get: () => (rootVm ? rootVm.$refs : {}), configurable: true },
        $root: { get: () => rootVm, configurable: true },
        $bvToast: { get: () => window.$bvToast, configurable: true },
        $bvModal: { get: () => window.$bvModal, configurable: true },
        $watch: { value: (src, cb, opt) => watch(() => vm[src], cb, opt), configurable: true }
    })

    if (options.watch) {
        Object.entries(options.watch).forEach(([key, def]) => {
            const handler = typeof def === 'function' ? def : def.handler
            const opt = typeof def === 'function' ? {} : def
            watch(() => key.split('.').reduce((o, k) => (o == null ? o : o[k]), vm),
                handler.bind(vm), opt)
        })
    }

    if (options.created) options.created.call(vm)
    if (options.mounted) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => options.mounted.call(vm))
        } else {
            nextTick(() => options.mounted.call(vm))
        }
    }
    return vm
}

export function LegacyVue(options = {}) {
    if (options.el) {
        const el = options.el
        const opts = { ...options }
        delete opts.el
        const app = makeApp(opts)
        const instance = app.mount(el)
        if (el === '#app' || (el.id === 'app')) rootVm = instance
        return instance
    }
    return makeStateVm(options)
}

LegacyVue.component = function (name, def) {
    pendingComponents[name] = def
}
LegacyVue.extend = function (options) {
    return function () {
        return makeStateVm(options || {})
    }
}
LegacyVue.createApp = makeApp
LegacyVue.nextTick = nextTick
LegacyVue.reactive = reactive

export default LegacyVue
