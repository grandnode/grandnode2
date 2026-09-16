// @vitest-environment jsdom
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { createRequire } from 'node:module'
import { installKendoWidgetShims, normalizeTabStripMarkup, resetDeprecations } from './kendo-widget-shims.js'

const require = createRequire(import.meta.url)
let $

beforeAll(() => {
    const jquery = require('jquery')
    $ = typeof jquery.param === 'function' ? jquery : jquery(window)
})

const shimmed = ['kendoWindow', 'kendoNumericTextBox', 'kendoDropDownList', 'kendoMultiSelect', 'kendoTabStrip']

function clear() {
    for (const name of shimmed) delete $.fn[name]
    delete window.kendo
    resetDeprecations()
}

beforeEach(clear)
afterEach(() => {
    clear()
    vi.restoreAllMocks()
})

/** The smallest GrandAdmin the shims need; each widget records what it was given. */
function grandAdmin(overrides = {}) {
    return {
        culture: () => ({ name: 'pl-PL' }),
        format: vi.fn((value, format) => `${value}|${format}`),
        htmlEncode: vi.fn(text => `enc(${text})`),
        modal: { get: vi.fn(() => modalStub()) },
        numeric: { create: vi.fn(() => numericStub()) },
        select: { create: vi.fn(() => selectStub()), lookup: vi.fn(() => selectStub()) },
        tabs: { init: vi.fn(element => [tabStripStub(element)]) },
        ...overrides
    }
}

function modalStub() {
    const element = document.createElement('div')
    return {
        element,
        open: vi.fn(function () { return this }),
        close: vi.fn(function () { return this }),
        center: vi.fn(function () { return this }),
        title: vi.fn(text => (text === undefined ? 'current' : undefined)),
        destroy: vi.fn()
    }
}

function numericStub() {
    let current = 3
    const text = document.createElement('input')
    return {
        element: document.createElement('input'),
        text,
        min: null,
        max: null,
        value: vi.fn(function (next) {
            if (next === undefined) return current
            current = next
            return this
        }),
        enable: vi.fn(),
        readonly: vi.fn(),
        destroy: vi.fn()
    }
}

function selectStub() {
    let value = 'b'
    return {
        input: document.createElement('select'),
        settings: { labelField: 'text' },
        options: { a: { value: 'a', text: 'Alpha' }, b: { value: 'b', text: 'Beta' } },
        getValue: () => value,
        setValue: vi.fn(next => { value = next }),
        refreshOptions: vi.fn(),
        open: vi.fn(),
        close: vi.fn(),
        enable: vi.fn(),
        disable: vi.fn(),
        destroy: vi.fn()
    }
}

function tabStripStub(element) {
    return {
        element,
        items: Array.from(element.querySelectorAll('ul > li')),
        panes: Array.from(element.querySelectorAll('.tab-content > .tab-pane')),
        select: vi.fn(function (index) { return index === undefined ? 1 : this }),
        append: vi.fn()
    }
}

describe('installKendoWidgetShims', () => {
    it('does not replace Kendo when it is loaded', () => {
        const kendoWindow = function () { }
        const win = { jQuery: { fn: { kendoWindow } }, GrandAdmin: grandAdmin() }
        expect(installKendoWidgetShims(win)).toBe(false)
        expect(win.jQuery.fn.kendoWindow).toBe(kendoWindow)
    })

    it('refuses to install without admin.ui.js', () => {
        const error = vi.spyOn(console, 'error').mockImplementation(() => { })
        expect(installKendoWidgetShims({ jQuery: { fn: {} }, GrandAdmin: {} })).toBe(false)
        expect(error).toHaveBeenCalled()
    })

    it('registers every shimmed widget name', () => {
        expect(installKendoWidgetShims({ jQuery: $, GrandAdmin: grandAdmin() })).toBe(true)
        for (const name of shimmed) expect($.fn[name]).toBeTypeOf('function')
    })

    it('warns once per shimmed name, however many widgets a page builds', () => {
        const warn = vi.spyOn(console, 'warn').mockImplementation(() => { })
        installKendoWidgetShims({ jQuery: $, GrandAdmin: grandAdmin() })
        document.body.innerHTML = '<input id="a"><input id="b">'
        $('#a').kendoNumericTextBox({})
        $('#b').kendoNumericTextBox({})
        const messages = warn.mock.calls.map(call => call[0]).filter(text => text.includes('kendoNumericTextBox'))
        expect(messages).toHaveLength(1)
        expect(messages[0]).toContain('GrandAdmin.numeric')
    })
})

describe('kendoWindow', () => {
    it('opens, closes and titles through GrandAdmin.modal', () => {
        const admin = grandAdmin()
        installKendoWidgetShims({ jQuery: $, GrandAdmin: admin })
        document.body.innerHTML = '<div id="popup"></div>'
        $('#popup').kendoWindow({ title: 'Edit', width: 800, actions: ['Close'] })

        expect(admin.modal.get).toHaveBeenCalledWith(document.getElementById('popup'), expect.objectContaining({ title: 'Edit', width: 800, actions: ['Close'] }))
        const widget = $('#popup').data('kendoWindow')
        expect(widget.center().open()).toBe(widget)
        expect(widget.grandModal.open).toHaveBeenCalled()
        widget.close()
        expect(widget.grandModal.close).toHaveBeenCalled()
        expect(widget.title()).toBe('current')
    })

    it('creates one widget per element', () => {
        const admin = grandAdmin()
        installKendoWidgetShims({ jQuery: $, GrandAdmin: admin })
        document.body.innerHTML = '<div id="popup"></div>'
        $('#popup').kendoWindow({})
        $('#popup').kendoWindow({})
        expect(admin.modal.get).toHaveBeenCalledTimes(1)
    })
})

describe('kendoNumericTextBox', () => {
    it('passes the Kendo options through and reads and writes the value', () => {
        const admin = grandAdmin()
        installKendoWidgetShims({ jQuery: $, GrandAdmin: admin })
        document.body.innerHTML = '<input id="qty" value="3">'
        $('#qty').kendoNumericTextBox({ format: 'n2', decimals: 2, min: 0, max: 10, step: 1 })

        expect(admin.numeric.create).toHaveBeenCalledWith(document.getElementById('qty'), { format: 'n2', decimals: 2, min: 0, max: 10, step: 1 })
        const widget = $('#qty').data('kendoNumericTextBox')
        expect(widget.value()).toBe(3)
        expect(widget.value(7)).toBe(widget)
        expect(widget.value()).toBe(7)
        widget.enable(false)
        expect(widget.grandNumeric.enable).toHaveBeenCalledWith(false)
    })
})

describe('kendoDropDownList and kendoMultiSelect', () => {
    it('maps dataSource, dataTextField and dataValueField onto GrandAdmin.select', () => {
        const admin = grandAdmin()
        installKendoWidgetShims({ jQuery: $, GrandAdmin: admin })
        document.body.innerHTML = '<select id="list"></select>'
        $('#list').kendoDropDownList({
            dataSource: { transport: { read: { url: '/Admin/Product/Search' } } },
            dataTextField: 'Title',
            dataValueField: 'Key',
            optionLabel: 'pick one'
        })
        expect(admin.select.create).toHaveBeenCalledWith(document.getElementById('list'), {
            mode: 'single', url: '/Admin/Product/Search', textField: 'Title', valueField: 'Key', placeholder: 'pick one'
        })
    })

    it('uses the lookup widget for an input that holds an id', () => {
        const admin = grandAdmin()
        installKendoWidgetShims({ jQuery: $, GrandAdmin: admin })
        document.body.innerHTML = '<input id="vendor" value="v1">'
        $('#vendor').kendoDropDownList({ dataSource: '/Admin/Vendor/Search' })
        expect(admin.select.lookup).toHaveBeenCalledWith(document.getElementById('vendor'), expect.objectContaining({ url: '/Admin/Vendor/Search' }))
    })

    it('reads value, text and dataItem and fires change', () => {
        installKendoWidgetShims({ jQuery: $, GrandAdmin: grandAdmin() })
        document.body.innerHTML = '<select id="list"></select>'
        $('#list').kendoDropDownList({})
        const widget = $('#list').data('kendoDropDownList')
        expect(widget.value()).toBe('b')
        expect(widget.text()).toBe('Beta')
        expect(widget.dataItem()).toEqual({ value: 'b', text: 'Beta' })
        widget.value('a')
        expect(widget.grandSelect.setValue).toHaveBeenCalledWith('a', true)

        const changed = vi.fn()
        widget.grandSelect.input.addEventListener('change', changed)
        widget.trigger('change')
        expect(changed).toHaveBeenCalled()
    })

    it('answers an array for a multiselect', () => {
        installKendoWidgetShims({ jQuery: $, GrandAdmin: grandAdmin() })
        document.body.innerHTML = '<select id="many" multiple></select>'
        $('#many').kendoMultiSelect({})
        expect($('#many').data('kendoMultiSelect').value()).toEqual(['b'])
    })
})

describe('kendoTabStrip', () => {
    const kendoMarkup = '<div id="strip"><ul><li>One</li><li>Two</li></ul><div>first</div><div>second</div></div>'

    it('labels bare Kendo markup with the Bootstrap classes the strip reads', () => {
        document.body.innerHTML = kendoMarkup
        expect(normalizeTabStripMarkup(document.getElementById('strip'))).toBe(true)
        const strip = document.getElementById('strip')
        expect(strip.querySelector('ul').className).toContain('nav-tabs')
        expect(strip.querySelectorAll('ul > li.nav-item')).toHaveLength(2)
        expect(strip.querySelectorAll('ul > li > a.nav-link')).toHaveLength(2)
        expect(strip.querySelectorAll(':scope > .tab-content > .tab-pane')).toHaveLength(2)
    })

    it('leaves markup that already carries the classes alone', () => {
        document.body.innerHTML = '<div id="strip"><ul class="nav nav-tabs"><li class="nav-item"><a class="nav-link">One</a></li></ul><div class="tab-content"><div class="tab-pane">first</div></div></div>'
        normalizeTabStripMarkup(document.getElementById('strip'))
        expect(document.querySelectorAll('#strip > .tab-content')).toHaveLength(1)
        expect(document.querySelectorAll('#strip .tab-pane')).toHaveLength(1)
    })

    it('builds the strip through GrandAdmin.tabs and appends tabs', () => {
        const admin = grandAdmin()
        installKendoWidgetShims({ jQuery: $, GrandAdmin: admin })
        document.body.innerHTML = kendoMarkup
        $('#strip').kendoTabStrip({})

        expect(admin.tabs.init).toHaveBeenCalledWith(document.getElementById('strip'))
        const widget = $('#strip').data('kendoTabStrip')
        expect(widget.select()).toBe(1)
        widget.select(0)
        expect(widget.grandTabStrip.select).toHaveBeenLastCalledWith(0)
        widget.append({ text: 'Third', content: '<p>third</p>' })
        expect(widget.grandTabStrip.append).toHaveBeenCalledWith({ text: 'Third', content: '<p>third</p>' })
        expect(widget.items()).toHaveLength(2)
        expect(widget.contentElement(0).textContent).toBe('first')
    })

    it('calls the Kendo select handler when a tab is shown', () => {
        installKendoWidgetShims({ jQuery: $, GrandAdmin: grandAdmin() })
        document.body.innerHTML = kendoMarkup
        const select = vi.fn()
        $('#strip').kendoTabStrip({ select })
        document.getElementById('strip').dispatchEvent(new CustomEvent('grand-tabstrip:show', { detail: { index: 1 } }))
        expect(select).toHaveBeenCalledWith(expect.objectContaining({ item: expect.anything() }))
    })
})

describe('window.kendo helpers', () => {
    it('maps toString, htmlEncode and culture onto GrandAdmin', () => {
        const warn = vi.spyOn(console, 'warn').mockImplementation(() => { })
        const admin = grandAdmin()
        const win = { jQuery: $, GrandAdmin: admin }
        installKendoWidgetShims(win)

        expect(win.kendo.toString(12.5, 'n2')).toBe('12.5|n2')
        expect(win.kendo.htmlEncode('<b>')).toBe('enc(<b>)')
        expect(win.kendo.culture()).toEqual({ name: 'pl-PL' })
        expect(win.kendo.parseDate('2026-09-16T00:00:00').getFullYear()).toBe(2026)
        expect(win.kendo.parseDate('nonsense')).toBeNull()
        expect(warn.mock.calls.map(call => call[0]).filter(text => text.includes('kendo.toString'))).toHaveLength(1)
    })

    it('does not define the Kendo widget namespace', () => {
        const win = { jQuery: $, GrandAdmin: grandAdmin() }
        installKendoWidgetShims(win)
        expect(win.kendo.ui).toBeUndefined()
        expect(win.kendo.data).toBeUndefined()
    })
})
