// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { createEditor, serializeValue } from './editors.js'
import { createDateInput } from '../ui/datetime.js'
import { createNumeric } from '../ui/numeric.js'

const pl = {
    name: 'pl-PL',
    calendar: {
        shortDate: 'dd.MM.yyyy', shortTime: 'HH:mm', longDate: 'd MMMM yyyy',
        dateSeparator: '.', timeSeparator: ':', firstDayOfWeek: 1
    }
}

describe('the date editor of a row', () => {
    beforeEach(() => { document.body.innerHTML = '' })
    afterEach(() => { delete globalThis.GrandAdmin })

    it('falls back to the native input when only the grid bundle is loaded', () => {
        const editor = createEditor({ column: { field: 'StartDate', editor: 'Date' }, value: '2026-09-16T14:05:00', culture: pl })
        expect(editor.element.tagName).toBe('INPUT')
        expect(editor.element.type).toBe('date')
        expect(editor.getValue()).toBe('2026-09-16')
    })

    it('uses the panel picker when admin.ui.js is loaded, and posts the same ISO string', () => {
        globalThis.GrandAdmin = { dateInput: { create: (el, o) => createDateInput(el, o) } }
        const editor = createEditor({ column: { field: 'StartDate', editor: 'Date' }, value: '2026-09-16T00:00:00', culture: pl })
        document.body.appendChild(editor.element)
        //what the user sees is the culture's short date, not the browser locale's
        expect(editor.element.querySelector('input').value).toBe('16.09.2026')
        //what the row posts is what it posted before
        expect(editor.getValue()).toBe('2026-09-16')
    })

    it('keeps the minute of a datetime column', () => {
        globalThis.GrandAdmin = { dateInput: { create: (el, o) => createDateInput(el, o) } }
        const editor = createEditor({ column: { field: 'StartDate', editor: 'DateTime' }, value: '2026-09-16T14:05:00', culture: pl })
        document.body.appendChild(editor.element)
        expect(editor.element.querySelector('input').value).toBe('16.09.2026 14:05')
        expect(editor.getValue()).toBe('2026-09-16T14:05:00')
    })

    it('answers null for an empty cell and refuses it when the column is required', () => {
        globalThis.GrandAdmin = { dateInput: { create: (el, o) => createDateInput(el, o) } }
        const editor = createEditor({ column: { field: 'StartDate', editor: 'Date', required: true }, value: null, culture: pl })
        expect(editor.getValue()).toBeNull()
        expect(editor.validate()).toBe('required')
    })
})

describe('the select editor of a row', () => {
    it('is a Bootstrap 5 select, so it draws its chevron', () => {
        const editor = createEditor({
            column: { field: 'CategoryId', editor: 'Select', options: [{ value: '1', text: 'One' }] },
            value: '1',
            culture: pl
        })
        expect(editor.element.className).toContain('form-select')
        expect(editor.element.className).not.toContain('form-control')
    })
})

const plNumbers = {
    name: 'pl-PL',
    numberFormat: { decimal: ',', group: ' ', groupSizes: [3], negativeSign: '-', decimals: 2 },
    calendar: pl.calendar
}

const enUs = {
    name: 'en-US',
    numberFormat: { decimal: '.', group: ',', groupSizes: [3], negativeSign: '-', decimals: 2 }
}

const withNumericWidget = () => {
    globalThis.GrandAdmin = { numeric: { create: (el, o) => createNumeric(el, o) } }
}

const numericEditor = (column, value, culture = plNumbers) =>
    createEditor({ column: { editor: 'Numeric', ...column }, value, culture })

describe('the numeric editor of a row', () => {
    beforeEach(() => { document.body.innerHTML = '' })
    afterEach(() => { delete globalThis.GrandAdmin })

    it('is a plain text box when only the grid bundle is loaded', () => {
        const editor = numericEditor({ field: 'Rate', decimals: 8 }, 1.5)
        expect(editor.element.tagName).toBe('INPUT')
        expect(editor.element.className).toContain('form-control-sm')
        expect(editor.getValue()).toBe(1.5)
    })

    it('is the numeric control of the forms when admin.ui.js is loaded, in the size of a row', () => {
        withNumericWidget()
        const editor = numericEditor({ field: 'Rate', decimals: 8, min: 0, max: 100 }, 1.5)
        document.body.appendChild(editor.element)
        const field = editor.element.querySelector('input.grand-numeric')
        expect(editor.element.className).toBe('grand-grid-numeric')
        expect(editor.element.querySelector('.grand-numeric-group').className).toContain('input-group-sm')
        expect(field.className).toContain('form-control-sm')
        expect(field.getAttribute('role')).toBe('spinbutton')
        expect(field.getAttribute('aria-valuemin')).toBe('0')
        expect(field.getAttribute('aria-valuemax')).toBe('100')
        expect(field.dataset.field).toBe('Rate')
    })

    it('keeps the arrows out of the tab order and out of a screen reader', () => {
        withNumericWidget()
        const editor = numericEditor({ field: 'Rate', decimals: 8 }, 1.5)
        document.body.appendChild(editor.element)
        expect(editor.element.querySelector('.grand-numeric-spin').getAttribute('aria-hidden')).toBe('true')
        for (const button of editor.element.querySelectorAll('.grand-numeric-spin > button')) {
            expect(button.tabIndex).toBe(-1)
            expect(button.type).toBe('button')
        }
    })

    it('steps with the arrows and stops at the bounds of the column', () => {
        withNumericWidget()
        const editor = numericEditor({ field: 'Percentage', decimals: 4, min: 0, max: 3 }, 1.5)
        document.body.appendChild(editor.element)
        const up = editor.element.querySelector('.grand-numeric-up')
        const down = editor.element.querySelector('.grand-numeric-down')
        up.click()
        expect(editor.getValue()).toBe(2.5)
        up.click()
        expect(editor.getValue()).toBe(3)
        down.click()
        down.click()
        down.click()
        down.click()
        expect(editor.getValue()).toBe(0)
    })

    it('tells the row about a step, the way typing tells it', () => {
        withNumericWidget()
        const editor = numericEditor({ field: 'Percentage', decimals: 4 }, 1.5)
        document.body.appendChild(editor.element)
        let seen = 0
        editor.element.addEventListener('input', () => { seen += 1 })
        editor.element.querySelector('.grand-numeric-up').click()
        const field = editor.element.querySelector('input.grand-numeric')
        field.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }))
        expect(seen).toBe(2)
        expect(editor.getValue()).toBe(3.5)
    })

    it('answers ArrowUp and ArrowDown in the field itself', () => {
        withNumericWidget()
        const editor = numericEditor({ field: 'Rate', decimals: 8 }, 4)
        document.body.appendChild(editor.element)
        const field = editor.element.querySelector('input.grand-numeric')
        field.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }))
        expect(editor.getValue()).toBe(3)
    })

    it('saves with Enter and cancels with Escape', () => {
        withNumericWidget()
        let saved = 0
        let cancelled = 0
        const editor = createEditor({
            column: { field: 'Rate', editor: 'Numeric', decimals: 8 },
            value: 1.5,
            culture: plNumbers,
            commit: () => { saved += 1 },
            cancel: () => { cancelled += 1 }
        })
        document.body.appendChild(editor.element)
        const field = editor.element.querySelector('input.grand-numeric')
        field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }))
        field.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
        expect([saved, cancelled]).toEqual([1, 1])
    })

    it('reads what was typed in the working culture, and posts what it posted before', () => {
        withNumericWidget()
        const column = { field: 'Rate', editor: 'Numeric', decimals: 8 }
        const editor = numericEditor(column, 1.5)
        document.body.appendChild(editor.element)
        editor.element.querySelector('input.grand-numeric').value = '2,25'
        expect(editor.getValue()).toBe(2.25)
        expect(serializeValue(column, editor.getValue(), plNumbers)).toBe('2,25000000')
    })

    it('posts the en-US separator when that is the request culture', () => {
        withNumericWidget()
        const column = { field: 'Rate', editor: 'Numeric', decimals: 8 }
        const editor = numericEditor(column, 1.5, enUs)
        document.body.appendChild(editor.element)
        editor.element.querySelector('input.grand-numeric').value = '2.25'
        expect(editor.getValue()).toBe(2.25)
        expect(serializeValue(column, editor.getValue(), enUs)).toBe('2.25000000')
    })

    it('refuses text that is not a number rather than saving a value nobody typed', () => {
        withNumericWidget()
        const editor = numericEditor({ field: 'Rate', decimals: 8 }, 1.5)
        document.body.appendChild(editor.element)
        editor.element.querySelector('input.grand-numeric').value = 'abc'
        expect(editor.validate()).toBe('number')
    })

    it('edits an Integer column in whole steps and posts a whole number', () => {
        withNumericWidget()
        const column = { field: 'DisplayOrder', editor: 'Integer' }
        const editor = createEditor({ column, value: 3, culture: plNumbers })
        document.body.appendChild(editor.element)
        const field = editor.element.querySelector('input.grand-numeric')
        expect(field.inputMode).toBe('numeric')
        editor.element.querySelector('.grand-numeric-up').click()
        expect(editor.getValue()).toBe(4)
        expect(serializeValue(column, editor.getValue(), plNumbers)).toBe('4')
    })
})
