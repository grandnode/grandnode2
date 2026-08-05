/*
 * Helpers every behaviour here needs.
 *
 * Behaviours run from the island registry, which fires *before* the root #app
 * is mounted. Almost all storefront markup is inside #app, and Vue replaces
 * that whole subtree when it mounts - so a listener attached directly to an
 * element at registration time is thrown away, and a DOM change made then is
 * read back as part of the template. Both of these avoid that.
 */
import { onRootReady } from '../runtime/islands'

/** Runs `fn` once the root app has rendered its markup. */
export function onReady(fn) {
    onRootReady(root => root.$nextTick(fn))
}

/**
 * Binds `handler` to every current and future element matching `selector`.
 * Delegated from the document, so it survives Vue re-rendering the element.
 */
export function delegate(type, selector, handler) {
    document.addEventListener(type, event => {
        const element = event.target.closest?.(selector)
        if (element) handler(element, event)
    })
}
