//Mini-shims for the Kendo widgets a third-party plugin view may still build in script.
//Nothing in this repository calls them any more - the panels were converted in Phase 4 -
//but a plugin written against an older GrandNode keeps its Configure.cshtml, and Kendo is
//no longer loaded. Each shim maps the handful of options and methods those views use onto
//the GrandAdmin widget that replaced the Kendo one, registers the widget under the Kendo
//name so `$(el).data('kendoWindow')` keeps answering, and warns once that it is a
//transitional compatibility layer.
//
//Covered: kendoWindow, kendoNumericTextBox, kendoDropDownList, kendoMultiSelect,
//kendoTabStrip (including append), and kendo.toString / kendo.htmlEncode / kendo.culture.
//Everything else Kendo had is gone; a call to it fails loudly rather than silently doing
//nothing.

const warned = new Set()

/** One deprecation line per shimmed name, however many widgets a page builds. */
export function deprecate(name, replacement) {
    if (warned.has(name)) return
    warned.add(name)
    console.warn(`[GrandAdmin legacy] ${name} is a compatibility shim over ${replacement} and will be removed in a future major release; use ${replacement} instead.`)
}

/** Test seam: forgets which names have already warned. */
export function resetDeprecations() {
    warned.clear()
}

function widgetOf($, element, name) {
    return $.data(element, name) || null
}

function register($, element, name, widget) {
    $.data(element, name, widget)
    return widget
}

/* --- kendoWindow -> GrandAdmin.modal ------------------------------------------------ */

function windowAdapter(modal) {
    return {
        grandModal: modal,
        element: modal.element,
        open() { modal.open(); return this },
        close() { modal.close(); return this },
        center() { modal.center(); return this },
        title(text) {
            const result = modal.title(text)
            return text === undefined ? result : this
        },
        content(html) {
            if (html === undefined) return modal.element.innerHTML
            modal.element.innerHTML = html
            return this
        },
        refresh() { return this },
        destroy() { modal.destroy() }
    }
}

/* --- kendoNumericTextBox -> GrandAdmin.numeric -------------------------------------- */

function numericAdapter(widget) {
    return {
        grandNumeric: widget,
        element: widget.element,
        value(next) {
            const result = widget.value(next)
            return next === undefined ? result : this
        },
        min(value) { if (value !== undefined) widget.min = Number(value); return widget.min },
        max(value) { if (value !== undefined) widget.max = Number(value); return widget.max },
        enable(enable = true) { widget.enable(enable); return this },
        readonly(readonly = true) { widget.readonly(readonly); return this },
        focus() { widget.text.focus() },
        destroy() { widget.destroy() }
    }
}

/* --- kendoDropDownList / kendoMultiSelect -> GrandAdmin.select ---------------------- */

function selectAdapter(widget, { multiple }) {
    const values = () => {
        const value = widget.getValue()
        return Array.isArray(value) ? value : [value]
    }
    return {
        grandSelect: widget,
        element: widget.input,
        value(next) {
            if (next === undefined) return multiple ? values() : String(widget.getValue() ?? '')
            widget.setValue(next, true)
            return this
        },
        text() {
            return values().map(value => widget.options[value]?.[widget.settings.labelField] ?? '').join(', ')
        },
        dataItem() {
            const value = multiple ? values()[0] : widget.getValue()
            return value == null ? null : widget.options[value] ?? null
        },
        select(index) {
            if (index === undefined) return Object.keys(widget.options).indexOf(String(widget.getValue()))
            const option = Object.keys(widget.options)[index]
            if (option !== undefined) widget.setValue(option, true)
            return this
        },
        enable(enable = true) {
            if (enable) widget.enable()
            else widget.disable()
            return this
        },
        refresh() { widget.refreshOptions(false); return this },
        open() { widget.open(); return this },
        close() { widget.close(); return this },
        trigger(name) {
            //Kendo views fire "change" by hand after setting a value
            if (name === 'change') widget.input.dispatchEvent(new Event('change', { bubbles: true }))
            return this
        },
        destroy() { widget.destroy() }
    }
}

/**
 * Kendo took its list from dataSource/dataTextField/dataValueField; GrandAdmin.select
 * takes the same endpoint as `url` plus the field names, so only the names differ.
 */
function selectOptions(options = {}, multiple) {
    const source = options.dataSource
    const url = typeof source === 'string'
        ? source
        : source?.transport?.read?.url ?? (typeof source?.transport?.read === 'string' ? source.transport.read : undefined)
    return {
        mode: multiple ? 'multiple' : 'single',
        url,
        textField: options.dataTextField || 'Name',
        valueField: options.dataValueField || 'Id',
        placeholder: options.optionLabel && typeof options.optionLabel === 'string' ? options.optionLabel : undefined
    }
}

/* --- kendoTabStrip -> GrandAdmin.tabs ----------------------------------------------- */

/**
 * A Kendo tab strip was a bare `<ul><li>` plus one `<div>` per tab. GrandAdmin.tabs reads
 * the Bootstrap markup the tag helper renders, so the plugin's markup is labelled with the
 * classes it expects first. Markup that already carries them is left alone.
 */
export function normalizeTabStripMarkup(element) {
    const doc = element.ownerDocument
    const list = element.querySelector(':scope > ul')
    if (!list) return false
    list.classList.add('nav', 'nav-tabs')
    for (const item of list.querySelectorAll(':scope > li')) {
        item.classList.add('nav-item')
        const link = item.querySelector(':scope > a')
        if (link) link.classList.add('nav-link')
        else {
            //Kendo allowed plain text items; the strip selects on .nav-link
            const anchor = doc.createElement('a')
            anchor.className = 'nav-link'
            anchor.href = '#'
            while (item.firstChild) anchor.appendChild(item.firstChild)
            item.appendChild(anchor)
        }
    }
    let content = element.querySelector(':scope > .tab-content')
    if (!content) {
        content = doc.createElement('div')
        content.className = 'tab-content'
        for (const pane of Array.from(element.children)) {
            if (pane === list) continue
            pane.classList.add('tab-pane')
            content.appendChild(pane)
        }
        element.appendChild(content)
    }
    return true
}

function tabStripAdapter(strip) {
    return {
        grandTabStrip: strip,
        element: strip.element,
        select(index) {
            if (index === undefined) return strip.select()
            const resolved = typeof index === 'number' ? index : strip.items.indexOf(index?.[0] ?? index)
            if (resolved >= 0) strip.select(resolved)
            return this
        },
        append(tab) {
            for (const entry of Array.isArray(tab) ? tab : [tab]) strip.append(entry)
            return this
        },
        //Kendo's items()/contentElement(i) were read by views looking for a pane
        items: () => strip.items,
        contentElement: index => strip.panes[index],
        reload() { return this },
        destroy() { }
    }
}

/* --- window.kendo helpers ----------------------------------------------------------- */

function kendoHelpers(GrandAdmin) {
    return {
        //kendo.toString(value, format) and kendo.format-style number/date output
        toString(value, format, culture) {
            deprecate('kendo.toString', 'GrandAdmin.format')
            return GrandAdmin.format(value, format, culture)
        },
        htmlEncode(text) {
            deprecate('kendo.htmlEncode', 'GrandAdmin.htmlEncode')
            return GrandAdmin.htmlEncode(text)
        },
        //kendo.culture() answered the culture object; there is one culture per page now
        culture() {
            deprecate('kendo.culture', 'GrandAdmin.culture()')
            return GrandAdmin.culture()
        },
        parseDate(value) {
            deprecate('kendo.parseDate', 'GrandAdmin.dateInput')
            const date = value instanceof Date ? value : new Date(value)
            return Number.isNaN(date.getTime()) ? null : date
        }
    }
}

/**
 * Registers the widget shims on jQuery and the helpers on window.kendo.
 * Registers nothing when Kendo itself is loaded, or when jQuery is missing.
 * @returns {boolean} whether the shims were installed
 */
export function installKendoWidgetShims(win) {
    const $ = win.jQuery
    if (!$ || $.fn.kendoWindow) return false
    const GrandAdmin = win.GrandAdmin
    if (!GrandAdmin?.modal) {
        console.error('[GrandAdmin legacy] admin.ui.js must be loaded before admin.legacy.js')
        return false
    }

    $.fn.kendoWindow = function (options = {}) {
        deprecate('$.fn.kendoWindow', 'GrandAdmin.modal')
        return this.each(function () {
            if (widgetOf($, this, 'kendoWindow')) return
            const modal = GrandAdmin.modal.get(this, {
                title: options.title,
                width: options.width,
                actions: options.actions,
                closeOnOverlay: options.modal !== false
            })
            if (modal) register($, this, 'kendoWindow', windowAdapter(modal))
        })
    }

    $.fn.kendoNumericTextBox = function (options = {}) {
        deprecate('$.fn.kendoNumericTextBox', 'GrandAdmin.numeric')
        return this.each(function () {
            if (widgetOf($, this, 'kendoNumericTextBox')) return
            const widget = GrandAdmin.numeric.create(this, {
                format: options.format,
                decimals: options.decimals,
                min: options.min,
                max: options.max,
                step: options.step
            })
            if (widget) register($, this, 'kendoNumericTextBox', numericAdapter(widget))
        })
    }

    const listShim = (name, multiple) => function (options = {}) {
        deprecate(`$.fn.${name}`, 'GrandAdmin.select')
        return this.each(function () {
            if (widgetOf($, this, name)) return
            const config = selectOptions(options, multiple)
            const widget = this.tagName === 'SELECT'
                ? GrandAdmin.select.create(this, config)
                : GrandAdmin.select.lookup(this, config)
            if (widget) register($, this, name, selectAdapter(widget, { multiple }))
        })
    }

    $.fn.kendoDropDownList = listShim('kendoDropDownList', false)
    $.fn.kendoMultiSelect = listShim('kendoMultiSelect', true)

    $.fn.kendoTabStrip = function (options = {}) {
        deprecate('$.fn.kendoTabStrip', 'GrandAdmin.tabs')
        return this.each(function () {
            if (widgetOf($, this, 'kendoTabStrip')) return
            if (!normalizeTabStripMarkup(this)) return
            this.setAttribute('data-grand-tabstrip', JSON.stringify({ selectedIndex: 0 }))
            const [strip] = GrandAdmin.tabs.init(this)
            if (!strip) return
            if (typeof options.select === 'function') {
                //Kendo's select event; the strip fires the global tabstrip_on_tab_select
                this.addEventListener('grand-tabstrip:show', event =>
                    options.select({ item: strip.items[event.detail.index], contentElement: strip.panes[event.detail.index], sender: strip }))
            }
            register($, this, 'kendoTabStrip', tabStripAdapter(strip))
        })
    }

    //the helpers views called off the global; the widget namespace itself stays gone
    win.kendo = { ...kendoHelpers(GrandAdmin), ...(win.kendo || {}) }
    return true
}
