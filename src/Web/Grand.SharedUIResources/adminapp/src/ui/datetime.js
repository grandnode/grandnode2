//GrandAdmin.dateInput - the replacement for the Kendo DatePicker, DateTimePicker and
//TimePicker of the Date, DateNullable, DateTime, DateTimeNullable and Time editor
//templates.
//
//What the posted value is, which is a server contract, does not change: the named input the
//template rendered keeps its name and holds the culture's short date (or short date + short
//time, or HH:mm), and the MVC binder reads it in the request culture - kendo.toString with
//the same pattern, verbatim.
//
//What changes is who renders and reads it. The first replacement put a native
//<input type="date" | "datetime-local" | "time"> next to the hidden named input, and a
//native input renders and parses in the *browser's* locale: a store running pl-PL showed a
//US administrator 04/03/2026 for the third of April, and a date typed into it was read with
//the browser's pattern. The calendar below is this file's own, built on Bootstrap 5 markup
//and driven only by the <admin-culture> island - the pattern, the month and day names, the
//first day of the week and the button texts all come from the server culture. There is one
//visible input again, the named one, and what it shows is what it posts.

import { formatDate, normalizeCulture } from '../grid/format.js'
import { parseDateParts, parseTimeParts } from './dateparse.js'
import { collect, pageTexts } from './culture.js'

const instances = new WeakMap()

const pad = n => String(n).padStart(2, '0')

const isoLike = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2}))?)?/

/** Parses the yyyy-MM-ddTHH:mm:ss seed the editor templates render, as local time. */
export function parseLocal(value) {
    const m = isoLike.exec(String(value ?? '').trim())
    if (!m) return null
    const date = new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]), Number(m[4] || 0), Number(m[5] || 0), Number(m[6] || 0))
    return isNaN(date.getTime()) ? null : date
}

/** The pattern Kendo used by default for each picker, and the one that is still posted. */
export function postPattern(mode, cultureData) {
    const calendar = normalizeCulture(cultureData).calendar
    if (mode === 'time') return 'HH:mm'
    if (mode === 'datetime') return `${calendar.shortDate} ${calendar.shortTime}`
    return calendar.shortDate
}

/**
 * Reads what the user typed with the culture's own pattern. A datetime is split on
 * whitespace at every position, because neither half has a fixed number of words: a culture
 * can write "3 kwietnia 2026" and another "4/3/2026 8:07 PM".
 * @returns {Date|null|undefined} null for empty, undefined for text that is not a value.
 */
export function parseTyped(text, mode, cultureData) {
    const raw = String(text ?? '').trim()
    if (!raw) return null
    const calendar = normalizeCulture(cultureData).calendar

    if (mode === 'time') {
        const time = parseTimeParts(raw, cultureData)
        if (!time) return undefined
        const now = new Date()
        return new Date(now.getFullYear(), now.getMonth(), now.getDate(), time.hours, time.minutes, time.seconds || 0)
    }

    const build = (date, time) =>
        new Date(date.year, date.month, date.day, time?.hours || 0, time?.minutes || 0, time?.seconds || 0)

    if (mode !== 'datetime') {
        const date = parseDateParts(raw, calendar.shortDate, cultureData)
        return date ? build(date, null) : undefined
    }

    const words = raw.split(/\s+/)
    for (let cut = words.length - 1; cut >= 1; cut--) {
        const date = parseDateParts(words.slice(0, cut).join(' '), calendar.shortDate, cultureData)
        if (!date) continue
        const time = parseTimeParts(words.slice(cut).join(' '), cultureData)
        if (time) return build(date, time)
    }
    //a datetime field also accepts a bare date, as DateTime.Parse does
    const dateOnly = parseDateParts(raw, calendar.shortDate, cultureData)
    return dateOnly ? build(dateOnly, null) : undefined
}

const sameDay = (a, b) => a && b &&
    a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate()

const startOfDay = date => new Date(date.getFullYear(), date.getMonth(), date.getDate())

function el(doc, tag, className, text) {
    const node = doc.createElement(tag)
    if (className) node.className = className
    if (text != null) node.textContent = text
    return node
}

function iconButton(doc, className, icon, label) {
    const button = el(doc, 'button', className)
    button.type = 'button'
    button.setAttribute('aria-label', label)
    button.title = label
    button.appendChild(el(doc, 'i', icon))
    return button
}

export class DateInput {
    constructor(element, options = {}) {
        const doc = element.ownerDocument
        this.doc = doc
        this.element = element
        this.mode = options.mode || 'date'
        this.culture = normalizeCulture(options.culture)
        this.calendar = this.culture.calendar
        this.pattern = options.format || postPattern(this.mode, options.culture)
        this.texts = { today: 'Today', clear: 'Clear', previousMonth: 'Previous month', nextMonth: 'Next month', openCalendar: 'Open the calendar', ...(options.texts || {}) }
        this.open = false

        //the field is the control the user types in, which is what Kendo's was; it is only
        //given the Bootstrap class when the editor template did not (most do not)
        if (!element.classList.contains('form-control') && !element.classList.contains('form-select'))
            element.classList.add('form-control')
        element.classList.add('grand-dateinput')
        element.autocomplete = 'off'
        element.setAttribute('aria-haspopup', 'dialog')
        element.setAttribute('aria-expanded', 'false')

        this.group = el(doc, 'div', 'grand-datepicker input-group')
        //a grid editor builds its cell before attaching it, so there may be no parent yet;
        //the caller then places the group itself
        element.parentNode?.insertBefore(this.group, element)
        this.group.appendChild(element)

        this.toggle = iconButton(doc, 'btn btn-outline-secondary grand-datepicker-toggle',
            this.mode === 'time' ? 'bi bi-clock' : 'bi bi-calendar3', this.texts.openCalendar)
        this.group.appendChild(this.toggle)

        this.panel = el(doc, 'div', 'grand-datepicker-panel')
        this.panel.setAttribute('role', 'dialog')
        this.panel.setAttribute('aria-label', this.texts.openCalendar)
        this.panel.hidden = true
        this.group.appendChild(this.panel)

        if (element.hasAttribute('readonly')) this.toggle.disabled = true
        if (element.hasAttribute('disabled')) this.toggle.disabled = true

        //the value the server rendered is left exactly as it is until the user picks a new
        //one, so a form that is opened and saved posts back the string it was given
        this._value = options.value != null && options.value !== ''
            ? parseLocal(options.value)
            : (parseTyped(element.value, this.mode, this.culture) || null)
        this.view = startOfDay(this._value || new Date())
        this.view.setDate(1)
        this.ready = true

        this.toggle.addEventListener('click', () => this.togglePanel())
        element.addEventListener('change', () => this.commitTyped())
        element.addEventListener('keydown', e => {
            if (e.key === 'ArrowDown' && (e.altKey || !this.open)) { e.preventDefault(); this.openPanel() }
            else if (e.key === 'Escape' && this.open) { e.preventDefault(); this.closePanel() }
        })
        this._onDocument = e => {
            if (this.open && !this.group.contains(e.target)) this.closePanel(false)
        }
        doc.addEventListener('mousedown', this._onDocument)
        this.panel.addEventListener('keydown', e => this.onPanelKey(e))
    }

    /** true when the panel is laid out right to left, so the arrow keys mirror. */
    get rtl() {
        const view = this.doc.defaultView
        if (!view?.getComputedStyle) return false
        return view.getComputedStyle(this.group).direction === 'rtl'
    }

    // ------------------------------------------------------------------ value

    value(next) {
        if (next === undefined) return this._value
        const date = next instanceof Date ? next : (next ? parseLocal(next) : null)
        this._value = date && !isNaN(date.getTime()) ? date : null
        this.element.value = this._value ? formatDate(this._value, this.pattern, this.culture) : ''
        if (this._value) {
            this.view = startOfDay(this._value)
            this.view.setDate(1)
        }
        if (this.open) this.render()
        if (this.ready) this.element.dispatchEvent(new Event('change', { bubbles: true }))
        return this
    }

    /** Reads the field after the user typed in it, and writes the culture's own spelling back. */
    commitTyped() {
        const parsed = parseTyped(this.element.value, this.mode, this.culture)
        if (parsed === undefined) {
            //not a date in this culture: the text is left for the server's validation to
            //explain rather than silently thrown away, as Kendo used to throw it away
            this._value = null
            this.element.setAttribute('aria-invalid', 'true')
            return this
        }
        this.element.removeAttribute('aria-invalid')
        this._value = parsed
        this.element.value = parsed ? formatDate(parsed, this.pattern, this.culture) : ''
        if (parsed) {
            this.view = startOfDay(parsed)
            this.view.setDate(1)
        }
        if (this.open) this.render()
        return this
    }

    // ------------------------------------------------------------------ panel

    togglePanel() {
        if (this.open) this.closePanel()
        else this.openPanel()
    }

    openPanel() {
        if (this.open || this.toggle.disabled) return this
        this.open = true
        this.panel.hidden = false
        this.element.setAttribute('aria-expanded', 'true')
        this.render()
        this.focusActive()
        return this
    }

    closePanel(focus = true) {
        if (!this.open) return this
        this.open = false
        this.panel.hidden = true
        this.element.setAttribute('aria-expanded', 'false')
        if (focus) this.element.focus()
        return this
    }

    focusActive() {
        const target = this.panel.querySelector('[data-grand-day][tabindex="0"]')
            || this.panel.querySelector('[data-grand-time][aria-selected="true"]')
            || this.panel.querySelector('[data-grand-time]')
        target?.focus()
    }

    render() {
        this.panel.textContent = ''
        if (this.mode === 'time') this.panel.appendChild(this.renderTimeList())
        else {
            this.panel.appendChild(this.renderCalendar())
            if (this.mode === 'datetime') this.panel.appendChild(this.renderTimeField())
        }
        this.panel.appendChild(this.renderActions())
    }

    renderCalendar() {
        const doc = this.doc
        const wrap = el(doc, 'div', 'grand-cal')

        const header = el(doc, 'div', 'grand-cal-header')
        const prev = iconButton(doc, 'btn btn-sm btn-link grand-cal-nav', 'bi bi-chevron-left', this.texts.previousMonth)
        const next = iconButton(doc, 'btn btn-sm btn-link grand-cal-nav', 'bi bi-chevron-right', this.texts.nextMonth)
        prev.addEventListener('click', () => this.shiftMonth(-1))
        next.addEventListener('click', () => this.shiftMonth(1))
        const label = el(doc, 'div', 'grand-cal-label',
            `${this.calendar.monthsStandalone[this.view.getMonth()]} ${this.view.getFullYear()}`)
        label.setAttribute('aria-live', 'polite')
        //the chevrons keep pointing at the earlier and the later month in both directions
        header.appendChild(prev)
        header.appendChild(label)
        header.appendChild(next)
        wrap.appendChild(header)

        const table = el(doc, 'table', 'grand-cal-grid')
        table.setAttribute('role', 'grid')
        const thead = el(doc, 'thead')
        const headRow = el(doc, 'tr')
        const first = Number(this.calendar.firstDayOfWeek) || 0
        for (let i = 0; i < 7; i++) {
            const day = (first + i) % 7
            const th = el(doc, 'th', null, this.calendar.daysAbbr[day])
            th.scope = 'col'
            th.setAttribute('abbr', this.calendar.days[day])
            headRow.appendChild(th)
        }
        thead.appendChild(headRow)
        table.appendChild(thead)

        const body = el(doc, 'tbody')
        const month = this.view.getMonth()
        const cursor = new Date(this.view.getTime())
        //back up to the first day of the week the 1st falls in
        cursor.setDate(1 - ((cursor.getDay() - first + 7) % 7))
        const today = startOfDay(new Date())
        let tabbable = null
        for (let week = 0; week < 6; week++) {
            const row = el(doc, 'tr')
            for (let i = 0; i < 7; i++) {
                const date = new Date(cursor.getTime())
                const cell = el(doc, 'td')
                cell.setAttribute('role', 'gridcell')
                const button = el(doc, 'button', 'grand-cal-day', String(date.getDate()))
                button.type = 'button'
                button.dataset.grandDay = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
                button.setAttribute('aria-label', formatDate(date, this.calendar.longDate, this.culture))
                if (date.getMonth() !== month) button.classList.add('grand-cal-other')
                if (sameDay(date, today)) button.classList.add('grand-cal-today')
                const selected = sameDay(date, this._value)
                button.setAttribute('aria-selected', String(selected))
                if (selected) button.classList.add('grand-cal-selected')
                //one tab stop for the whole grid, as a grid widget has
                button.tabIndex = -1
                if (selected || (!tabbable && !this._value && sameDay(date, today))) tabbable = button
                button.addEventListener('click', () => this.pick(date))
                cell.appendChild(button)
                row.appendChild(cell)
                cursor.setDate(cursor.getDate() + 1)
            }
            body.appendChild(row)
        }
        if (!tabbable) {
            tabbable = body.querySelector('.grand-cal-day:not(.grand-cal-other)')
        }
        if (tabbable) tabbable.tabIndex = 0
        table.appendChild(body)
        wrap.appendChild(table)
        return wrap
    }

    /** The time half of a datetime: a text field read with the culture's time pattern. */
    renderTimeField() {
        const doc = this.doc
        const row = el(doc, 'div', 'grand-cal-time')
        const input = el(doc, 'input', 'form-control form-control-sm')
        input.type = 'text'
        input.autocomplete = 'off'
        input.setAttribute('aria-label', this.calendar.shortTime)
        input.placeholder = this.calendar.shortTime
        input.value = this._value ? formatDate(this._value, this.calendar.shortTime, this.culture) : ''
        input.addEventListener('change', () => {
            const time = parseTimeParts(input.value, this.culture)
            const base = this._value || startOfDay(new Date())
            if (!time) {
                input.value = this._value ? formatDate(this._value, this.calendar.shortTime, this.culture) : ''
                return
            }
            this.value(new Date(base.getFullYear(), base.getMonth(), base.getDate(), time.hours, time.minutes, time.seconds || 0))
        })
        row.appendChild(el(doc, 'i', 'bi bi-clock'))
        row.appendChild(input)
        return row
    }

    /** A time-only field gets the half-hour list the Kendo TimePicker showed. */
    renderTimeList() {
        const doc = this.doc
        const list = el(doc, 'div', 'grand-time-list')
        list.setAttribute('role', 'listbox')
        const base = this._value || startOfDay(new Date())
        for (let minutes = 0; minutes < 24 * 60; minutes += 30) {
            const date = new Date(base.getFullYear(), base.getMonth(), base.getDate(), Math.floor(minutes / 60), minutes % 60)
            const option = el(doc, 'button', 'grand-time-option',
                formatDate(date, this.calendar.shortTime, this.culture))
            option.type = 'button'
            option.dataset.grandTime = String(minutes)
            option.setAttribute('role', 'option')
            const selected = !!this._value &&
                this._value.getHours() * 60 + this._value.getMinutes() === minutes
            option.setAttribute('aria-selected', String(selected))
            if (selected) option.classList.add('grand-time-selected')
            option.tabIndex = selected ? 0 : -1
            option.addEventListener('click', () => { this.value(date); this.closePanel() })
            list.appendChild(option)
        }
        if (!list.querySelector('[tabindex="0"]')) {
            const firstOption = list.querySelector('.grand-time-option')
            if (firstOption) firstOption.tabIndex = 0
        }
        return list
    }

    renderActions() {
        const doc = this.doc
        const actions = el(doc, 'div', 'grand-cal-actions')
        const today = el(doc, 'button', 'btn btn-sm btn-link', this.texts.today)
        today.type = 'button'
        today.addEventListener('click', () => {
            const now = new Date()
            this.value(this.mode === 'date' ? startOfDay(now) : now)
            this.closePanel()
        })
        const clear = el(doc, 'button', 'btn btn-sm btn-link', this.texts.clear)
        clear.type = 'button'
        clear.addEventListener('click', () => { this.value(null); this.closePanel() })
        actions.appendChild(today)
        actions.appendChild(clear)
        return actions
    }

    pick(date) {
        const kept = this._value
        const next = this.mode === 'datetime' && kept
            ? new Date(date.getFullYear(), date.getMonth(), date.getDate(), kept.getHours(), kept.getMinutes(), kept.getSeconds())
            : startOfDay(date)
        this.value(next)
        if (this.mode !== 'datetime') this.closePanel()
        else this.focusActive()
        return this
    }

    shiftMonth(delta) {
        this.view = new Date(this.view.getFullYear(), this.view.getMonth() + delta, 1)
        this.render()
        this.focusActive()
        return this
    }

    onPanelKey(e) {
        if (e.key === 'Escape') {
            e.preventDefault()
            this.closePanel()
            return
        }
        const day = e.target.closest?.('[data-grand-day]')
        if (day) return this.onDayKey(e, day)
        const time = e.target.closest?.('[data-grand-time]')
        if (time) return this.onTimeKey(e, time)
    }

    onDayKey(e, button) {
        const current = parseLocal(button.dataset.grandDay)
        if (!current) return
        const mirror = this.rtl ? -1 : 1
        let next = null
        switch (e.key) {
            case 'ArrowLeft': next = new Date(current.getFullYear(), current.getMonth(), current.getDate() - mirror); break
            case 'ArrowRight': next = new Date(current.getFullYear(), current.getMonth(), current.getDate() + mirror); break
            case 'ArrowUp': next = new Date(current.getFullYear(), current.getMonth(), current.getDate() - 7); break
            case 'ArrowDown': next = new Date(current.getFullYear(), current.getMonth(), current.getDate() + 7); break
            case 'Home': next = new Date(current.getFullYear(), current.getMonth(), 1); break
            case 'End': next = new Date(current.getFullYear(), current.getMonth() + 1, 0); break
            case 'PageUp': next = new Date(current.getFullYear(), current.getMonth() - (e.shiftKey ? 12 : 1), current.getDate()); break
            case 'PageDown': next = new Date(current.getFullYear(), current.getMonth() + (e.shiftKey ? 12 : 1), current.getDate()); break
            case 'Enter':
            case ' ':
                e.preventDefault()
                this.pick(current)
                return
            default: return
        }
        e.preventDefault()
        this.moveFocusTo(next)
    }

    moveFocusTo(date) {
        if (date.getMonth() !== this.view.getMonth() || date.getFullYear() !== this.view.getFullYear()) {
            this.view = new Date(date.getFullYear(), date.getMonth(), 1)
            this.render()
        }
        const key = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
        const target = this.panel.querySelector(`[data-grand-day="${key}"]`)
        if (!target) return
        for (const other of this.panel.querySelectorAll('[data-grand-day]')) other.tabIndex = -1
        target.tabIndex = 0
        target.focus()
    }

    onTimeKey(e, option) {
        const options = Array.from(this.panel.querySelectorAll('[data-grand-time]'))
        const index = options.indexOf(option)
        let next = -1
        if (e.key === 'ArrowDown') next = Math.min(index + 1, options.length - 1)
        else if (e.key === 'ArrowUp') next = Math.max(index - 1, 0)
        else if (e.key === 'Home') next = 0
        else if (e.key === 'End') next = options.length - 1
        else if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault()
            option.click()
            return
        } else return
        e.preventDefault()
        for (const other of options) other.tabIndex = -1
        options[next].tabIndex = 0
        options[next].focus()
    }

    enable(enable = true) {
        this.element.disabled = !enable
        this.toggle.disabled = !enable
        if (!enable) this.closePanel(false)
        return this
    }

    readonly(readonly = true) {
        this.element.readOnly = readonly
        this.toggle.disabled = readonly
        return this
    }

    destroy() {
        this.doc.removeEventListener('mousedown', this._onDocument)
        this.group.parentNode?.insertBefore(this.element, this.group)
        this.group.remove()
        this.element.classList.remove('grand-dateinput')
        instances.delete(this.element)
    }
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
export function initDateInputs(root, culture, texts) {
    const created = []
    const widgetTexts = texts || pageTexts()
    for (const element of collect(root, 'input[data-grand-date]')) {
        if (instances.has(element)) continue
        created.push(createDateInput(element, {
            culture,
            texts: widgetTexts,
            mode: element.getAttribute('data-grand-date'),
            format: element.getAttribute('data-grand-date-format') || null,
            value: element.getAttribute('data-grand-date-value')
        }))
    }
    return created
}
