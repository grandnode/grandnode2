/*
 * Cookie consent bar (was Views/Shared/Partials/Cookie.cshtml).
 *
 * The buttons carried onclick="cookieFnc(this)"; the choice is read from the
 * button's data-accept instead, so the markup holds no JavaScript.
 *
 * The bar is not revealed from here: the view only renders it when the visitor
 * has not answered yet, so nothing has to be decided in the browser.
 */
import { registerView } from '../views/index'
import { axios, notifyRequestError } from '../views/shared'
import { delegate } from './dom'

registerView('cookieBar', ({ route, elements, res }) => {
    delegate('click', '#' + elements.barId + ' [data-accept]', button => {
        axios.post(route, null, { params: { accept: button.dataset.accept } })
            .then(() => {
                const bar = document.getElementById(elements.barId)
                if (bar) bar.style.display = 'none'
            })
            .catch(error => notifyRequestError(error, res.warning))
    })
})
