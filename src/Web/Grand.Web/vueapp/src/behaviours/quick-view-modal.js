/*
 * Quick-view modal hooks (was the second inline script in
 * Views/Shared/Partials/ProductQuickView.cshtml).
 *
 * `initReservationQV` and `QuickViewShown` are methods of the shell (the
 * theme's app.js), not globals. They used to be plain functions on window in
 * wwwroot/theme/script/public.common.js; when that file was folded into the
 * shell the calls here kept reading `window.*`, and `?.()` swallowed the miss -
 * so opening the quick view silently stopped applying the pre-selected
 * attributes. Which meant the gallery showed the product's default picture
 * instead of the one for the colour that was already ticked, and a reservation
 * product opened without its calendar.
 */
import { registerView } from '../views/index'
import { delegate } from './dom'

registerView('quickViewModal', ({ elements }) => {
    delegate('shown.bs.modal', '#' + elements.modalId, () => {
        const shell = window.vm
        if (!shell) return
        shell.initReservationQV?.()
        shell.QuickViewShown?.()
    })
})
