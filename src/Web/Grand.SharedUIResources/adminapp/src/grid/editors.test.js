// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { createEditor, serializeValue } from './editors.js'
import { createDateInput } from '../ui/datetime.js'
import { createNumeric } from '../ui/numeric.js'
import { createSelect } from '../ui/select.js'

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
    afterEach(() => { delete globalThis.GrandAdmin })

    it('is a Bootstrap 5 select, so it draws its chevron, when only the grid bundle is loaded', () => {
        const editor = createEditor({
            column: { field: 'CategoryId', editor: 'Select', options: [{ value: '1', text: 'One' }] },
            value: '1',
            culture: pl
        })
        expect(editor.element.tagName).toBe('SELECT')
        expect(editor.element.className).toContain('form-select')
        expect(editor.element.className).not.toContain('form-control')
        expect(editor.getValue()).toBe('1')
    })

    it('keeps the search box above the list of a remote column without the panel bundle', async () => {
        const loadOptions = vi.fn(async () => [{ value: 'c1', text: 'Computers' }])
        const editor = createEditor({
            column: { field: 'CategoryId', editor: 'Select', optionsUrl: '/Search/Category', optionsFilter: 'startswith' },
            value: '',
            loadOptions
        })
        expect(editor.element.querySelector('input[type=search]')).not.toBeNull()
        expect(editor.element.querySelector('select')).not.toBeNull()
    })
})

const texts = { select: 'Select...', noRecords: 'No records' }

/** admin.ui.js as a panel page loads it, with the server of the test behind the lists. */
const withSelectWidget = fetchJson => {
    globalThis.GrandAdmin = { select: { create: (el, o) => createSelect(el, { texts, fetchJson, ...o }) } }
}

/** Opens the list of a cell the way a person does, and types into the field itself. */
const openList = async (editor, query) => {
    const input = editor.element.querySelector('.ts-control input')
    editor.focus()
    //jsdom does not focus an element that is only .focus()-ed, so the list is asked to open
    editor.widget.open()
    if (query != null) {
        input.value = query
        input.dispatchEvent(new Event('input', { bubbles: true }))
    }
    //Tom Select redraws the list on a throttle
    await vi.waitFor(() => expect(editor.widget.dropdown.querySelector('.option, .no-results')).not.toBeNull())
    return input
}

const shown = editor => Array.from(editor.widget.dropdown.querySelectorAll('.ts-dropdown-content .option')).map(o => o.textContent)

describe('the select editor of a row, with admin.ui.js loaded', () => {
    beforeEach(() => { document.body.innerHTML = '' })
    afterEach(() => {
        delete globalThis.GrandAdmin
        document.querySelectorAll('.ts-dropdown').forEach(node => node.remove())
    })

    const fixed = (extra = {}) => {
        const editor = createEditor({
            column: {
                field: 'CustomerGroupId', editor: 'Select', optionLabel: 'All',
                options: [{ value: '1', text: 'Administrators' }, { value: '2', text: 'Registered' }],
                ...extra
            },
            value: '2',
            culture: pl
        })
        document.body.appendChild(editor.element)
        return editor
    }

    it('is the list of the forms, in the size of a row, with no search box of its own', () => {
        withSelectWidget()
        const editor = fixed()
        expect(editor.element.className).toBe('grand-grid-select')
        expect(editor.element.querySelector('input[type=search]')).toBeNull()
        expect(editor.element.querySelector('.ts-wrapper').className).toContain('form-select-sm')
        //the value the row carries is the one in the field, and it is the one that is posted
        expect(editor.getValue()).toBe('2')
        expect(editor.getText()).toBe('Registered')
    })

    it('hangs the list on the body, so no table or modal can clip it', () => {
        withSelectWidget()
        const editor = fixed()
        expect(editor.widget.dropdown.parentElement).toBe(document.body)
        expect(editor.widget.dropdown.classList.contains('grand-grid-dropdown')).toBe(true)
    })

    it('narrows a fixed list on what is typed in the field itself', async () => {
        withSelectWidget()
        const editor = fixed()
        await openList(editor, 'Reg')
        expect(shown(editor)).toEqual(['Registered'])
    })

    it('asks the server for the rows of a filtered column, with the query it always sent', async () => {
        const fetchJson = vi.fn(async () => ({ Data: [{ Id: 'c2', Name: 'Notebooks' }] }))
        withSelectWidget(fetchJson)
        const editor = createEditor({
            column: {
                field: 'CategoryId', editor: 'Select', optionsUrl: '/Search/Category',
                optionsFilter: 'startswith', textField: 'Category', optionLabel: 'Select category...'
            },
            item: { CategoryId: 'c9', Category: 'Old one' },
            value: 'c9',
            culture: pl
        })
        document.body.appendChild(editor.element)
        //the row keeps its value and its text until something else is chosen
        expect(editor.getValue()).toBe('c9')
        expect(editor.getText()).toBe('Old one')

        await openList(editor, 'Note')
        //the query DataSourceRequestFilterBinder parses, the one the search box above the
        //list sent before and the one a lookup on a screen sends
        await vi.waitFor(() => expect(fetchJson.mock.calls.some(([url]) => url.includes('Note'))).toBe(true))
        const url = fetchJson.mock.calls.find(([called]) => called.includes('Note'))[0]
        expect(url.startsWith('/Search/Category?')).toBe(true)
        expect(url).toContain('filter%5Blogic%5D=and')
        expect(url).toContain('filter%5Bfilters%5D%5B0%5D%5Bvalue%5D=Note')
        expect(url).toContain('filter%5Bfilters%5D%5B0%5D%5Boperator%5D=startswith')
        expect(url).toContain('filter%5Bfilters%5D%5B0%5D%5Bfield%5D=Name')
        expect(url).toContain('filter%5Bfilters%5D%5B0%5D%5BignoreCase%5D=true')
    })

    it('asks a remote column without an operator for its list once, as it always did', async () => {
        withSelectWidget()
        const loadOptions = vi.fn(async () => [{ value: 's1', text: 'Store one' }])
        const editor = createEditor({
            column: { field: 'StoreId', editor: 'Select', optionsUrl: '/Store/List' },
            value: '',
            loadOptions
        })
        document.body.appendChild(editor.element)
        await vi.waitFor(() => expect(editor.widget.options.s1).toBeTruthy())
        expect(loadOptions).toHaveBeenCalledTimes(1)
        expect(loadOptions.mock.calls[0][0]).toBe('/Store/List')
    })

    it('is chosen with the arrows and Enter, and Enter with the list closed saves the row', async () => {
        withSelectWidget()
        let saved = 0
        const editor = createEditor({
            column: { field: 'CustomerGroupId', editor: 'Select', options: [{ value: '1', text: 'One' }, { value: '2', text: 'Two' }] },
            value: '1',
            culture: pl,
            commit: () => { saved += 1 }
        })
        document.body.appendChild(editor.element)
        const input = await openList(editor)
        const press = key => input.dispatchEvent(new KeyboardEvent('keydown', { key, keyCode: { ArrowDown: 40, Enter: 13, Escape: 27 }[key], bubbles: true, cancelable: true }))
        press('ArrowDown')
        press('Enter')
        expect(editor.getValue()).toBe('2')
        expect(saved).toBe(0)
        press('Enter')
        expect(saved).toBe(1)
    })

    it('cancels the cell on Escape, even while the list is open', async () => {
        withSelectWidget()
        let cancelled = 0
        const editor = createEditor({
            column: { field: 'CustomerGroupId', editor: 'Select', options: [{ value: '1', text: 'One' }] },
            value: '1',
            culture: pl,
            cancel: () => { cancelled += 1 }
        })
        document.body.appendChild(editor.element)
        const input = await openList(editor)
        input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true, cancelable: true }))
        expect(cancelled).toBe(1)
    })

    it('refuses an empty choice in a required column and marks the field a screen shows', () => {
        withSelectWidget()
        const editor = fixed({ required: true })
        editor.widget.clear()
        expect(editor.getValue()).toBe('')
        expect(editor.validate()).toBe('required')
        expect(editor.widget.wrapper.classList.contains('is-invalid')).toBe(true)
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
