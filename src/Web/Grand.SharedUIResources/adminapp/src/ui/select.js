//GrandAdmin.select - the replacement for the Kendo DropDownList and MultiSelect of the
//Brand, Category, Collection, Vendor, Stores, CustomerGroups and MultiSelect editor
//templates, built on Tom Select (Apache-2.0, bundled - no CDN).
//
//The posted value is unchanged:
//  - a single select is created next to the named <input> the template rendered and
//    writes the chosen Id into it, exactly as the Kendo DropDownList did;
//  - a multiselect enhances the named <select multiple> in place, so the browser posts
//    the selected <option> values under the same name as before.
//
//Remote lists are read with the query DataSourceRequestFilterBinder parses, the one the
//Kendo data sources sent with filter: "startswith" and serverFiltering: true.

import TomSelect from 'tom-select'
import { getJson } from '../grid/transport.js'
import { filteredOptionsUrl } from '../grid/editors.js'
import { collect, pageTexts } from './culture.js'

const instances = new WeakMap()

function rows(body) {
    if (Array.isArray(body)) return body
    return body && Array.isArray(body.Data) ? body.Data : []
}

/** Fetches option rows for a query; an empty query asks for the unfiltered list. */
export function fetchOptions(url, query, { textField = 'Name', valueField = 'Id', fetchJson = getJson, filter = 'startswith' } = {}) {
    const target = query ? filteredOptionsUrl(url, { text: query, operator: filter, field: textField }) : url
    return fetchJson(target).then(body => rows(body).map(row => ({
        value: String(row[valueField] ?? ''),
        text: String(row[textField] ?? '')
    })))
}

function baseSettings(options) {
    const texts = options.texts || {}
    return {
        valueField: 'value',
        labelField: 'text',
        searchField: 'text',
        placeholder: options.placeholder || texts.select || undefined,
        allowEmptyOption: true,
        //the lists are server-filtered; scoring locally would hide rows the server returned
        score: () => () => 1,
        render: {
            option: (data, escape) => `<div>${escape(data.text)}</div>`,
            item: (data, escape) => `<div>${escape(data.text)}</div>`,
            //texts come from the culture island, so a list says the same as a grid does
            no_results: () => `<div class="no-results">${escapeHtml(texts.noRecords || '')}</div>`,
            loading: () => '<div class="spinner"></div>'
        }
    }
}

function escapeHtml(text) {
    return String(text ?? '')
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;').replace(/'/g, '&#39;')
}

/** The Tom Select settings of a list that is read from a server endpoint. */
export function remote(options) {
    if (!options.url) return {}
    let timer = null
    let latest = 0
    //The values the server answered with, per query. Tom Select keeps every option it has
    //ever been given, and these lists are filtered on the server and scored as 1 here, so
    //without this the rows of the wider query stayed in the list next to the answer to the
    //narrower one. The grid's own remote select editor rebuilds its list the same way.
    const answered = new Map()
    return {
        load(query, callback) {
            clearTimeout(timer)
            const request = ++latest
            timer = setTimeout(() => {
                fetchOptions(options.url, query, options)
                    .then(loaded => {
                        //a slower earlier request must not put its rows back over a newer one
                        if (request !== latest) return callback()
                        answered.set(query, new Set(loaded.map(row => row.value)))
                        callback(loaded)
                    })
                    .catch(() => callback())
            }, query ? 250 : 0)
        },
        /** Shows the answer to the query in the box; until it arrives, what is there now. */
        score(search) {
            const allowed = answered.get(search ?? '')
            return option => !allowed || allowed.has(option.value) ? 1 : 0
        },
        //the endpoints answer the whole (limited) list, so the first list is asked for once
        preload: 'focus',
        loadThrottle: null
    }
}

/**
 * Enhances a <select> (single or multiple) in place. The select keeps its name, so the
 * form posts what it posted before.
 */
export function createSelect(element, options = {}) {
    if (!element) return null
    let widget = instances.get(element)
    if (widget) return widget
    const multiple = options.mode === 'multiple' || element.multiple
    const settings = { ...baseSettings(options), ...remote(options), maxItems: multiple ? null : 1 }
    if (multiple) settings.plugins = ['remove_button']
    if (options.onChange) settings.onChange = options.onChange
    widget = new TomSelect(element, settings)
    if (multiple && options.emptyValue != null) applyEmptyValue(widget, String(options.emptyValue))
    instances.set(element, widget)
    element.grandSelect = widget
    if (window.jQuery) window.jQuery.data(element, 'grandSelect', widget)
    return widget
}

/**
 * Enhances a named <input> that holds an Id: the input is hidden and keeps the value, a
 * single select next to it does the choosing. Kendo's DropDownList did the same.
 */
export function createLookup(element, options = {}) {
    if (!element) return null
    let widget = instances.get(element)
    if (widget) return widget
    const doc = element.ownerDocument
    const select = doc.createElement('select')
    //Bootstrap 5 styles a select with form-select, not form-control; Tom Select reads the
    //class off the element it enhances and puts it on the wrapper it builds
    select.className = element.className.replace(/\bform-control\b/g, 'form-select')
    if (!select.classList.contains('form-select')) select.classList.add('form-select')
    select.setAttribute('style', element.getAttribute('style') || '')
    if (element.id) select.id = `${element.id}-select`
    if (element.hasAttribute('disabled')) select.disabled = true

    const empty = doc.createElement('option')
    empty.value = ''
    empty.textContent = options.placeholder || ''
    select.appendChild(empty)

    element.parentNode.insertBefore(select, element.nextSibling)
    element.type = 'hidden'

    widget = createSelect(select, {
        ...options,
        mode: 'single',
        onChange: value => {
            element.value = value == null ? '' : value
            element.dispatchEvent(new Event('change', { bubbles: true }))
        }
    })
    instances.set(element, widget)
    element.grandSelect = widget

    const current = element.value
    //the endpoint returns the current item first when the URL carries its id
    fetchOptions(options.url, '', options).then(rowsLoaded => {
        widget.addOptions(rowsLoaded)
        if (current) {
            if (!widget.options[current]) widget.addOption({ value: current, text: current })
            widget.setValue(current, true)
        }
        widget.refreshOptions(false)
    }).catch(() => { })
    return widget
}

/**
 * The MultiSelect editor template's rule: the "0" entry means "all" and cannot be combined
 * with another value, and an empty selection falls back to it (Kendo select/change handlers).
 */
function applyEmptyValue(widget, emptyValue) {
    let guard = false
    let lastAdded = null
    widget.on('item_add', value => { lastAdded = String(value) })
    widget.on('change', () => {
        if (guard) return
        const values = widget.getValue()
        const selected = Array.isArray(values) ? values.map(String) : [String(values)]
        let next = null
        if (selected.length > 1 && selected.includes(emptyValue)) {
            //picking anything next to "all" drops "all"; picking "all" drops the rest
            next = lastAdded === emptyValue ? [emptyValue] : selected.filter(v => v !== emptyValue)
        } else if (selected.length === 0 || (selected.length === 1 && selected[0] === '')) {
            next = [emptyValue]
        }
        if (!next) return
        guard = true
        widget.setValue(next, true)
        guard = false
    })
}

export function getSelect(element) {
    return element ? instances.get(element) || null : null
}

function parseConfig(element) {
    try {
        return JSON.parse(element.getAttribute('data-grand-select') || '{}')
    } catch (error) {
        console.error('[admin-ui] invalid select options', error)
        return {}
    }
}

/** Upgrades every element carrying data-grand-select under a root. */
export function initSelects(root, texts) {
    const created = []
    const widgetTexts = texts || pageTexts()
    for (const element of collect(root, '[data-grand-select]')) {
        if (instances.has(element)) continue
        const config = { texts: widgetTexts, ...parseConfig(element) }
        if (element.tagName === 'SELECT') {
            if (!element.classList.contains('form-select')) element.classList.add('form-select')
            const widget = createSelect(element, config)
            if (config.url) preselect(widget, config)
            created.push(widget)
        } else {
            created.push(createLookup(element, config))
        }
    }
    return created
}

/** Loads the remote list of a multiselect and restores the values the model carried. */
function preselect(widget, config) {
    const values = Array.isArray(config.values) ? config.values.map(String) : []
    fetchOptions(config.url, '', config).then(options => {
        widget.addOptions(options)
        if (values.length) widget.setValue(values, true)
        widget.refreshOptions(false)
    }).catch(() => { })
}
