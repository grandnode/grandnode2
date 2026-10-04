/*
 * Reports the visitor's coordinates once, if they differ from what the server
 * already has (was Views/Shared/Components/GetCoordinate/Default.cshtml, which
 * also relied on SaveCurrentPossition in public.common.js).
 */
import { registerView } from '../views/index'
import { axios } from '../views/shared'

registerView('geolocation', ({ route, known }) => {
    if (!navigator.geolocation) return

    navigator.geolocation.getCurrentPosition(position => {
        const { latitude, longitude } = position.coords
        if (latitude === known.latitude && longitude === known.longitude) return

        const data = new FormData()
        data.append('latitude', latitude)
        data.append('longitude', longitude)
        // a failure here is invisible to the visitor by design - it only costs
        // us a more accurate shipping estimate
        axios.post(route, data).catch(err => console.error('[grand] position update failed', err))
    })
})
