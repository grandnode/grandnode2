/*
 * "Delete" buttons in the account lists - addresses and sub-accounts
 * (were Views/Account/Addresses.cshtml, Views/Account/SubAccounts.cshtml and
 *  their Theme.Modern overrides, each with its own copy of the same function).
 *
 * The buttons carried onclick="deletecustomeraddress('<id>')"; they now say
 * what to delete in data attributes and this handles all of them.
 */
import { registerView } from '../views/index'
import { axios, formData, notify, notifyRequestError } from '../views/shared'

registerView('confirmDelete', ({ route, field, selector, res }) => {
    document.addEventListener('click', event => {
        const button = event.target.closest(selector)
        if (!button) return

        // native confirm, as before - a themed dialog would be a UX change
        if (!window.confirm(res.areYouSure)) return

        axios.post(route, formData({ [field]: button.dataset.deleteId }))
            .then(response => {
                // the address endpoint always redirects; the sub-account one
                // reports failure instead
                if (response.data.success === false) {
                    notify(response.data.error || response.data.message, res.warning)
                    return
                }
                window.location = response.data.redirect
            })
            .catch(error => notifyRequestError(error, res.warning))
    })
})
