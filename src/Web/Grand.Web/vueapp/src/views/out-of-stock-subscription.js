/*
 * "Notify me when available" (was Views/Product/Partials/OutOfStockSubscription.cshtml).
 *
 * The button used to call `showModalOutOfStock` in the theme's app.js, which
 * did `this.$refs['out-of-stock'].show()` - the BootstrapVue `<b-modal ref>`
 * API. Nothing has carried that ref since the move to Bootstrap 5, so every
 * click threw "Cannot read properties of undefined (reading 'show')" and the
 * dialog never opened. It is a plain Bootstrap modal now.
 *
 * The POST does the subscribe/unsubscribe itself and answers with the message
 * to show and the new button caption.
 */
import * as bootstrap from 'bootstrap'
import { onRootReady } from '../compat/core'
import { registerView } from './index'
import { axios, notifyRequestError } from './shared'

registerView('outOfStockSubscription', ({ productId, routes, elements, res }) => {
    // The button is rendered by the root app, so it does not exist until #app
    // has mounted.
    onRootReady(root => root.$nextTick(() => {
        const button = document.getElementById(elements.buttonId)
        if (!button) return

        // the caption depends on whether this customer is already subscribed
        axios.get(routes.buttonText)
            .then(response => {
                button.value = response.data
                button.style.display = 'inline-block'
            })
            .catch(error => notifyRequestError(error, res.warning))

        button.addEventListener('click', () => {
            const form = document.getElementById('product-details-form-' + productId)
            axios.post(routes.popup, form ? new FormData(form) : null)
                .then(response => {
                    document.getElementById(elements.contentId).innerHTML = response.data.resource
                    button.value = response.data.buttontext
                    bootstrap.Modal.getOrCreateInstance(document.getElementById(elements.modalId)).show()
                })
                .catch(error => notifyRequestError(error, res.warning))
        })
    }))
})
