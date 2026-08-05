/*
 * Checkout attributes (cart page) and contact attributes (contact page)
 * (were the inline scripts in Views/ShoppingCart/Partials/CheckoutAttributes.cshtml
 *  and Views/Contact/Partials/ContactAttributes.cshtml).
 *
 * Both forms work the same way: any change re-posts the form, the answer says
 * which attribute rows to show or hide, and a file attribute uploads separately.
 * The contact view also generated its change wiring as JavaScript from a C#
 * StringBuilder; both are delegated here instead.
 *
 * `uploadFile` stays on window because the file inputs are rendered with an
 * inline onchange - those attributes are the next thing to go.
 */
import { registerView } from '../views/index'
import { axios } from '../views/shared'
import { delegate, onReady } from './dom'

/** Shows or hides the attribute rows the server says are (in)active. */
function applyVisibility(data, prefix) {
    const set = (ids, display) => (ids || []).forEach(id => {
        const label = document.querySelector(`#${prefix}_attribute_label_${id}`)
        const input = document.querySelector(`#${prefix}_attribute_input_${id}`)
        if (label) label.style.display = display
        if (input) input.style.display = display
    })
    set(data.enabledattributeids, 'block')
    set(data.disabledattributeids, 'none')
}

function showUploadMessage(message, ok) {
    const container = document.getElementById('download-message')
    if (!container) return
    container.style.display = 'block'
    container.classList.toggle('alert-info', ok)
    container.classList.toggle('alert-danger', !ok)
    container.innerText = message
}

function revealDownload(url) {
    const button = document.querySelector('.download-file')
    if (!button) return
    button.style.display = 'inline-block'
    button.setAttribute('href', url)
}

/* ------------------------------- checkout ------------------------------- */

registerView('checkoutAttributes', ({ routes }) => {
    const change = () => {
        const form = document.querySelector('#shopping-cart-form')
        if (!form) return
        return axios.post(routes.change, new FormData(form))
            .then(({ data }) => {
                const cart = window.vmorder
                if (cart) {
                    cart.totals = data.model
                    cart.cart.CheckoutAttributeInfo = data.checkoutattributeinfo || ''
                }
                applyVisibility(data, 'checkout')
            })
            .catch(err => console.error('[grand] checkout attribute change failed', err))
    }

    window.checkoutAttributeChange = change

    window.uploadFile = input => {
        const body = new FormData()
        body.append('file', input.files[0])
        const attributeId = input.getAttribute('attribute')

        axios.post(input.getAttribute('data-url'), body)
            .then(({ data }) => {
                if (data.success) {
                    const attribute = window.vmorder?.cart.CheckoutAttributes.find(a => a.Id === attributeId)
                    if (attribute) attribute.DefaultValue = data.downloadGuid
                    input.setAttribute('qq-button-id', data.downloadGuid)
                    const hidden = document.querySelector('.hidden-upload-input')
                    if (hidden) hidden.value = data.downloadGuid
                    revealDownload(data.downloadUrl)
                }
                showUploadMessage(data.message, data.success)
                return change()
            })
            .catch(err => console.error('[grand] checkout attribute upload failed', err))
    }

    onReady(change)
})

/* -------------------------------- contact ------------------------------- */

registerView('contactAttributes', ({ routes }) => {
    const change = () => {
        const form = document.querySelector('#contactus-form')
        if (!form) return
        return axios.post(routes.change, new FormData(form))
            .then(({ data }) => applyVisibility(data, 'contact'))
            .catch(err => console.error('[grand] contact attribute change failed', err))
    }

    window.contactAttributeChange = change

    window.uploadFile = input => {
        const body = new FormData()
        body.append('file', input.files[0])

        axios.post(input.getAttribute('data-url'), body)
            .then(({ data }) => {
                if (data.success) {
                    const hidden = document.querySelector('.hidden-upload-input')
                    if (hidden) hidden.setAttribute('value', data.downloadGuid)
                    revealDownload(data.downloadUrl)
                }
                showUploadMessage(data.message, data.success)
                return change()
            })
            .catch(err => console.error('[grand] contact attribute upload failed', err))
    }

    // replaces the generated addEventListener-per-control script
    delegate('change', '.contact-attributes [id^="contact_attribute_"]', change)
    delegate('click', '.contact-attributes [id^="contact_attribute_"]', change)

    // colour squares mark their own selection
    delegate('click', '.contact-attributes [id^="color-squares-"] li', item => {
        document.querySelector('.contact-attributes .selected-value')?.classList.remove('selected-value')
        item.classList.add('selected-value')
    })

    onReady(change)
})
