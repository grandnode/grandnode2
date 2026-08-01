/*
 * Reservation date picker bootstrap (was Views/Product/Partials/ReservationInfo.cshtml).
 *
 * `Reservation` still lives in wwwroot/theme/script/public.common.js, so it is
 * reached through window until that file moves into the bundle.
 */
import { registerView } from '../views/index'
import { onReady } from './dom'

registerView('reservationInfo', ({ productId, start, routes, res }) => {
    onReady(() => {
        window.Reservation?.init(
            start.date, start.year, start.month, res.noAvailableReservations,
            routes.datesForMonth, productId, routes.attributeChange)
    })
})
