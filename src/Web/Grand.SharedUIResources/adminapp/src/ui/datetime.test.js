// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest'
import { createDateInput, initDateInputs, postPattern, parseTyped } from './datetime.js'
import { formatDate } from '../grid/format.js'

const pl = {
    name: 'pl-PL',
    calendar: {
        shortDate: 'dd.MM.yyyy', shortTime: 'HH:mm', longDate: 'd MMMM yyyy',
        dateSeparator: '.', timeSeparator: ':', firstDayOfWeek: 1,
        months: ['stycznia', 'lutego', 'marca', 'kwietnia', 'maja', 'czerwca', 'lipca', 'sierpnia', 'września', 'października', 'listopada', 'grudnia'],
        monthsStandalone: ['styczeń', 'luty', 'marzec', 'kwiecień', 'maj', 'czerwiec', 'lipiec', 'sierpień', 'wrzesień', 'październik', 'listopad', 'grudzień'],
        monthsAbbr: ['sty', 'lut', 'mar', 'kwi', 'maj', 'cze', 'lip', 'sie', 'wrz', 'paź', 'lis', 'gru'],
        days: ['niedziela', 'poniedziałek', 'wtorek', 'środa', 'czwartek', 'piątek', 'sobota'],
        daysAbbr: ['nie', 'pon', 'wto', 'śro', 'czw', 'pią', 'sob']
    }
}
const en = {
    name: 'en-US',
    calendar: {
        shortDate: 'M/d/yyyy', shortTime: 'h:mm tt', longDate: 'dddd, MMMM d, yyyy',
        dateSeparator: '/', timeSeparator: ':', am: 'AM', pm: 'PM', firstDayOfWeek: 0
    }
}
const sv = { name: 'sv-SE', calendar: { shortDate: 'yyyy-MM-dd', shortTime: 'HH:mm', firstDayOfWeek: 1 } }
const ar = { name: 'ar-SA', calendar: { shortDate: 'dd/MM/yyyy', shortTime: 'h:mm tt', am: 'ص', pm: 'م', firstDayOfWeek: 0 } }

function build(mode, value, culture, rendered = 'rendered by the server') {
    document.body.innerHTML = `<input id="d" name="StartDate" value="${rendered}" data-grand-date="${mode}" data-grand-date-value="${value}">`
    const element = document.getElementById('d')
    return { element, widget: createDateInput(element, { culture, mode, value }) }
}

const key = (target, name, init = {}) =>
    target.dispatchEvent(new window.KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true, ...init }))

describe('date input', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
    })

    it('uses the pattern the Kendo pickers defaulted to', () => {
        expect(postPattern('date', pl)).toBe('dd.MM.yyyy')
        expect(postPattern('datetime', pl)).toBe('dd.MM.yyyy HH:mm')
        expect(postPattern('time', pl)).toBe('HH:mm')
    })

    it('leaves the value the server rendered alone, as the Kendo pickers did', () => {
        const { element, widget } = build('datetime', '2026-09-16T14:05:09', en, '09/16/2026 2:05:09 PM')
        expect(element.value).toBe('09/16/2026 2:05:09 PM')
        expect(widget.value().getHours()).toBe(14)
    })

    it('is the field itself that the user types in, styled as a Bootstrap control', () => {
        const { element } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        expect(element.classList.contains('form-control')).toBe(true)
        expect(element.type).toBe('text')
        expect(element.closest('.grand-datepicker')).not.toBeNull()
        //there is one input to type in, not a hidden one behind a native picker
        expect(document.querySelectorAll('.grand-datepicker > input').length).toBe(1)
    })

    it('posts the culture short date once a day is picked', () => {
        const { element, widget } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        widget.openPanel()
        widget.panel.querySelector('[data-grand-day="2026-09-02"]').click()
        expect(element.value).toBe('02.09.2026')
    })

    it('posts the culture short date and time for a datetime editor', () => {
        const { element, widget } = build('datetime', '2026-09-16T14:05:00', en, '9/16/2026 2:05 PM')
        widget.openPanel()
        widget.panel.querySelector('[data-grand-day="2026-09-17"]').click()
        //picking a day keeps the time the value already carried
        expect(element.value).toBe('9/17/2026 2:05 PM')
    })

    it('posts HH:mm for a time editor and keeps the day', () => {
        const { element, widget } = build('time', '2026-09-16T08:07:00', en, '08:07')
        widget.openPanel()
        //23:30 is the 47th half hour
        widget.panel.querySelector('[data-grand-time="1410"]').click()
        expect(element.value).toBe('23:30')
        expect(widget.value().getFullYear()).toBe(2026)
    })

    it('clears the posted value from the Clear button', () => {
        const { element, widget } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        widget.openPanel()
        widget.panel.querySelectorAll('.grand-cal-actions .btn')[1].click()
        expect(element.value).toBe('')
        expect(widget.value()).toBeNull()
    })

    it('leaves an empty nullable editor empty', () => {
        document.body.innerHTML = '<input id="d" name="X" value="" data-grand-date="date" data-grand-date-value="">'
        initDateInputs(document, pl, {})
        expect(document.getElementById('d').value).toBe('')
    })

    it('upgrades each marked input once', () => {
        document.body.innerHTML = '<input id="d" data-grand-date="date" data-grand-date-value="2026-09-16T00:00:00">'
        expect(initDateInputs(document, pl, {}).length).toBe(1)
        expect(initDateInputs(document, pl, {}).length).toBe(0)
    })
})

describe('the culture, not the browser locale', () => {
    beforeEach(() => { document.body.innerHTML = '' })

    it('reads the same text as two different days in two cultures', () => {
        //03/04/2026 is 4 March in en-US and 3 April in ar-SA, whatever the browser thinks
        expect(parseTyped('03/04/2026', 'date', en).getMonth()).toBe(2)
        expect(parseTyped('03/04/2026', 'date', ar).getMonth()).toBe(3)
        expect(parseTyped('03.04.2026', 'date', pl).getMonth()).toBe(3)
        expect(parseTyped('2026-04-03', 'date', sv).getMonth()).toBe(3)
    })

    it('reads a datetime with the culture AM/PM designator, including a non-Latin one', () => {
        expect(parseTyped('9/16/2026 8:07 PM', 'datetime', en).getHours()).toBe(20)
        expect(parseTyped('16/09/2026 8:07 م', 'datetime', ar).getHours()).toBe(20)
        expect(parseTyped('16.09.2026 20:07', 'datetime', pl).getHours()).toBe(20)
    })

    it('round-trips every day of a year through what it posts and what it reads back', () => {
        for (const culture of [pl, en, sv, ar]) {
            const pattern = postPattern('date', culture)
            for (let day = 0; day < 365; day++) {
                const date = new Date(2026, 0, 1 + day)
                const text = formatDate(date, pattern, culture)
                const back = parseTyped(text, 'date', culture)
                expect(`${culture.name} ${text}`).toBe(`${culture.name} ${formatDate(back, pattern, culture)}`)
                expect(back.getTime()).toBe(date.getTime())
            }
        }
    })

    it('writes the culture spelling back when the field is typed in', () => {
        const { element, widget } = build('date', '', pl, '')
        element.value = '3-4-2026'
        element.dispatchEvent(new window.Event('change'))
        expect(element.value).toBe('03.04.2026')
        expect(widget.value().getMonth()).toBe(3)
    })

    it('keeps text that is not a date for the server to refuse, and marks it invalid', () => {
        const { element, widget } = build('date', '', pl, '')
        element.value = 'nonsense'
        element.dispatchEvent(new window.Event('change'))
        expect(element.value).toBe('nonsense')
        expect(element.getAttribute('aria-invalid')).toBe('true')
        expect(widget.value()).toBeNull()
    })

    it('starts the calendar week on the day the culture starts it', () => {
        const { widget } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        widget.openPanel()
        expect(Array.from(widget.panel.querySelectorAll('thead th')).map(th => th.textContent))
            .toEqual(['pon', 'wto', 'śro', 'czw', 'pią', 'sob', 'nie'])
        const american = build('date', '2026-09-16T00:00:00', en, '9/16/2026')
        american.widget.openPanel()
        expect(american.widget.panel.querySelector('thead th').textContent).toBe('Sun')
    })

    it('heads the calendar with the standalone month name of the culture', () => {
        const { widget } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        widget.openPanel()
        //"wrzesień", not the genitive "września" a date is written with
        expect(widget.panel.querySelector('.grand-cal-label').textContent).toBe('wrzesień 2026')
    })
})

describe('keyboard and aria', () => {
    beforeEach(() => { document.body.innerHTML = '' })

    it('opens on Alt+ArrowDown and closes on Escape, giving the field the focus back', () => {
        const { element, widget } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        key(element, 'ArrowDown', { altKey: true })
        expect(widget.open).toBe(true)
        expect(element.getAttribute('aria-expanded')).toBe('true')
        key(widget.panel.querySelector('[data-grand-day]'), 'Escape')
        expect(widget.open).toBe(false)
        expect(document.activeElement).toBe(element)
    })

    it('moves a day with the arrows, a week with up and down, and a month with PageDown', () => {
        const { widget } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        widget.openPanel()
        const focused = () => document.activeElement.dataset.grandDay
        expect(focused()).toBe('2026-09-16')
        key(document.activeElement, 'ArrowRight')
        expect(focused()).toBe('2026-09-17')
        key(document.activeElement, 'ArrowDown')
        expect(focused()).toBe('2026-09-24')
        key(document.activeElement, 'PageDown')
        expect(focused()).toBe('2026-10-24')
        key(document.activeElement, 'Home')
        expect(focused()).toBe('2026-10-01')
    })

    it('selects the focused day with Enter', () => {
        const { element, widget } = build('date', '2026-09-16T00:00:00', pl, '16.09.2026')
        widget.openPanel()
        key(document.activeElement, 'ArrowRight')
        key(document.activeElement, 'Enter')
        expect(element.value).toBe('17.09.2026')
        expect(widget.open).toBe(false)
    })

    it('marks the grid up for a screen reader', () => {
        const { widget } = build('date', '2026-09-16T00:00:00', en, '9/16/2026')
        widget.openPanel()
        expect(widget.panel.getAttribute('role')).toBe('dialog')
        expect(widget.panel.querySelector('table').getAttribute('role')).toBe('grid')
        const selected = widget.panel.querySelector('[aria-selected="true"]')
        expect(selected.dataset.grandDay).toBe('2026-09-16')
        expect(selected.getAttribute('aria-label')).toBe('Wednesday, September 16, 2026')
        //one tab stop for the whole grid
        expect(widget.panel.querySelectorAll('[data-grand-day][tabindex="0"]').length).toBe(1)
    })

    it('uses the translated button texts when the island carries them', () => {
        document.body.innerHTML = '<input id="d" data-grand-date="date" data-grand-date-value="">'
        const [widget] = initDateInputs(document, pl, { today: 'Dzisiaj', clear: 'Wyczyść' })
        widget.openPanel()
        expect(Array.from(widget.panel.querySelectorAll('.grand-cal-actions .btn')).map(b => b.textContent))
            .toEqual(['Dzisiaj', 'Wyczyść'])
    })
})
