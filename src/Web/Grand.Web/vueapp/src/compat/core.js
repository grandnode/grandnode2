/*
 * Vue 2 -> Vue 3 compatibility core.
 *
 * The storefront is a "hybrid" app: Razor renders in-DOM templates, and many
 * views create *unmounted* `new Vue({ data, methods })` instances that act as
 * global reactive view-models referenced from the templates by their global
 * variable name (Vue 2 `with(this)` scope fell through to `window`).
 *
 * The page used to be one Vue instance mounted on a #app that wrapped the whole
 * <body>. It is now a *shell* - one plain reactive object holding the state and
 * methods the chrome shares (cart, wishlist, compare, colour scheme, modals) -
 * plus a set of small apps mounted on the elements marked `vue-island`, each
 * using that shell as its data. See `defineShell` / `mountIslands`.
 *
 * This module recreates the Vue 2 semantics on top of Vue 3:
 *  - `LegacyVue(options)` (callable with `new`) builds a reactive state
 *    view-model. `el` is rejected - islands and `Vue.shell()` replaced it.
 *  - `LegacyVue.component()` collects global components registered by view
 *    scripts *before* the islands are created.
 *  - Template identifier lookup falls back to `window.*` via a Proxy placed
 *    over `app.config.globalProperties`.
 *
 * That last one is what actually keeps this file alive: around 65 .cshtml files
 * address their view-models by bare global name (`catalog.Model`,
 * `vmorder.cart`). The facade is a symptom - the templates living in Razor are
 * the cause - so retiring it means moving templates into components, not
 * rewriting the 22 `new Vue({ data })` call sites into `reactive()`.
 */
import { createApp, reactive, computed, watch, nextTick } from 'vue'

const pendingComponents = {}
const appInstalls = []
const rootReadyCallbacks = []
const beforeRootMountCallbacks = []
const islands = []
let rootVm = null

export function onAppCreate(fn) {
    appInstalls.push(fn)
}

/** The shell - the state and methods every island shares. */
export function getRootVm() {
    return rootVm
}

/**
 * Runs `fn` immediately before the islands are mounted - the deadline for
 * anything their templates need to already exist, such as the per-page
 * view-models. Hooking it here rather than to a footer script means every theme
 * gets it from loading the bundle; Theme.Modern has its own Head.cshtml and
 * would otherwise have had to remember to opt in.
 */
export function onBeforeRootMount(fn) {
    beforeRootMountCallbacks.push(fn)
}

/**
 * Runs `fn` once the shell exists and the islands are up. View-models are built
 * before that (they have to be - the island templates read them), so anything
 * that writes *into* rendered markup has to wait for this.
 */
export function onRootReady(fn) {
    if (rootVm) fn(rootVm)
    else rootReadyCallbacks.push(fn)
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

/*
 * Root-level props without a parent were used as plain reactive state in Vue 2;
 * fold them into data so Vue 3 does not treat them as props (it would warn and
 * make them read-only, and the theme shells declare half their cart state that
 * way).
 */
function foldProps(options) {
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

function makeApp(options) {
    const opts = foldProps(options)
    const app = createApp(opts)
    app.config.warnHandler = () => { /* keep console clean on legacy templates */ }
    Object.entries(pendingComponents).forEach(([name, def]) => app.component(name, def))
    appInstalls.forEach(fn => fn(app))
    app.config.globalProperties = windowFallbackGlobals(app.config.globalProperties)
    return app
}

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
    islands.forEach(vm => Object.keys(vm.$refs || {}).forEach(k => names.add(k)))
    return [...names]
}

const aggregatedRefs = new Proxy({}, {
    get(_, key) {
        for (let i = islands.length - 1; i >= 0; i--) {
            const value = islands[i].$refs?.[key]
            if (value != null) return value
        }
        return undefined
    },
    has(_, key) {
        return islands.some(vm => key in (vm.$refs || {}))
    },
    ownKeys() {
        return islandRefNames()
    },
    getOwnPropertyDescriptor() {
        return { configurable: true, enumerable: true }
    }
})

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
        $refs: { get: () => aggregatedRefs, configurable: true },
        $root: { get: () => rootVm, configurable: true },
        $forceUpdate: { value: () => islands.forEach(i => i.$forceUpdate()), configurable: true },
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

const ISLAND_ATTRIBUTE = 'vue-island'
const ISLAND_SELECTOR = `[${ISLAND_ATTRIBUTE}]`
const HOLE_ATTRIBUTE = 'data-vue-island-hole'
let holeSeq = 0

/**
 * Mounts a small app on every element marked `vue-island` that has not got one
 * yet. Each island compiles only its own subtree, and shares the shell object as
 * its data, so the header, the drawers and the page body all read and write the
 * same `flycart`, `wishindicator`, `darkMode` and so on.
 *
 * Islands may be nested; see the note below on how the inner ones are carved
 * out of their parent rather than compiled twice.
 *
 * A template that fails to compile takes down its own island and nothing else -
 * the whole page used to go with it.
 */
export function mountIslands(root = document) {
    if (!rootVm) return
    /*
     * Outermost first, one level at a time.
     *
     * A nested island is lifted out of its parent before the parent compiles,
     * and put back afterwards: the parent renders a bare placeholder in its
     * place, so the inner markup is never compiled twice and a syntax error
     * inside it cannot take the parent down - a broken review block stops at
     * the review block. The inner islands can only mount on the next pass,
     * once their parent has rendered the placeholder they slot back into.
     *
     * Lifting rather than `v-pre`: v-pre does stop the parent compiling the
     * subtree, but the parent then re-creates it with setAttribute, and `@click`
     * is not a valid HTML attribute name - the DOMException took out the whole
     * parent island.
     *
     * Nesting is normal, not a mistake: a partial can be standalone in one
     * theme and nested in the next (the newsletter block is its own island in
     * Grand.Web and sits inside the footer in Theme.Modern), and the product
     * page renders reviews, ask-a-question and the related-product grids inside
     * the add-to-cart form.
     */
    const hasPendingAncestor = el => {
        for (let a = el.parentElement?.closest(ISLAND_SELECTOR); a; a = a.parentElement?.closest(ISLAND_SELECTOR)) {
            if (!a.__vueIsland) return true
        }
        return false
    }

    for (let depth = 0; depth < 10; depth++) {
        const outermost = [...root.querySelectorAll(ISLAND_SELECTOR)]
            .filter(el => !el.__vueIsland && !hasPendingAncestor(el))
        if (!outermost.length) return

        const holes = new Map()
        outermost.forEach(el => {
            el.querySelectorAll(ISLAND_SELECTOR).forEach(inner => {
                const key = 'h' + (holeSeq++)
                // same tag and classes, so the parent lays out as it will once
                // the real subtree is back
                const placeholder = document.createElement(inner.tagName)
                placeholder.className = inner.className
                placeholder.setAttribute(HOLE_ATTRIBUTE, key)
                holes.set(key, inner)
                inner.replaceWith(placeholder)
            })
            el.__vueIsland = true
            try {
                const app = makeApp({ data: () => rootVm })
                islands.push(app.mount(el))
            } catch (err) {
                console.error('[grand] island failed to mount', el, err)
            }
        })

        // back into whatever the parent rendered; a placeholder that never made
        // it into the output (a v-if that was false) simply drops its island
        holes.forEach((node, key) => {
            const placeholder = root.querySelector(`[${HOLE_ATTRIBUTE}="${key}"]`)
            if (placeholder) placeholder.replaceWith(node)
        })
    }
}

/**
 * Builds the shared shell and brings the islands up.
 *
 * Replaces `new Vue({ el: '#app', ... })`: the same options object, but the data
 * and methods become one reactive object instead of a component instance, and
 * the markup is compiled per island rather than over the whole document.
 */
export function defineShell(options = {}) {
    if (rootVm) return rootVm

    rootVm = makeStateVm(foldProps(options))
    // the view scripts, the colour-scheme snippet in <head> and the Razor
    // templates all address it as `vm`; publish it before anything can look
    window.vm = rootVm

    // per-page view-models first - the island templates reference them by name
    beforeRootMountCallbacks.splice(0).forEach(fn => fn())
    mountIslands()
    rootReadyCallbacks.splice(0).forEach(fn => fn(rootVm))
    return rootVm
}

export function LegacyVue(options = {}) {
    /*
     * `el` is not supported any more. Nothing in the storefront mounts that way
     * since the shell replaced the single #app root, but window.Vue is a public
     * surface a plugin could still call - and silently building an unmounted
     * view-model instead would look like "my component renders nothing".
     */
    if (options.el) {
        console.error('[grand] new Vue({ el }) is gone: mark the element `vue-island` ' +
            'and put shared state in Vue.shell(), or use Vue.createApp(...).mount(el)', options.el)
        return null
    }
    return makeStateVm(options)
}

LegacyVue.component = function (name, def) {
    pendingComponents[name] = def
}
LegacyVue.createApp = makeApp
LegacyVue.nextTick = nextTick
LegacyVue.reactive = reactive
LegacyVue.shell = defineShell
LegacyVue.mountIslands = mountIslands

export default LegacyVue
