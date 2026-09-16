//GrandAdmin.dateInput - the replacement for the Kendo DatePicker, DateTimePicker and
//TimePicker of the Date, DateNullable, DateTime, DateTimeNullable and Time editor
//templates.
//
//Kendo kept the named input visible and wrote kendo.toString(date, format, culture) into
//it, so the posted value is the culture's short date (or short date + short time, or
//HH:mm) and the MVC binder reads it in the request culture. That contract is kept: the
//named input stays the value holder and is hidden, and a native <input type="date" |
//"datetime-local" | "time"> next to it is what the user edits. The seed value comes from
//the server as an ISO string in data-grand-date-value, so nothing has to parse a
//culture-formatted date in the browser.

import { formatDate, normalizeCulture } from '../grid/format.js'

const instances = new WeakMap()

const pad = n => String(n).padStart(2, '0')

const isoLike = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2}))?)?/

/** Parses the yyyy-MM-ddTHH:mm:ss seed (and what the native inputs return) as local time. */
export function parseLocal(value) {
    const m = isoLike.exec(String(value ?? '').trim())
    if (!m) return null
    const date = new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]), Number(m[4] || 0), Number(m[5] || 0), Number(m[6] || 0))
    return isNaN(date.getTime()) ? null : date
}

function nativeValue(date, mode) {
    if (!date) return ''
    const day = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
    const time = `${pad(date.getHours())}:${pad(date.getMinutes())}`
    if (mode === 'time') return time
    if (mode === 'datetime') return `${day}T${time}`
    return day
}

function nativeType(mode) {
    if (mode === 'time') return 'time'
    return mode === 'datetime' ? 'datetime-local' : 'date'
}

/** The pattern Kendo used by default for each picker. */
export function postPattern(mode, cultureData) {
    const calendar = normalizeCulture(cultureData).calendar
    if (mode === 'time') return 'HH:mm'
    if (mode === 'datetime') return `${calendar.shortDate} ${calendar.shortTime}`
    return calendar.shortDate
}

export class DateInput {
    constructor(element, options = {}) {
        const doc = element.ownerDocument
        this.element = element
        this.mode = options.mode || 'date'
        this.culture = options.culture
        this.pattern = options.format || postPattern(this.mode, this.culture)

        this.picker = doc.createElement('input')
        this.picker.type = nativeType(this.mode)
        this.picker.className = element.className
        this.picker.setAttribute('style', element.getAttribute('style') || '')
        this.picker.classList.add('grand-dateinput')
        if (element.hasAttribute('readonly')) this.picker.readOnly = true
        if (element.hasAttribute('disabled')) this.picker.disabled = true

        element.parentNode.insertBefore(this.picker, element.nextSibling)
        element.style.display = 'none'

        const seed = options.value != null && options.value !== '' ? parseLocal(options.value) : null
        this.value(seed)
        //Kendo did not raise change while it normalized the server value on load
        this.ready = true

        this.picker.addEventListener('change', () => {
            const raw = this.picker.value
            if (raw === '') {
                this.value(null)
                return
            }
            //a time-only input has no day; keep the day the value already carried
            const base = this.mode === 'time' ? (this._value || new Date()) : null
            this.value(this.mode === 'time' ? mergeTime(base, raw) : parseLocal(raw))
        })
    }

    value(next) {
        if (next === undefined) return this._value
        const date = next instanceof Date ? next : (next ? parseLocal(next) : null)
        this._value = date
        this.element.value = date ? formatDate(date, this.pattern, this.culture) : ''
        this.picker.value = nativeValue(date, this.mode)
        if (this.ready) this.element.dispatchEvent(new Event('change', { bubbles: true }))
        return this
    }

    enable(enable = true) {
        this.picker.disabled = !enable
        return this
    }

    destroy() {
        this.picker.remove()
        this.element.style.display = ''
        instances.delete(this.element)
    }
}

function mergeTime(base, hhmm) {
    const [h, m] = String(hhmm).split(':')
    const date = base ? new Date(base.getTime()) : new Date()
    date.setHours(Number(h) || 0, Number(m) || 0, 0, 0)
    return date
}

export function createDateInput(element, options = {}) {
    if (!element) return null
    let widget = instances.get(element)
    if (!widget) {
        widget = new DateInput(element, options)
        instances.set(element, widget)
        element.grandDateInput = widget
        if (window.jQuery) window.jQuery.data(element, 'grandDateInput', widget)
    }
    return widget
}

export function getDateInput(element) {
    return element ? instances.get(element) || null : null
}

/** Upgrades every <input data-grand-date="date|datetime|time"> under a root. */
export function initDateInputs(root, culture) {
    const scope = root?.querySelectorAll ? root : globalThis.document
    const created = []
    for (const element of scope.querySelectorAll('input[data-grand-date]')) {
        if (instances.has(element)) continue
        created.push(createDateInput(element, {
            culture,
            mode: element.getAttribute('data-grand-date'),
            format: element.getAttribute('data-grand-date-format') || null,
            value: element.getAttribute('data-grand-date-value')
        }))
    }
    return created
}
