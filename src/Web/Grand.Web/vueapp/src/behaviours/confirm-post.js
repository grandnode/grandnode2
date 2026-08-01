/*
 * "Are you sure?" then POST, then reflect the result in the page
 * (was the lesson-approval script in Views/Course/Lesson.cshtml).
 */
import { registerView } from '../views/index'
import { axios, notifyRequestError } from '../views/shared'
import { delegate } from './dom'

registerView('confirmPost', ({ route, elements, res }) => {
    delegate('click', '#' + elements.buttonId, button => {
        if (!window.confirm(res.areYouSure)) return

        axios.post(route)
            .then(() => {
                button.style.display = 'none'
                const check = document.getElementById(elements.checkId)
                if (check) check.checked = true
            })
            .catch(error => notifyRequestError(error, res.warning))
    })
})
