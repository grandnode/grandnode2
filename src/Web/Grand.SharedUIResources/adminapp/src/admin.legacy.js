//Entry point of admin.legacy.js: everything that keeps a view written against Kendo
//working now that Kendo is no longer loaded.
//
//Two halves:
//  - the transitional stylesheet for the `k-button`, `k-link` and `k-icon` classes the
//    admin views still carry (src/legacy/legacy.css); it ships as admin.legacy.css;
//  - the `$.fn.kendoGrid` shim and the widget mini-shims (kendoWindow,
//    kendoNumericTextBox, kendoDropDownList, kendoMultiSelect, kendoTabStrip) for
//    third-party plugin views, plus the kendo.toString / htmlEncode / culture helpers.
//
//Both shims register only when Kendo itself is absent, so loading kendo.grid.js next to
//this bundle (an installation that still needs it) keeps the real widgets. They are
//available for one major release and then removed.
import './legacy/legacy.css'
import { installKendoGridShim } from './legacy/kendo-grid-shim.js'
import { installKendoWidgetShims } from './legacy/kendo-widget-shims.js'

installKendoGridShim(window)
installKendoWidgetShims(window)
