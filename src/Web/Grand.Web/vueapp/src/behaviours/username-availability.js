/*
 * "Check availability" next to the username field
 * (was Views/Account/Partials/CheckUsernameAvailability.cshtml).
 *
 * The result element used to be injected with insertAdjacentHTML from the
 * script; it is part of the markup now.
 */
import { registerView } from '../views/index'
import { axios, formData, notifyRequestError } from '../views/shared'
import { delegate } from './dom'

const AVAILABLE = 'username-available-status alert alert-success d-flex justify-content-center mt-3 mb-0'
const TAKEN = 'username-not-available-status alert alert-danger d-flex justify-content-center mt-3 mb-0'
const EMPTY = 'username-not-available-status text-danger d-flex order-2 mt-3'

registerView('usernameAvailability', ({ route, elements, res }) => {
    delegate('click', '#' + elements.buttonId, button => {
        const username = document.getElementById(elements.usernameId)
        const result = document.getElementById(elements.resultId)
        const progress = document.getElementById(elements.progressId)
        if (!username || !result) return

        result.innerText = ''

        if (!username.value) {
            result.setAttribute('class', EMPTY)
            result.innerText = res.enterUsername
            return
        }

        if (progress) progress.style.display = 'block'
        button.setAttribute('disabled', 'disabled')

        const done = () => {
            button.removeAttribute('disabled')
            if (progress) progress.style.display = 'none'
        }

        axios.post(route, formData({ Username: username.value }))
            .then(response => {
                done()
                result.setAttribute('class', response.data.Available ? AVAILABLE : TAKEN)
                result.innerText = response.data.Text
            })
            .catch(error => {
                done()
                notifyRequestError(error, res.warning)
            })
    })
})
