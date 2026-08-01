/*
 * Warehouse picker on the product page (was Views/Product/Partials/Warehouses.cshtml).
 *
 * That view registered two change listeners on the same select. The first
 * called `attribute_change_handler_<productId>()`, which has never been a
 * global - it was only the internal name of the function expression assigned to
 * `attrchange`, so selecting a warehouse threw ReferenceError before it got to
 * the second listener. It asks the product-attributes view-model directly now.
 */
import { registerView } from '../views/index'
import { axios } from '../views/shared'
import { delegate } from './dom'

registerView('warehouseSelector', ({ productId, route, elements }) => {
    const refresh = () => {
        const select = document.getElementById(elements.selectId)
        if (!select) return

        const data = new FormData()
        data.append('warehouseId', select.value)
        data.append('productId', productId)

        axios.post(route, data)
            .then(response => {
                const stock = document.getElementById('stock-availability-value-' + productId)
                if (stock && response.data.stockAvailability) {
                    stock.innerText = response.data.stockAvailability
                }
                // the price and the rest of the availability block depend on the
                // warehouse too
                window['standardProductAttributes_' + productId]?.attrchange()
            })
            .catch(err => console.error('[grand] warehouse change failed', err))
    }

    // No priming on load: the page is served with availability already worked
    // out for the preselected warehouse (see GetProductDetailsPageHandler).
    // Writing it from here fought Vue anyway - the label is static Razor text
    // inside #app's template, so the next re-render restored the server value.
    delegate('change', '#' + elements.selectId, refresh)
})
