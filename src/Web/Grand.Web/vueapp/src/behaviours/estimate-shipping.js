/*
 * Shipping estimate on the cart page
 * (was Views/Shared/Components/EstimateShipping/Default.cshtml).
 *
 * The country change used `response.data.forEach(function (id, option) {...})`,
 * whose parameters were the wrong way round - it happened to work only because
 * the second one was never used.
 */
import { registerView } from '../views/index'
import { axios, notify, notifyRequestError } from '../views/shared'
import { delegate } from './dom'

registerView('estimateShipping', ({ routes, elements, res }) => {
    delegate('click', '#' + elements.buttonId, () => {
        const form = document.getElementById('shopping-cart-form')
        if (!form) return
        axios.post(routes.estimate, new FormData(form))
            .then(response => {
                const result = document.querySelector('.estimate-shipping-result')
                if (result) result.innerHTML = response.data
                window.checkoutAttributeChange?.()
            })
            .catch(() => notify(res.estimateFailed, res.warning))
    })

    delegate('change', '#' + elements.countrySelectId, select => {
        const states = document.getElementById(elements.stateSelectId)
        const progress = document.getElementById(elements.progressId)
        if (progress) progress.style.display = 'block'

        axios.get(routes.getStates, { params: { countryId: select.value, addSelectStateItem: false } })
            .then(response => {
                if (states) {
                    states.innerHTML = ''
                    response.data.forEach(state => {
                        const option = document.createElement('option')
                        option.value = state.id
                        option.innerHTML = state.name
                        states.appendChild(option)
                    })
                }
                if (progress) progress.style.display = 'none'
                window.checkoutAttributeChange?.()
            })
            .catch(error => {
                if (progress) progress.style.display = 'none'
                notifyRequestError(error, res.warning)
            })
    })
})
