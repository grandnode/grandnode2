//Entry point of admin.legacy.js: the $.fn.kendoGrid shim for plugins that still build
//Kendo grids in script. Not loaded by any panel yet; it only registers itself when
//kendo.grid.js is absent (see src/legacy/kendo-grid-shim.js).
import { installKendoGridShim } from './legacy/kendo-grid-shim.js'

installKendoGridShim(window)
