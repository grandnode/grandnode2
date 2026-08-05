/*
 * `window.Vue` - the storefront's public Vue surface.
 *
 * Themes and plugins ship plain <script> files that are not part of this bundle
 * (wwwroot/theme/script/public.checkout.js, Plugins/Theme.Modern/Content/script/*),
 * and they reach Vue through this object. It is deliberately small: a
 * view-model factory, the shell, the island mounter and the two Vue exports
 * those scripts use.
 */
import { nextTick, reactive } from 'vue'
import { createViewModel, defineShell } from './view-model'
import { createStorefrontApp, mountIslands, registerComponent, registerViewModel } from '../runtime/islands'

export function StorefrontVue(options = {}) {
    /*
     * `el` is not supported any more. Nothing in the storefront mounts that way
     * since the shell replaced the single #app root, but this is a public
     * surface a plugin could still call - and silently building an unmounted
     * view-model instead would look like "my component renders nothing".
     */
    if (options.el) {
        console.error('[grand] new Vue({ el }) is gone: mark the element `vue-island` ' +
            'and put shared state in Vue.shell(), or use Vue.createApp(...).mount(el)', options.el)
        return null
    }
    return createViewModel(options)
}

StorefrontVue.component = registerComponent
StorefrontVue.createApp = createStorefrontApp
StorefrontVue.nextTick = nextTick
StorefrontVue.reactive = reactive
StorefrontVue.shell = defineShell
StorefrontVue.mountIslands = mountIslands
/*
 * Lets an out-of-bundle script publish its view-model under the name an island
 * declares (`vue-island="vmorder"`), so the template resolves it as ordinary
 * component data rather than through the window fall-through Proxy. Must run
 * before the islands mount - these scripts do, they are ordered ahead of the
 * theme's app.js, which is what calls Vue.shell().
 */
StorefrontVue.registerViewModel = registerViewModel

export default StorefrontVue
