//Entry point of admin.legacy.js: everything that keeps a view written against Kendo
//working now that Kendo is no longer loaded.
//
//Three parts:
//  - the transitional stylesheet for the `k-button`, `k-link` and `k-icon` classes a
//    third-party plugin view still renders (src/legacy/legacy.css); it ships as
//    admin.legacy.css;
//  - the `$.fn.kendoGrid` shim and the widget mini-shims (kendoWindow,
//    kendoNumericTextBox, kendoDropDownList, kendoMultiSelect, kendoTabStrip) for
//    third-party plugin views, plus the kendo.toString / htmlEncode / culture helpers;
//  - the rename of the Bootstrap 4 data attributes (data-toggle, data-target,
//    data-dismiss) to their data-bs-* spelling. No view in this repository writes them
//    any more, but a plugin view does, and so does markup a plugin loads over AJAX.
//
//Both shims register only when Kendo itself is absent, so loading kendo.grid.js next to
//this bundle (an installation that still needs it) keeps the real widgets. They are
//available for one major release and then removed.
import './legacy/legacy.css'
import { installKendoGridShim } from './legacy/kendo-grid-shim.js'
import { installKendoWidgetShims } from './legacy/kendo-widget-shims.js'
import { watchBootstrap4Attributes } from './legacy/bs4-attributes.js'

installKendoGridShim(window)
installKendoWidgetShims(window)

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', watchBootstrap4Attributes)
} else {
    watchBootstrap4Attributes()
}
