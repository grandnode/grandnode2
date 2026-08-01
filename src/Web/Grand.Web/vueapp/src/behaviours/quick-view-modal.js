/*
 * Quick-view modal hooks (was the second inline script in
 * Views/Shared/Partials/ProductQuickView.cshtml).
 *
 * `initReservationQV` and `QuickViewShown` still live in
 * wwwroot/theme/script/public.common.js, so they are reached through window
 * until that file moves into the bundle.
 */
import { registerView } from '../views/index'
import { delegate } from './dom'

registerView('quickViewModal', ({ elements }) => {
    delegate('shown.bs.modal', '#' + elements.modalId, () => {
        window.initReservationQV?.()
        window.QuickViewShown?.()
    })
})
