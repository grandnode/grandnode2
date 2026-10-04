/*
 * Per-page view-model registry.
 *
 * The storefront used to declare its Vue view-models in <script> blocks inside
 * the .cshtml files, with Razor interpolating route URLs and localized strings
 * straight into the JavaScript. That code was outside the bundle, so it was
 * never linted, never minified and never cached - broken callbacks shipped
 * unnoticed for a long time.
 *
 * Now a view emits data only, as a JSON island:
 *
 *   <script type="application/json" data-grand-vm="shoppingCart">
 *       { "model": ..., "routes": ..., "res": ... }
 *   </script>
 *
 * and the matching factory registered here builds the view-model. `initViews`
 * is called from the footer (see Partials/JsResources.cshtml) so the globals
 * exist before app.js mounts the root instance on #app - the root template
 * references them by name.
 */

const registry = {}

export function registerView(name, factory) {
    registry[name] = factory
}

/**
 * Reads the JSON payload of a data island. Kept separate so a malformed
 * payload names the view that produced it instead of failing anonymously
 * somewhere inside JSON.parse.
 */
function readPayload(el) {
    const raw = el.textContent.trim()
    if (!raw) return {}
    try {
        return JSON.parse(raw)
    } catch (err) {
        console.error(`[grand] view "${el.dataset.grandVm}" has an unparseable payload`, err)
        return null
    }
}

const ISLAND = 'script[type="application/json"][data-grand-vm]'

/**
 * Collects islands including those nested in <template> elements.
 *
 * The checkout wraps whole steps in `<template v-if>`, and a <template>'s
 * children live in its inert `.content` fragment - querySelectorAll does not
 * descend into it. The old inline scripts were hoisted out to the footer by the
 * asp-location tag helper, so they never hit this; a data island stays where the
 * view put it.
 */
function collectIslands(root, found = []) {
    root.querySelectorAll(ISLAND).forEach(el => found.push(el))
    root.querySelectorAll('template').forEach(template => collectIslands(template.content, found))
    return found
}

export function initViews(root = document) {
    collectIslands(root).forEach(el => {
        // guards against a second init pass over markup that is already live
        if (el.dataset.grandVmReady) return

        const name = el.dataset.grandVm
        const factory = registry[name]
        if (!factory) {
            console.error(`[grand] no view-model registered for "${name}"`)
            return
        }

        const payload = readPayload(el)
        if (payload === null) return

        el.dataset.grandVmReady = '1'
        factory(payload, el)
    })
}
