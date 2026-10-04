/*
 * +/- buttons beside the add-to-cart quantity box
 * (was Views/Modern/Product/Partials/AddToCart.cshtml, which generated a pair
 *  of global increaseValue/decreaseValue functions per product - so a grouped
 *  product silently ended up with one product's pair driving every box).
 *
 * The buttons say which input they belong to instead.
 */
import { registerView } from '../views/index'
import { delegate } from './dom'

function step(input, by) {
    const value = parseInt(input.value, 10)
    const current = isNaN(value) ? 0 : value
    input.value = Math.max(1, current + by)
}

registerView('quantityStepper', () => {
    delegate('click', '[data-quantity-step]', button => {
        const input = document.getElementById(button.dataset.quantityTarget)
        if (input) step(input, Number(button.dataset.quantityStep))
    })
})
