/*
 * Route URLs and localized strings the bundled theme scripts read at runtime
 * (was the inline script in Views/Shared/Partials/JsResources.cshtml).
 *
 * Still published as window.grandRes / window.grandRoutes: axios-cart.js reads
 * them by name, and so may store customisations.
 */
import { registerView } from '../views/index'

registerView('jsResources', ({ res, routes }) => {
    window.grandRes = Object.assign(window.grandRes || {}, res)
    window.grandRoutes = Object.assign(window.grandRoutes || {}, routes)
})
