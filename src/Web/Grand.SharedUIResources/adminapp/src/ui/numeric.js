//GrandAdmin.numeric - the replacement for the Kendo NumericTextBox of the Decimal,
//Double and Int32 editor templates.
//
//It keeps Kendo's split exactly, because the posted value is a server contract:
//  - the element the template rendered keeps its name and holds the value the MVC model
//    binder reads in the request culture: rounded to the column decimals, written with
//    Number#toString (no grouping) and the invariant point swapped for the culture's
//    decimal separator - kendo.ui.NumericTextBox._update, verbatim;
//  - a second, unnamed input shows the same number formatted with the column format
//    ("n2", "n4", "#"), and is the one the user types in.
//The named element is hidden, as Kendo hid it, so jquery.validate keeps ignoring it.

import { formatNumber, parseNumber, normalizeCulture } from '../grid/format.js'
import { collect } from './culture.js'

const instances = new WeakMap()

function round(value, decimals) {
    if (decimals == null) return value
    const factor = Math.pow(10, decimals)
    //the Number.EPSILON nudge keeps 1.005 at 2 decimals from falling to 1.00
    return Math.round((Math.abs(value) * factor) + Number.EPSILON * factor) / factor * (value < 0 ? -1 : 1)
}

/** The string Kendo put in the hidden input: culture decimal separator, no grouping. */
export function toPostValue(value, decimals, cultureData) {
    if (value == null || Number.isNaN(value)) return ''
    const nf = normalizeCulture(cultureData).numberFormat
    let text = round(value, decimals).toString()
    if (text.includes('e')) text = round(Number(text), decimals ?? 20).toFixed(Math.min(decimals ?? 20, 20))
    return text.replace('.', nf.decimal)
}

export class NumericInput {
    constructor(element, options = {}) {
        const doc = element.ownerDocument
        this.element = element
        this.culture = options.culture
        this.format = options.format || 'n'
        this.decimals = options.decimals == null ? null : Number(options.decimals)
        this.min = options.min == null ? null : Number(options.min)
        this.max = options.max == null ? null : Number(options.max)
        this.step = options.step == null ? 1 : Number(options.step)

        this.text = doc.createElement('input')
        this.text.type = 'text'
        this.text.autocomplete = 'off'
        this.text.inputMode = this.decimals === 0 ? 'numeric' : 'decimal'
        //the visible control is a Bootstrap form control whatever the editor template put on
        //the named element (most put nothing, which is why these fields looked unstyled)
        this.text.className = element.className.replace(/\b(input-validation-error|valid|text-box|single-line)\b/g, '').trim()
        if (!this.text.classList.contains('form-control')) this.text.classList.add('form-control')
        this.text.setAttribute('style', element.getAttribute('style') || '')
        this.text.classList.add('grand-numeric')
        if (element.placeholder) this.text.placeholder = element.placeholder
        if (element.title) this.text.title = element.title
        if (element.hasAttribute('readonly')) this.text.readOnly = true
        if (element.hasAttribute('disabled')) this.text.disabled = true
        this.text.setAttribute('role', 'spinbutton')
        if (this.min != null) this.text.setAttribute('aria-valuemin', String(this.min))
        if (this.max != null) this.text.setAttribute('aria-valuemax', String(this.max))
        if (element.id) this.text.setAttribute('aria-labelledby', `${element.id}-label`)

        //the spinner Kendo drew, in Bootstrap's input group: the arrows are not tab stops,
        //because the field itself answers ArrowUp and ArrowDown
        this.group = doc.createElement('div')
        this.group.className = 'grand-numeric-group input-group'
        this.spin = doc.createElement('span')
        this.spin.className = 'grand-numeric-spin'
        this.spin.setAttribute('aria-hidden', 'true')
        this.up = this.spinButton(doc, 'grand-numeric-up', 'bi bi-caret-up-fill', 1)
        this.down = this.spinButton(doc, 'grand-numeric-down', 'bi bi-caret-down-fill', -1)
        this.spin.appendChild(this.up)
        this.spin.appendChild(this.down)

        element.parentNode.insertBefore(this.group, element.nextSibling)
        this.group.appendChild(this.text)
        this.group.appendChild(this.spin)
        element.style.display = 'none'

        this.value(parseNumber(element.value, this.culture))

        this.text.addEventListener('focus', () => {
            this.text.value = toPostValue(this._value, this.decimals, this.culture)
            this.text.select?.()
        })
        this.text.addEventListener('blur', () => this.value(parseNumber(this.text.value, this.culture)))
        this.text.addEventListener('keydown', e => {
            if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
                e.preventDefault()
                this.stepBy(e.key === 'ArrowUp' ? 1 : -1)
            }
        })
    }

    spinButton(doc, className, icon, direction) {
        const button = doc.createElement('button')
        button.type = 'button'
        button.className = className
        button.tabIndex = -1
        const glyph = doc.createElement('i')
        glyph.className = icon
        button.appendChild(glyph)
        button.addEventListener('mousedown', e => e.preventDefault())
        button.addEventListener('click', () => {
            if (this.text.disabled || this.text.readOnly) return
            this.text.focus()
            this.stepBy(direction)
        })
        return button
    }

    /** One step up or down from what is in the field right now. */
    stepBy(direction) {
        const current = parseNumber(this.text.value, this.culture)
        const base = current == null || Number.isNaN(current) ? 0 : current
        this.value(base + direction * this.step)
        this.text.value = toPostValue(this._value, this.decimals, this.culture)
        return this
    }

    /** Reads or writes the number, updating both the posted and the displayed value. */
    value(next) {
        if (next === undefined) return this._value
        let value = typeof next === 'number' ? next : parseNumber(next, this.culture)
        //text that is not a number in this culture falls back to the value it had, and the
        //control says so rather than silently changing under the user
        const refused = value != null && Number.isNaN(value)
        this.text.classList.toggle('is-invalid', refused)
        if (refused) value = this._value ?? null
        if (value != null) {
            value = round(value, this.decimals)
            if (this.min != null && value < this.min) value = this.min
            if (this.max != null && value > this.max) value = this.max
        }
        this._value = value
        this.element.value = toPostValue(value, this.decimals, this.culture)
        this.text.value = value == null ? '' : formatNumber(value, this.format, this.culture)
        this.text.setAttribute('aria-valuenow', this.element.value)
        return this
    }

    enable(enable = true) {
        this.text.disabled = !enable
        this.group.classList.toggle('grand-numeric-disabled', !enable)
        if (enable) this.element.removeAttribute('disabled')
        else this.element.setAttribute('disabled', 'disabled')
        return this
    }

    readonly(readonly = true) {
        this.text.readOnly = readonly
        this.group.classList.toggle('grand-numeric-readonly', readonly)
        return this
    }

    destroy() {
        this.group.remove()
        this.element.style.display = ''
        instances.delete(this.element)
    }
}

/** Creates (or returns) the numeric widget of an element. */
export function createNumeric(element, options = {}) {
    if (!element) return null
    let widget = instances.get(element)
    if (!widget) {
        widget = new NumericInput(element, options)
        instances.set(element, widget)
        element.grandNumeric = widget
        if (window.jQuery) window.jQuery.data(element, 'grandNumeric', widget)
    }
    return widget
}

export function getNumeric(element) {
    return element ? instances.get(element) || null : null
}

/**
 * Upgrades every <input data-grand-numeric='{"format":"n2","decimals":2}'> under a root.
 * The culture comes from the page island unless the attribute carries its own.
 */
export function initNumeric(root, culture) {
    const created = []
    for (const element of collect(root, 'input[data-grand-numeric]')) {
        if (instances.has(element)) continue
        let options = {}
        try {
            options = JSON.parse(element.getAttribute('data-grand-numeric') || '{}')
        } catch (error) {
            console.error('[admin-ui] invalid numeric options', error)
        }
        created.push(createNumeric(element, { culture, ...options }))
    }
    return created
}
