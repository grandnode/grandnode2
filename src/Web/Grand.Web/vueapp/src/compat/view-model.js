/*
 * Options-API shaped view-models, on top of Vue 3 reactivity.
 *
 * The storefront is a hybrid app: Razor renders the templates, and the view
 * scripts create *unmounted* view-models that act as global reactive state,
 * referenced from those templates by their global variable name. `data`,
 * `methods`, `computed`, `watch` and `created` are the shape all ~27 call sites
 * (22 in views/, 5 in the theme script files) are written in, so they are kept
 * rather than rewritten one by one into `reactive()`.
 *
 * This is compat, not architecture: it is the Vue 2 instance API minus the
 * instance. Rewriting the call sites would retire this file; retiring the
 * *globals* means moving the templates out of Razor - see ./globals.js.
 */
import { reactive, computed, watch, nextTick } from 'vue'
import { foldProps } from './globals'
import { bringUpIslands, getIslands, getRootVm } from '../runtime/islands'

/*
 * `$refs` as the page used to know it.
 *
 * With a single root instance every `ref` on the page landed in one object, and
 * the view scripts read them from anywhere - the search box reaches for
 * `searchForm`, the shell for `wishlistQty`, the product page for `swiperTop`.
 * Now that refs are split across the islands that own them, this proxy searches
 * all of them, so `vm.$refs.x` keeps meaning "the element called x on this page".
 *
 * Later islands win: a ref name used both on the page and inside the quick-view
 * modal (product-details-form ids are duplicated that way) resolves to the modal,
 * which is the one the visitor is looking at.
 */
function islandRefNames() {
    const names = new Set()
    getIslands().forEach(vm => Object.keys(vm.$refs || {}).forEach(k => names.add(k)))
    return [...names]
}

const aggregatedRefs = new Proxy({}, {
    get(_, key) {
        const islands = getIslands()
        for (let i = islands.length - 1; i >= 0; i--) {
            const value = islands[i].$refs?.[key]
            if (value != null) return value
        }
        return undefined
    },
    has(_, key) {
        return getIslands().some(vm => key in (vm.$refs || {}))
    },
    ownKeys() {
        return islandRefNames()
    },
    getOwnPropertyDescriptor() {
        return { configurable: true, enumerable: true }
    }
})

/**
 * Builds a reactive view-model from Vue 2 style options: reactive data, bound
 * methods, computed, watch and the created/mounted hooks. Enough for the view
 * scripts, which use these objects as global state plus method containers.
 */
export function createViewModel(options = {}) {
    const opts = foldProps(options)
    const data = typeof opts.data === 'function' ? opts.data.call({}) : (opts.data || {})
    const vm = reactive({ ...data })

    if (opts.methods) {
        Object.entries(opts.methods).forEach(([name, fn]) => {
            vm[name] = fn.bind(vm)
        })
    }
    if (opts.computed) {
        Object.entries(opts.computed).forEach(([name, def]) => {
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
        $nextTick: { value: fn => nextTick(fn), configurable: true },
        $refs: { get: () => aggregatedRefs, configurable: true },
        $root: { get: () => getRootVm(), configurable: true },
        // Theme.Modern's app.js still forces a redraw after it rewrites the cart
        $forceUpdate: { value: () => getIslands().forEach(i => i.$forceUpdate()), configurable: true },
        $bvToast: { get: () => window.$bvToast, configurable: true }
    })

    if (opts.watch) {
        Object.entries(opts.watch).forEach(([key, def]) => {
            const handler = typeof def === 'function' ? def : def.handler
            const opt = typeof def === 'function' ? {} : def
            watch(() => key.split('.').reduce((o, k) => (o == null ? o : o[k]), vm),
                handler.bind(vm), opt)
        })
    }

    if (opts.created) opts.created.call(vm)
    if (opts.mounted) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => opts.mounted.call(vm))
        } else {
            nextTick(() => opts.mounted.call(vm))
        }
    }
    return vm
}

/**
 * Builds the shared shell and brings the islands up.
 *
 * Replaces `new Vue({ el: '#app', ... })`: the same options object, but the data
 * and methods become one reactive object instead of a component instance, and
 * the markup is compiled per island rather than over the whole document.
 */
export function defineShell(options = {}) {
    return getRootVm() || bringUpIslands(createViewModel(options))
}
