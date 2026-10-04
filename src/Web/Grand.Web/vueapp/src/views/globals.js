/*
 * Plain per-page data, published on window under a given name.
 *
 * For values a view has to hand to a script that is not a view-model - the
 * product gallery tells Theme.Modern's app.js how many pictures there are, so
 * it can decide whether Swiper's loop mode has enough slides to work.
 * Deliberately not reactive: nothing re-reads these after startup.
 */
import { registerView } from './index'

registerView('globals', ({ name, data }) => {
    window[name] = data
})
