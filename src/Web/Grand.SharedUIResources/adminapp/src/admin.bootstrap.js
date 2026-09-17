//Bootstrap 5.3 for the Admin, Store and Vendor panels.
//
//Replaces the two vendored files the panels loaded in the footer,
//administration/bootstrap/js/popper.min.js and administration/bootstrap/js/bootstrap.min.js
//(Bootstrap 4.5.2 with Popper 1). Popper 2 comes with this bundle, so there is no second
//script tag any more.
//
//The stylesheet is imported here as well, which is what makes Vite emit
//bundles/admin.bootstrap.css next to it; scripts/build.mjs then writes the right-to-left
//variant from that file with rtlcss.

import * as bootstrap from 'bootstrap'
import { watchBootstrap4Attributes } from './compat/bs4-attributes.js'
import './styles/admin.scss'

//Views and third-party plugins reach the components through window.bootstrap.
window.bootstrap = bootstrap

//Bootstrap registers itself as a set of jQuery plugins when jQuery is on the page - the
//panels load jQuery before this bundle, and custom.js still calls $(x).tooltip(),
//$(x).popover() and $(x).dropdown('toggle'). The registration lives in a side-effect at
//the bottom of the module, which Rollup drops from a bundle that only imports the
//namespace, so it is done here instead. It is the same two lines Bootstrap's own
//defineJQueryPlugin writes.
const jquery = window.jQuery
if (jquery) {
    for (const component of Object.values(bootstrap)) {
        if (typeof component !== 'function' || !component.NAME || !component.jQueryInterface) continue
        const name = component.NAME
        const noConflict = jquery.fn[name]
        jquery.fn[name] = component.jQueryInterface
        jquery.fn[name].Constructor = component
        jquery.fn[name].noConflict = () => {
            jquery.fn[name] = noConflict
            return component.jQueryInterface
        }
    }
}

//Bootstrap 4 spelled the data attributes without the bs- infix; see compat/bs4-attributes.js.
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', watchBootstrap4Attributes)
} else {
    watchBootstrap4Attributes()
}
