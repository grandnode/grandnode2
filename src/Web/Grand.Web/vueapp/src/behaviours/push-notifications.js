/*
 * Firebase push notification registration
 * (was Views/Shared/Components/PushNotificationsRegistration/Default.cshtml).
 *
 * `PushNotifications` still lives in wwwroot/theme/script/public.push.notifications.js
 * and is reached through window until that file moves into the bundle.
 */
import { registerView } from '../views/index'
import { onReady } from './dom'

registerView('pushNotifications', ({ firebase, route }) => {
    onReady(() => {
        if (!window.PushNotifications) return
        window.PushNotifications.init(
            firebase.publicApiKey, firebase.senderId, firebase.projectId, firebase.authDomain,
            firebase.storageBucket, firebase.databaseUrl, route, firebase.appId)
        window.PushNotifications.process()
    })
})
