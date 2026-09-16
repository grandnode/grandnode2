//Entry point of admin.ui.js: the admin widgets that used to be Kendo UI - the tab strip,
//the window, the numeric text box, the date/time pickers and the drop-down and multiselect
//lists. Loaded by HeadAdmin, HeadStore and HeadVendor next to admin.grid.js.
import 'tom-select/dist/css/tom-select.bootstrap4.css'
import './ui/ui.css'
import { pageCulture } from './ui/culture.js'
import { modal } from './ui/modal.js'
import { createTabs } from './ui/tabs.js'
import { createNumeric, getNumeric, initNumeric } from './ui/numeric.js'
import { createDateInput, getDateInput, initDateInputs } from './ui/datetime.js'
import { createSelect, createLookup, getSelect, initSelects } from './ui/select.js'
import { formatValue, formatDate, formatNumber, toDate } from './grid/format.js'

const GrandAdmin = (window.GrandAdmin = window.GrandAdmin || {})

if (!GrandAdmin.ui) {
    const culture = () => pageCulture()

    GrandAdmin.culture = culture
    //kendo.toString(value, format) - the culture is the page culture unless one is passed
    GrandAdmin.format = (value, format, cultureData) => formatValue(value, format, cultureData || culture())
    GrandAdmin.formatDate = (value, format, cultureData) => {
        const date = toDate(value)
        return date ? formatDate(date, format, cultureData || culture()) : ''
    }
    GrandAdmin.formatNumber = (value, format, cultureData) => formatNumber(value, format, cultureData || culture())
    //kendo.htmlEncode
    GrandAdmin.htmlEncode = text => String(text ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;')

    GrandAdmin.modal = modal
    GrandAdmin.tabs = createTabs({})
    GrandAdmin.numeric = { create: (el, o) => createNumeric(el, { culture: culture(), ...o }), get: getNumeric }
    GrandAdmin.dateInput = { create: (el, o) => createDateInput(el, { culture: culture(), ...o }), get: getDateInput }
    GrandAdmin.select = { create: createSelect, lookup: createLookup, get: getSelect }

    GrandAdmin.ui = {
        /** Upgrades the widgets of a freshly inserted fragment (popups, appended tabs). */
        init(root = document) {
            GrandAdmin.tabs.init(root)
            initNumeric(root, culture())
            initDateInputs(root, culture())
            initSelects(root)
        }
    }

    let started = false
    const start = () => {
        if (started) return
        started = true
        GrandAdmin.ui.init(document)
    }
    //this bundle is in <head>, so a jQuery ready handler registered here runs before the
    //views' own; the DOMContentLoaded listener is the fallback when an earlier ready
    //handler throws and jQuery 2 stops calling the rest
    if (window.jQuery) window.jQuery(start)
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start)
    else start()
}
