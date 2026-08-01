/*
 * "Ask a question about this product" form
 * (was Views/Product/Partials/AskQuestionOnProduct.cshtml).
 *
 * Unlike the other message forms this one reports into two alert blocks on the
 * page rather than a toast, and validates through the root observer before it
 * posts. The fields are read from the root vm's $refs because the markup is
 * part of #app's template.
 */
import LegacyVue, { getRootVm } from '../compat/core'
import { registerView } from './index'
import { axios, formData, notifyRequestError } from './shared'

const FIELDS = ['AskQuestionEmail', 'AskQuestionFullName', 'AskQuestionPhone', 'AskQuestionMessage']

registerView('askQuestion', ({ data, captchaTarget, res }) => {
    window.askquestion = new LegacyVue({
        data: () => ({ ...data }),
        methods: {
            async sendContactUsForm(url) {
                const root = getRootVm()
                const refs = root.$refs

                if (!(await refs.contact.validate())) return

                const fields = {}
                FIELDS.forEach(field => { fields[field] = refs[field].value })
                fields.Id = refs.AskQuestionProductId.value

                const recaptcha = document.querySelector("[id^='g-recaptcha-response']")
                if (recaptcha) fields['g-recaptcha-response-value'] = recaptcha.value

                return axios.post(url, formData(fields))
                    .then(response => {
                        const scope = document.querySelector('.product-standard')
                        if (!scope) return
                        const error = scope.querySelector('.product-contact-error')
                        const sent = scope.querySelector('.product-contact-send')

                        if (response.data.success) {
                            scope.querySelector('#contact-us-product').style.display = 'none'
                            error.style.display = 'none'
                            sent.innerHTML = response.data.message
                            sent.style.display = 'block'
                        } else {
                            error.innerHTML = response.data.message
                            error.style.display = 'block'
                        }
                    })
                    // was alert(error), which showed the visitor a raw axios message
                    .catch(error => notifyRequestError(error, res.warning))
            },
            // called from the markup when the tab holding this form is opened
            getCaptcha() {
                if (!captchaTarget) return
                const box = document.getElementById('captcha-box')
                const target = document.getElementById(captchaTarget)
                if (box && target) target.prepend(box)
            }
        }
    })
})
