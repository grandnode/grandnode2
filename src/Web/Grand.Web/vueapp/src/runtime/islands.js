/*
 * How the storefront runs Vue. This is the current architecture, not a leftover:
 * see ../compat for the parts that only exist to keep Vue 2 era templates alive.
 *
 * The page used to be one Vue instance mounted on an #app that wrapped the whole
 * <body>. It is now a *shell* - one plain reactive object holding the state and
 * methods the chrome shares (cart, wishlist, compare, colour scheme, modals) -
 * plus a set of small apps mounted on the elements marked `vue-island`, each
 * using that shell as its data. A template that fails to compile takes down its
 * own island and nothing else; the whole page used to go with it.
 */
import { createApp } from 'vue'
import { withWindowFallback } from '../compat/globals'

const pendingComponents = {}
const appInstalls = []
const rootReadyCallbacks = []
const beforeRootMountCallbacks = []
const islands = []
const viewModels = {}
let rootVm = null

/**
 * Publishes a per-page view-model under a name an island can ask for.
 *
 * Templates address these by bare name (`applyvendor.Email`). Today that name is
 * resolved by the `window` fall-through Proxy in compat/globals.js; an island
 * that declares `vue-island="applyvendor"` gets the same object as ordinary
 * component data instead, and needs no fall-through. Once every island declares
 * what it uses, that Proxy can go.
 */
export function registerViewModel(name, vm) {
    viewModels[name] = vm
    return vm
}

/** Runs `fn` against every island app as it is created (plugins, globals). */
export function onAppCreate(fn) {
    appInstalls.push(fn)
}

/**
 * Registers a global component. Islands do not exist yet when the view scripts
 * run, so the definitions are collected and handed to each app on creation.
 */
export function registerComponent(name, definition) {
    pendingComponents[name] = definition
}

/** The shell - the state and methods every island shares. */
export function getRootVm() {
    return rootVm
}

/** Every mounted island, outermost first. */
export function getIslands() {
    return islands
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

/** Creates an app carrying the storefront's components, plugins and globals. */
export function createStorefrontApp(options) {
    const app = createApp(options)
    // In-DOM templates written for Vue 2 trigger noisy dev warnings; real
    // render/setup errors go through errorHandler (see main.js) and stay visible.
    app.config.warnHandler = () => {}
    Object.entries(pendingComponents).forEach(([name, def]) => app.component(name, def))
    appInstalls.forEach(fn => fn(app))
    app.config.globalProperties = withWindowFallback(app.config.globalProperties)
    return app
}

const ISLAND_ATTRIBUTE = 'vue-island'

/*
 * The data an island renders against.
 *
 * Plain islands get the shell object itself, so every one of them reads and
 * writes the same reactive state. An island that names view-models in its
 * `vue-island` attribute gets a view over the shell with those names layered on
 * top - reads fall through to the shell, writes still land on it, so sharing is
 * unaffected. Returning a wrapper rather than a copy is the point: `{ ...shell }`
 * would give each island a private snapshot and the drawers would stop agreeing.
 */
function islandData(el) {
    const names = (el.getAttribute(ISLAND_ATTRIBUTE) || '')
        .split(/[\s,]+/).filter(Boolean)
    if (!names.length) return rootVm

    const declared = {}
    names.forEach(name => {
        if (viewModels[name]) declared[name] = viewModels[name]
        else console.warn('[grand] island asked for an unknown view-model', name, el)
    })

    return new Proxy(rootVm, {
        get: (target, key) => (key in declared ? declared[key] : target[key]),
        has: (target, key) => key in declared || key in target,
        set: (target, key, value) => {
            if (key in declared) return false
            target[key] = value
            return true
        },
        ownKeys: target => [...new Set([...Reflect.ownKeys(target), ...Object.keys(declared)])],
        getOwnPropertyDescriptor: (target, key) =>
            key in declared
                ? { configurable: true, enumerable: true, writable: false, value: declared[key] }
                : Reflect.getOwnPropertyDescriptor(target, key)
    })
}

const ISLAND_SELECTOR = `[${ISLAND_ATTRIBUTE}]`
const HOLE_ATTRIBUTE = 'data-vue-island-hole'
let holeSeq = 0

/**
 * Mounts a small app on every element marked `vue-island` that has not got one
 * yet. Each island compiles only its own subtree, and shares the shell object as
 * its data, so the header, the drawers and the page body all read and write the
 * same `flycart`, `wishindicator`, `darkMode` and so on.
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
                const data = islandData(el)
                const app = createStorefrontApp({ data: () => data })
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
 * Adopts `vm` as the shell and brings the page up. Called once per page, by
 * `Vue.shell()` - the themes' entry point.
 */
export function bringUpIslands(vm) {
    if (rootVm) return rootVm
    rootVm = vm
    // the view scripts, the colour-scheme snippet in <head> and the Razor
    // templates all address it as `vm`; publish it before anything can look
    window.vm = rootVm

    // per-page view-models first - the island templates reference them by name
    beforeRootMountCallbacks.splice(0).forEach(fn => fn())
    mountIslands()
    rootReadyCallbacks.splice(0).forEach(fn => fn(rootVm))
    return rootVm
}
