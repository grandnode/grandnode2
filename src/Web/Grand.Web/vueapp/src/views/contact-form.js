/*
 * The "send us a message" forms: contact a vendor, email a friend about a
 * product, email a wishlist.
 *
 * (was Views/Catalog/Components/VendorContact/Default.cshtml,
 *  Views/Product/Components/ProductEmailAFriend/Default.cshtml,
 *  Views/Wishlist/Components/EmailWishlist/Default.cshtml)
 *
 * All three carried the same code. Each of them showed its result by mounting a
 * *whole new Vue application* onto `.modal-place` on every submit, purely to
 * reach `this.$bvToast` - so submitting twice mounted two apps over the same
 * element. That is now one notify() call.
 *
 * The captcha widget is a single element that has to be moved between its
 * resting place and the popup, because a page may only have one; `captcha`
 * in the payload says where it goes.
 */
import * as bootstrap from 'bootstrap'
import LegacyVue from '../compat/core'
import { registerView } from './index'
import { axios, notify, notifyRequestError } from './shared'

function moveCaptcha(targetId) {
    const box = document.getElementById('captcha-box')
    const target = document.getElementById(targetId)
    if (box && target) target.prepend(box)
}

registerView('contactForm', ({ name, data, formId, submitMethod, modalId, captcha, res }) => {
    const vm = new LegacyVue({
        data: () => ({
            ...data,
            Message: { Result: null, SuccessfullySent: false }
        }),
        methods: {
            [submitMethod](url) {
                const form = document.getElementById(formId)
                return axios.post(url, new FormData(form), { headers: { Accept: 'application/json' } })
                    .then(response => {
                        const { Result, SuccessfullySent } = response.data
                        this.Message.Result = Result
                        this.Message.SuccessfullySent = SuccessfullySent

                        if (SuccessfullySent) {
                            form.style.display = 'none'
                            if (modalId) {
                                bootstrap.Modal.getOrCreateInstance(document.getElementById(modalId)).hide()
                            }
                        }
                        notify(Result, '', SuccessfullySent ? 'info' : 'danger')
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            // called from the markup when the tab holding this form is opened
            getCaptcha() {
                if (captcha?.tabTarget) moveCaptcha(captcha.tabTarget)
            }
        }
    })

    window[name] = vm

    // A captcha inside a modal has to be carried in when the modal opens and
    // put back when it closes, or the next open finds it gone.
    if (modalId && captcha?.popupTarget) {
        const modal = document.getElementById(modalId)
        if (modal) {
            modal.addEventListener('shown.bs.modal', () => {
                if (!modal.querySelector('.captcha-box')) moveCaptcha(captcha.popupTarget)
            })
            modal.addEventListener('hide.bs.modal', () => {
                if (modal.querySelector('.captcha-box')) moveCaptcha(captcha.restoreTarget)
            })
        }
    }
})
