/*
 * Forms with a country -> state/province cascade: the customer and vendor
 * address forms, and customer registration / account info.
 *
 * (were Views/Shared/Partials/CreateOrUpdateAddress.cshtml,
 *  Views/Vendor/Partials/CreateOrUpdateVendorAddress.cshtml,
 *  Views/Account/Register.cshtml, Views/Account/Info.cshtml and their
 *  Theme.Modern overrides)
 *
 * All of them carried the same code, and each generated a pair of *global
 * functions named after its field ids* - `BillingNewAddress_CountryId_select_element`
 * and friends - called from inline onchange attributes. The handlers are bound
 * here by delegation instead, so one module serves every such form on the page
 * and survives Vue re-rendering the selects, which a directly attached listener
 * would not.
 */
import { createViewModel } from '../compat/view-model'
import { onRootReady } from '../runtime/islands'
import { registerView } from './index'
import { axios, notifyRequestError } from './shared'

/** One delegated listener per document, however many forms there are. */
const forms = new Map()
let delegated = false

function markState(select, valid) {
    select.classList.remove(valid ? 'is-invalid' : 'is-valid')
    select.classList.add(valid ? 'is-valid' : 'is-invalid')
}

function onCountryChange(form, select) {
    if (!select.value) return
    const states = document.getElementById(form.elements.stateSelectId)
    return axios.get(form.routes.getStates, {
        params: { countryId: select.value, addSelectStateItem: 'true' }
    }).then(response => {
        const vm = window[form.name]
        vm.AvailableStates = response.data
        vm.StateProvinceId = response.data[0].id
        // the reloaded list starts on the "select state" placeholder, so the
        // field is not valid until the visitor picks one
        if (states) markState(states, false)
    }).catch(error => notifyRequestError(error, form.res.warning))
}

/** Shows the newsletter category picker only while the newsletter box is ticked. */
function applyNewsletterToggle(toggle) {
    const checkbox = document.getElementById(toggle.checkboxId)
    const target = document.querySelector(toggle.targetSelector)
    if (!checkbox || !target) return
    target.style.display = checkbox.checked ? 'block' : 'none'
}

function delegate() {
    if (delegated) return
    delegated = true
    document.addEventListener('change', event => {
        const id = event.target.id
        if (!id) return
        for (const form of forms.values()) {
            if (id === form.elements.countrySelectId) return void onCountryChange(form, event.target)
            if (id === form.elements.stateSelectId) return void markState(event.target, event.target.selectedIndex > 0)
            if (form.newsletterToggle && id === form.newsletterToggle.checkboxId) {
                return void applyNewsletterToggle(form.newsletterToggle)
            }
        }
    })
}

registerView('countryStateForm', form => {
    window[form.name] = createViewModel({ data: () => ({ ...form.data }) })

    forms.set(form.name, form)
    delegate()

    // Preselected checkbox/radio attributes are marked in the markup and have to
    // be applied after the root app has rendered it - doing it earlier (the old
    // DOMContentLoaded hook) lost the change to Vue's first render.
    onRootReady(vm => vm.$nextTick(() => {
        document.querySelectorAll("[data-checked='true']").forEach(el => { el.checked = true })
        if (form.newsletterToggle) applyNewsletterToggle(form.newsletterToggle)
    }))
})
