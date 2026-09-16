// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest'
import { createDateInput, initDateInputs, postPattern } from './datetime.js'

const pl = {
    name: 'pl-PL',
    calendar: { shortDate: 'dd.MM.yyyy', shortTime: 'HH:mm', dateSeparator: '.', timeSeparator: ':' }
}
const en = {
    name: 'en-US',
    calendar: { shortDate: 'M/d/yyyy', shortTime: 'h:mm tt', dateSeparator: '/', timeSeparator: ':', am: 'AM', pm: 'PM' }
}

function build(mode, value, culture) {
    document.body.innerHTML = `<input id="d" name="StartDate" value="ignored" data-grand-date="${mode}" data-grand-date-value="${value}">`
    const element = document.getElementById('d')
    return { element, widget: createDateInput(element, { culture, mode, value }) }
}

describe('date input', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
    })

    it('uses the pattern the Kendo pickers defaulted to', () => {
        expect(postPattern('date', pl)).toBe('dd.MM.yyyy')
        expect(postPattern('datetime', pl)).toBe('dd.MM.yyyy HH:mm')
        expect(postPattern('time', pl)).toBe('HH:mm')
    })

    it('posts the culture short date and edits in a native date input', () => {
        const { element, widget } = build('date', '2026-09-16T00:00:00', pl)
        expect(element.style.display).toBe('none')
        expect(element.value).toBe('16.09.2026')
        expect(widget.picker.type).toBe('date')
        expect(widget.picker.value).toBe('2026-09-16')
    })

    it('posts the culture short date and time for a datetime editor', () => {
        const { element } = build('datetime', '2026-09-16T14:05:00', en)
        expect(element.value).toBe('9/16/2026 2:05 PM')
    })

    it('posts HH:mm for a time editor', () => {
        const { element, widget } = build('time', '2026-09-16T08:07:00', en)
        expect(element.value).toBe('08:07')
        expect(widget.picker.type).toBe('time')
        expect(widget.picker.value).toBe('08:07')
    })

    it('writes the new value into the named input when the picker changes', () => {
        const { element, widget } = build('date', '2026-09-16T00:00:00', pl)
        widget.picker.value = '2027-01-02'
        widget.picker.dispatchEvent(new window.Event('change'))
        expect(element.value).toBe('02.01.2027')
    })

    it('keeps the day when only the time changes', () => {
        const { element, widget } = build('time', '2026-09-16T08:07:00', en)
        widget.picker.value = '23:45'
        widget.picker.dispatchEvent(new window.Event('change'))
        expect(element.value).toBe('23:45')
        expect(widget.value().getFullYear()).toBe(2026)
    })

    it('clears the posted value when the picker is emptied', () => {
        const { element, widget } = build('date', '2026-09-16T00:00:00', pl)
        widget.picker.value = ''
        widget.picker.dispatchEvent(new window.Event('change'))
        expect(element.value).toBe('')
    })

    it('leaves an empty nullable editor empty', () => {
        document.body.innerHTML = '<input id="d" name="X" data-grand-date="date" data-grand-date-value="">'
        initDateInputs(document, pl)
        expect(document.getElementById('d').value).toBe('')
    })

    it('upgrades each marked input once', () => {
        document.body.innerHTML = '<input id="d" data-grand-date="date" data-grand-date-value="2026-09-16T00:00:00">'
        expect(initDateInputs(document, pl).length).toBe(1)
        expect(initDateInputs(document, pl).length).toBe(0)
    })
})
