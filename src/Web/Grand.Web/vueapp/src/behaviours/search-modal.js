/*
 * Puts the caret in the search field when the search modal opens
 * (was the inline script in Views/Shared/Partials/Header.cshtml).
 */
import { getRootVm } from '../compat/core'
import { registerView } from '../views/index'
import { delegate } from './dom'

registerView('searchModal', ({ elements }) => {
    // Bootstrap's modal events bubble, so this survives Vue re-rendering the
    // modal - binding to the element at registration time would not.
    delegate('shown.bs.modal', '#' + elements.modalId, () => {
        const input = getRootVm()?.$refs?.searchBoxInput
        if (input) input.focus()
    })
})
