// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest'
import { createNumeric, initNumeric, toPostValue } from './numeric.js'

//pl-PL: comma decimal separator, non-breaking space groups - the culture that breaks a
//naive replacement of the Kendo NumericTextBox
const pl = {
    name: 'pl-PL',
    numberFormat: { decimal: ',', group: ' ', groupSizes: [3], negativeSign: '-', decimals: 2 }
}
const en = { name: 'en-US', numberFormat: { decimal: '.', group: ',', groupSizes: [3], negativeSign: '-', decimals: 2 } }

describe('toPostValue (kendo.ui.NumericTextBox._update)', () => {
    it('writes the culture decimal separator and no grouping', () => {
        expect(toPostValue(1234.5, 2, pl)).toBe('1234,5')
        expect(toPostValue(1234.5, 2, en)).toBe('1234.5')
    })

    it('drops trailing zeros the way Number#toString does', () => {
        expect(toPostValue(1.5, 2, en)).toBe('1.5')
        expect(toPostValue(2, 2, en)).toBe('2')
    })

    it('rounds to the configured decimals', () => {
        expect(toPostValue(1.005, 2, en)).toBe('1.01')
        expect(toPostValue(0.123456789, 8, en)).toBe('0.12345679')
        expect(toPostValue(7.9, 0, en)).toBe('8')
    })

    it('writes an empty string for no value', () => {
        expect(toPostValue(null, 2, en)).toBe('')
        expect(toPostValue(undefined, 2, en)).toBe('')
    })
})

describe('numeric input', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
    })

    function build(value, options) {
        document.body.innerHTML = `<input id="price" name="Price" class="form-control" value="${value}">`
        const element = document.getElementById('price')
        return { element, widget: createNumeric(element, options) }
    }

    it('hides the named input and posts through it, as Kendo did', () => {
        const { element, widget } = build('1234,5', { culture: pl, format: 'n2', decimals: 2 })
        expect(element.style.display).toBe('none')
        expect(element.name).toBe('Price')
        expect(element.value).toBe('1234,5')
        expect(widget.text.name).toBe('')
        expect(widget.text.value).toBe('1 234,50')
    })

    it('reformats on blur and keeps the posted value in the culture format', () => {
        const { element, widget } = build('0', { culture: pl, format: 'n2', decimals: 2 })
        widget.text.value = '12 345,678'
        widget.text.dispatchEvent(new window.Event('blur'))
        expect(element.value).toBe('12345,68')
        expect(widget.text.value).toBe('12 345,68')
    })

    it('shows the editable value on focus', () => {
        const { widget } = build('1234.5', { culture: en, format: 'n2', decimals: 2 })
        widget.text.dispatchEvent(new window.Event('focus'))
        expect(widget.text.value).toBe('1234.5')
    })

    it('keeps the last value when the text is not a number', () => {
        const { element, widget } = build('5', { culture: en, format: 'n2', decimals: 2 })
        widget.text.value = 'abc'
        widget.text.dispatchEvent(new window.Event('blur'))
        expect(element.value).toBe('5')
    })

    it('posts an integer for the Int32 templates', () => {
        const { element, widget } = build('3', { culture: pl, format: '#', decimals: 0 })
        expect(widget.text.value).toBe('3')
        widget.text.value = '7,9'
        widget.text.dispatchEvent(new window.Event('blur'))
        expect(element.value).toBe('8')
    })

    it('enables and disables both inputs, as checkOverriddenStoreValue needs', () => {
        const { element, widget } = build('1', { culture: en, format: 'n2', decimals: 2 })
        widget.enable(false)
        expect(widget.text.disabled).toBe(true)
        expect(element.hasAttribute('disabled')).toBe(true)
        widget.enable()
        expect(widget.text.disabled).toBe(false)
        expect(element.hasAttribute('disabled')).toBe(false)
    })

    it('upgrades every input marked by the editor templates once', () => {
        document.body.innerHTML = '<input id="a" value="1" data-grand-numeric=\'{"format":"n4","decimals":4}\'>'
        expect(initNumeric(document, en).length).toBe(1)
        expect(initNumeric(document, en).length).toBe(0)
        expect(document.querySelector('.grand-numeric').value).toBe('1.0000')
    })
})
