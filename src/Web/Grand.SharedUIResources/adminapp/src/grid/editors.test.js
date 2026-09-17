// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { createEditor } from './editors.js'
import { createDateInput } from '../ui/datetime.js'

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
