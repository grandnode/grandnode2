/*
 * Password-protected pages (were Views/Page/PageDetails.cshtml,
 * Views/Shared/Components/PagesBlock/Default.cshtml and
 * Views/Shared/Components/PageBlock/Default.cshtml in both themes - four copies
 * of the same routine, differing only in whether the element ids carry the page
 * id and whether Enter submits).
 */
import { registerView } from '../views/index'
import { axios, formData, notifyRequestError } from '../views/shared'
import { delegate, onReady } from './dom'

registerView('passwordPage', ({ route, elements, submitOnEnter, res }) => {
    const el = id => (id ? document.getElementById(id) : null)

    const unlock = () => {
        const idField = el(elements.pageIdFieldId)
        const password = el(elements.passwordId)
        if (!idField || !password) return

        axios.post(route, formData({ id: idField.value, password: password.value }))
            .then(response => {
                const error = el(elements.errorId)
                if (!response.data.Authenticated) {
                    if (error) error.innerText = response.data.Error
                    return
                }

                const page = el(elements.pageId)
                const title = page?.querySelector(elements.titleSelector)
                if (title) {
                    title.innerHTML = response.data.Title
                    if (!title.innerText.trim()) {
                        const titleBlock = el(elements.titleId)
                        if (titleBlock) titleBlock.style.display = 'none'
                    }
                }
                const body = page?.querySelector('.page-body')
                if (body) body.innerHTML = response.data.Body

                const prompt = el(elements.promptId)
                if (prompt) prompt.style.display = 'none'
                if (page) page.style.display = 'block'
            })
            .catch(error => notifyRequestError(error, res.warning))
    }

    onReady(() => {
        const page = el(elements.pageId)
        if (page) page.style.display = 'none'
    })

    if (elements.buttonId) delegate('click', '#' + elements.buttonId, unlock)

    if (submitOnEnter) {
        delegate('keydown', '#' + elements.passwordId, (_, event) => {
            // was a document-wide keydown listener, so Enter anywhere on the page
            // fired the request
            if (event.key === 'Enter') unlock()
        })
    }
})
