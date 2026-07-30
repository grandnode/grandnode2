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

import LegacyVue, { onAppCreate } from './compat/core'
import { registerBvComponents, VueGallerySlideshow } from './compat/bv-components'
import { registerValidation, veeGetMessage } from './compat/validate'
import { $bvToast } from './compat/bv-services'

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

window.bootstrap = bootstrap
window.Vue = LegacyVue
window.axios = axios
window.Pikaday = Pikaday
window.VueGallerySlideshow = VueGallerySlideshow
window.$bvToast = $bvToast
window.vee_getMessage = veeGetMessage
