/*
 * Compare-products page (Views/Product/CompareProducts.cshtml and its
 * Theme.Modern override).
 *
 * The product list is not a view-model of its own - it is written onto the root
 * #app instance, which is why the inline scripts had to be ordered after app.js
 * (default theme) or deferred to DOMContentLoaded (Modern). onRootReady states
 * that dependency directly instead.
 *
 * The two themes differ only in where the list comes from: the default theme
 * serializes it into the page, Modern asks the root vm to fetch it.
 */
import LegacyVue, { onRootReady } from '../compat/core'
import { registerView } from './index'

registerView('compareProducts', ({ model, specificationAttributes, loadRoute }) => {
    window.specificationAttributes = new LegacyVue({
        data: () => ({ Model: specificationAttributes })
    })

    onRootReady(vm => {
        if (loadRoute) vm.getCompareList(loadRoute)
        else vm.compareproducts = model
    })
})
