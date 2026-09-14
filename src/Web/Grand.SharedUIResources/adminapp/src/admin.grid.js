//Entry point of admin.grid.js: the <admin-grid> runtime, loaded by HeadAdmin, HeadStore
//and HeadVendor after jQuery and admin.common.js, next to Kendo (which keeps serving the
//grids that are not converted yet).
import { Tabulator, FormatModule, ResizeTableModule } from 'tabulator-tables'
import 'tabulator-tables/dist/css/tabulator_bootstrap5.min.css'
import './grid/grid.css'
import { createRegistry } from './grid/registry.js'
import { formatValue } from './grid/format.js'

Tabulator.registerModule([FormatModule, ResizeTableModule])

const GrandAdmin = (window.GrandAdmin = window.GrandAdmin || {})
if (!GrandAdmin.grids) {
    GrandAdmin.grids = createRegistry({ Tabulator })
    GrandAdmin.format = GrandAdmin.format || formatValue

    let started = false
    const start = () => {
        if (started) return
        started = true
        GrandAdmin.grids.init(document)
        GrandAdmin.grids.observe()
    }
    //Views look grids up in their own $(document).ready handlers. This bundle is loaded in
    //<head>, so a jQuery ready handler registered here runs before theirs. The plain
    //DOMContentLoaded listener is the fallback when an earlier ready handler throws, which
    //stops jQuery 2 from calling the remaining ones.
    if (window.jQuery) window.jQuery(start)
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start)
    else start()
}
