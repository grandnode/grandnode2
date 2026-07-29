/*
 * Storefront bundle entry (Vue 3 + Bootstrap 5).
 *
 * Exposes `window.Vue` — a Vue 2 style compatibility facade (see compat/core.js)
 * used by the Razor views and theme scripts, plus the shared globals
 * (axios, Pikaday, $bvToast, $bvModal).
 */
import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-icons/font/bootstrap-icons.css'
import 'animate.css'
import 'pikaday/css/pikaday.css'
import './compat/compat.css'

import axios from 'axios'
import Pikaday from 'pikaday'

import LegacyVue, { onAppCreate } from './compat/core'
import { registerBvComponents, VueGallerySlideshow } from './compat/bv-components'
import { registerBvDirectives } from './compat/bv-directives'
import { registerValidation, veeGetMessage } from './compat/validate'
import { $bvToast, $bvModal, toggleTarget } from './compat/bv-services'

onAppCreate(app => {
    registerBvComponents(app)
    registerBvDirectives(app)
    registerValidation(app)
    app.config.globalProperties.$bvToast = $bvToast
    app.config.globalProperties.$bvModal = $bvModal
    // warnHandler is silenced (legacy in-DOM templates trigger noisy dev
    // warnings), but real render/setup errors must stay visible - otherwise
    // Vue silently swallows them and renders an empty comment node instead.
    app.config.errorHandler = (err, instance, info) => console.error(err, info)
})

window.Vue = LegacyVue
window.axios = axios
window.Pikaday = Pikaday
window.VueGallerySlideshow = VueGallerySlideshow
window.$bvToast = $bvToast
window.$bvModal = $bvModal
window.bvToggle = toggleTarget
window.vee_getMessage = veeGetMessage
