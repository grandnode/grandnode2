/*
 * Server-side success/error messages surfaced as a bar notification
 * (was Views/Shared/Partials/Notifications.cshtml, which called
 * vm.displayBarNotification directly from a footer script ordered after app.js).
 */
import { onRootReady } from '../runtime/islands'
import { registerView } from '../views/index'

registerView('barNotifications', ({ messages }) => {
    onRootReady(vm => {
        messages.forEach(({ text, variant }) => vm.displayBarNotification(text, '', variant, 3500))
    })
})
