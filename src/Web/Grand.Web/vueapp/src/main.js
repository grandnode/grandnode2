/*
 * Storefront bundle entry (Vue 3 + Bootstrap 5).
 *
 * Bootstrap's own JavaScript drives modals, offcanvas drawers, collapse, tabs,
 * dropdowns, tooltips and carousels straight from the data-bs-* attributes the
 * Razor views render. Vue is left with the pieces that are genuinely dynamic.
 *
 * Exposes `window.Vue` - the public Vue surface the Razor views and the theme
 * script files use (see compat/vue-global.js) - plus the shared globals
 * (bootstrap, axios, Pikaday, $bvToast).
 */
import 'bootstrap/dist/css/bootstrap.css'
// The full icon set, deliberately: category, brand and collection icons are stored
// per record in the database and rendered as `bi bi-@Model.Category.Icon`, so a
// build-time subset scanned from the views would blank out whatever a merchant had
// configured.
import 'bootstrap-icons/font/bootstrap-icons.css'
// animate.css is not here on purpose: 95 kB of keyframes that only Theme.Modern
// used, and only for fadeIn - it now ships its own subset from
// Plugins/Theme.Modern/Content/css/animate-subset.css
import 'pikaday/css/pikaday.css'
import './compat/compat.css'

import * as bootstrap from 'bootstrap'
import axios from 'axios'
import Pikaday from 'pikaday'

import { onAppCreate, onBeforeRootMount, mountIslands, getRootVm } from './runtime/islands'
import StorefrontVue from './compat/vue-global'
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

/*
 * The globals a Razor template may name in a Vue expression.
 *
 * These used to arrive through a Proxy that forwarded *any* miss to `window`.
 * That made every undeclared view-model work by accident, which is exactly how
 * missing island declarations stayed invisible, and it turned a typo into
 * `undefined` instead of a warning. The list is short and now explicit; a name
 * that is not here is a mistake, and Vue says so.
 *
 * `document`, `window` and `location` are here because Vue's own allowed-globals
 * list does not include them - only Math, JSON, console and friends - so a
 * template calling document.getElementById() resolves it as an instance property.
 */
function installTemplateGlobals(app) {
    /*
     * `vm` is layered into every island's data, but an app made with
     * Vue.createApp() - the popups displayPopup renders - is not an island and
     * gets no layering, so it needs the shell here too. A getter because the
     * shell does not exist yet when the first apps are created.
     */
    Object.defineProperty(app.config.globalProperties, 'vm', {
        get: () => getRootVm(),
        configurable: true
    })
    Object.assign(app.config.globalProperties, {
        window,
        document,
        location,
        localStorage,
        bootstrap,
        hideTooltip
    })
}

onAppCreate(app => {
    registerBvComponents(app)
    registerValidation(app)
    installTemplateGlobals(app)
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
 * Dismisses the tooltip on an element. A touch leaves the tooltip on screen with
 * nothing to close it - there is no mouseleave on a phone - so the voice-search
 * button asks for it explicitly on touchend. Replaces the BootstrapVue
 * `$root.$emit('bv::hide::tooltip')` the templates used to send into a $root
 * that no longer has $emit.
 */
function hideTooltip(element) {
    if (element) bootstrap.Tooltip.getInstance(element)?.hide()
}
// a declaration, not an assignment, so installTemplateGlobals can hand it to the
// islands; still on window for markup outside a Vue expression
window.hideTooltip = hideTooltip

/*
 * Per-page view-models are built from the [data-grand-vm] JSON islands. Two
 * triggers, both idempotent:
 *  - right before the islands are mounted, which is the deadline (their
 *    templates address the view-models by their global name);
 *  - on DOMContentLoaded, so data islands on a layout that never defines a
 *    shell still come up.
 * Also exposed on window for markup injected after load.
 */
onBeforeRootMount(() => initViews())
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => initViews())
} else {
    initViews()
}
window.grandInitViews = initViews
// markup fetched after load (a widget, a re-rendered block) can bring its own
// islands up without waiting for a page reload
window.grandMountIslands = mountIslands

window.bootstrap = bootstrap
window.Vue = StorefrontVue
window.axios = axios
window.Pikaday = Pikaday
window.VueGallerySlideshow = VueGallerySlideshow
window.$bvToast = $bvToast
window.vee_getMessage = veeGetMessage
