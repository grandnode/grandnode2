/*
 * Wires the checkout step controllers to their forms and endpoints
 * (was the inline script in Views/Checkout/Start.cshtml).
 *
 * The controllers themselves come from wwwroot/theme/script/public.checkout.js,
 * which stays a separate file on purpose: it is 39 kB of checkout-only code and
 * the bundle is a single IIFE served on every page. It extends the `vmorder`
 * view-model from a footer script, so this waits for the root app rather than
 * running at registration time.
 */
import { registerView } from '../views/index'
import { onReady } from './dom'

registerView('checkoutSteps', ({ routes }) => {
    onReady(() => {
        const cart = window.vmorder
        if (!cart?.vCartUrl) {
            console.error('[grand] checkout controllers missing - public.checkout.js did not load')
            return
        }

        cart.vCartUrl.init(routes.cartSummary, routes.cartTotal)
        cart.vShipping.init('#co-shipping-form', routes.saveShipping)
        cart.vBilling.init('#co-billing-form', routes.saveBilling)
        cart.vShippingMethod.init('#co-shipping-method-form', routes.saveShippingMethod)
        cart.vPaymentMethod.init('#co-payment-method-form', routes.savePaymentMethod)
        cart.vPaymentInfo.init('#co-payment-info-form', routes.savePaymentInfo)
        cart.vConfirmOrder.init(routes.confirmOrder, routes.completed)
    })
})
