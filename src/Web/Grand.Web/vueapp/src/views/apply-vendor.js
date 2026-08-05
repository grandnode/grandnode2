/*
 * Apply-for-vendor-account form (was Views/Vendor/ApplyVendor.cshtml).
 *
 * The terms-of-service gate used to `return false` out of an addEventListener
 * callback, which does nothing - the visitor got an alert and the form posted
 * anyway. Preventing the default on the click actually stops the submit.
 */
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { notify } from './shared'

registerView('applyVendor', ({ model, res }) => {
    // registered for the island that declares `vue-island="applyvendor"`, and
    // still on window for anything outside the bundle that reaches for it
    window.applyvendor = registerViewModel('applyvendor', createViewModel({
        data: () => ({
            Name: model.Name,
            Email: model.Email,
            AcceptPrivacyPolicy: false
        })
    }))

    const button = document.getElementById('apply-vendor')
    const terms = document.getElementById('accept-terms-of-service')
    if (!button || !terms) return

    button.addEventListener('click', event => {
        if (terms.checked) return
        event.preventDefault()
        notify(res.acceptTermsRequired, res.warning)
    })
})
