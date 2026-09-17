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

//Views and third-party plugins call bootstrap.Modal, $('#x').modal() is gone in Bootstrap 5.
window.bootstrap = bootstrap

//Bootstrap 4 spelled the data attributes without the bs- infix; see compat/bs4-attributes.js.
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', watchBootstrap4Attributes)
} else {
    watchBootstrap4Attributes()
}
