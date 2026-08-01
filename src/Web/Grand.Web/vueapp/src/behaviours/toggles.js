/*
 * Two one-line checkbox behaviours that each had their own inline script.
 *
 * `enableWhenChecked` - Views/Account/UserAgreement.cshtml: the download link
 *   stays disabled until the agreement is ticked.
 * `checkAll` - Views/OutOfStockSubscription/CustomerSubscriptions.cshtml:
 *   the header checkbox drives the rows.
 */
import { registerView } from '../views/index'
import { delegate } from './dom'

registerView('enableWhenChecked', ({ elements }) => {
    delegate('change', '#' + elements.checkboxId, checkbox => {
        const target = document.getElementById(elements.targetId)
        if (!target) return
        target.classList.toggle('disabled', !checkbox.checked)
    })
})

registerView('checkAll', ({ elements }) => {
    delegate('click', '#' + elements.checkboxId, checkbox => {
        document.querySelectorAll(elements.rowSelector).forEach(row => { row.checked = checkbox.checked })
    })
})
