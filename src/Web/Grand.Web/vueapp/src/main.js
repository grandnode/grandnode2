/*
 * Storefront bundle entry (Vue 3 + Bootstrap 5).
 *
 * Bootstrap's own JavaScript drives modals, offcanvas drawers, collapse, tabs,
 * dropdowns, tooltips and carousels straight from the data-bs-* attributes the
 * Razor views render. Vue is left with the pieces that are genuinely dynamic.
 *
 * Exposes `window.Vue` - a Vue 2 style compatibility facade (see compat/core.js)
 * used by the Razor views and theme scripts - plus the shared globals
 * (bootstrap, axios, Pikaday, $bvToast).
 */
import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-icons/font/bootstrap-icons.css'
import 'animate.css'
import 'pikaday/css/pikaday.css'
import './compat/compat.css'

import * as bootstrap from 'bootstrap'
import axios from 'axios'
import Pikaday from 'pikaday'

import LegacyVue, { onAppCreate, onBeforeRootMount } from './compat/core'
import { registerBvComponents, VueGallerySlideshow } from './compat/bv-components'
import { registerValidation, veeGetMessage } from './compat/validate'
import { $bvToast } from './compat/bv-services'
import { initViews } from './views'

// Storefront helpers that used to be classic <script src> tags ordered by hand
// in Head.cshtml. They publish the globals the Razor markup still calls.
import './theme/common'
import './theme/axios-cart'
import './theme/push-notifications'
import './behaviours/advanced-search'
import './behaviours/attribute-forms'
import './behaviours/bar-notifications'
import './behaviours/checkout-steps'
import './behaviours/js-resources'
import './behaviours/confirm-delete'
import './behaviours/confirm-post'
import './behaviours/estimate-shipping'
import './behaviours/cookie-bar'
import './behaviours/geolocation'
import './behaviours/in-dom-components'
import './behaviours/password-page'
import './behaviours/product-attributes-bundle'
import './behaviours/product-gallery'
import './behaviours/push-notifications'
import './behaviours/quantity-stepper'
import './behaviours/quick-view-modal'
import './behaviours/reservation-info'
import './behaviours/search-modal'
import './behaviours/two-columns-sidebar'
import './behaviours/toggles'
import './behaviours/username-availability'
import './behaviours/warehouse-selector'

import './views/state'
import './views/apply-vendor'
import './views/ask-question'
import './views/catalog'
import './views/catalog-modern'
import './views/comments'
import './views/compare-products'
import './views/contact-form'
import './views/country-state-form'
import './views/globals'
import './views/merchandise-return'
import './views/out-of-stock-subscription'
import './views/product-attributes'
import './views/reviews'
import './views/scroll-pagination'
import './views/search-box'
import './views/shopping-cart'
import './views/vendor-review-overview'
import './views/voice-navigation'
import './views/wishlist'

onAppCreate(app => {
    registerBvComponents(app)
    registerValidation(app)
    app.config.globalProperties.$bvToast = $bvToast
    // warnHandler is silenced (legacy in-DOM templates trigger noisy dev
    // warnings), but real render/setup errors must stay visible - otherwise
    // Vue silently swallows them and renders an empty comment node instead.
    app.config.errorHandler = (err, instance, info) => console.error(err, info)
})

// One delegated Tooltip instance covers the whole page, including markup Vue
// renders later - a per-element init would miss everything added after load.
function initTooltips() {
    if (document.body.__tooltipDelegate) return
    document.body.__tooltipDelegate = new bootstrap.Tooltip(document.body, {
        selector: '[data-bs-toggle="tooltip"]'
    })
}
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initTooltips)
} else {
    initTooltips()
}

/*
 * Per-page view-models are built from the [data-grand-vm] JSON islands. Two
 * triggers, both idempotent:
 *  - right before the root #app is mounted, which is the deadline (the root
 *    template addresses the view-models by their global name);
 *  - on DOMContentLoaded, so islands on a layout that never mounts a root app
 *    still come up.
 * Also exposed on window for markup injected after load.
 */
onBeforeRootMount(() => initViews())
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => initViews())
} else {
    initViews()
}
window.grandInitViews = initViews

window.bootstrap = bootstrap
window.Vue = LegacyVue
window.axios = axios
window.Pikaday = Pikaday
window.VueGallerySlideshow = VueGallerySlideshow
window.$bvToast = $bvToast
window.vee_getMessage = veeGetMessage
