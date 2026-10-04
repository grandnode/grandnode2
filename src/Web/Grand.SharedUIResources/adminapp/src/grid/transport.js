import { param } from './param.js'

//HTTP side of the grid: the same POST application/x-www-form-urlencoded requests the Kendo
//transports send, the antiforgery token from the same hidden input addAntiForgeryToken
//reads, the page loader admin.common.js shows for jQuery requests, and the same error UX
//as display_kendoui_grid_error.

let pending = 0

function startLoading() {
    pending++
    //admin.common.js hooks the loader to $(document).ajaxStart, which fetch never triggers
    if (pending === 1 && typeof window.StartPageLoading === 'function') window.StartPageLoading()
}

function stopLoading() {
    pending = Math.max(0, pending - 1)
    if (pending === 0 && typeof window.StopPageLoading === 'function') window.StopPageLoading()
}

/** Reads the antiforgery token the way addAntiForgeryToken does. */
export function antiForgeryToken(doc = document) {
    const input = doc.querySelector('input[name=__RequestVerificationToken]')
    return input ? input.value : null
}

/** Adds the antiforgery token to a data object, like addAntiForgeryToken(data). */
export function withAntiForgeryToken(data, doc = document) {
    const result = data || {}
    const token = antiForgeryToken(doc)
    if (token) result.__RequestVerificationToken = token
    return result
}

/**
 * Posts form data and parses the JSON answer.
 * Resolves with the parsed body; rejects with { errorThrown, status } on HTTP or parse
 * failures, like a failed $.ajax call.
 */
export async function postForm(url, data, { fetchImpl = globalThis.fetch, loader = true } = {}) {
    if (loader) startLoading()
    try {
        let response
        try {
            response = await fetchImpl(url, {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                    'Accept': 'application/json, text/javascript, */*; q=0.01',
                    //jQuery sends it; Request.IsAjaxRequest() style checks rely on it
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: param(data)
            })
        } catch (error) {
            throw { errorThrown: error?.message || 'error', status: 0 }
        }
        if (!response.ok) throw { errorThrown: response.statusText || 'error', status: response.status }
        const text = await response.text()
        if (text.trim() === '') return ''
        try {
            return JSON.parse(text)
        } catch {
            throw { errorThrown: 'parsererror', status: response.status }
        }
    } finally {
        if (loader) stopLoading()
    }
}

/** Loads JSON with GET, the default transport of the Kendo DropDownList data sources. */
export async function getJson(url, { fetchImpl = globalThis.fetch } = {}) {
    const response = await fetchImpl(url, {
        credentials: 'same-origin',
        headers: { 'Accept': 'application/json', 'X-Requested-With': 'XMLHttpRequest' }
    })
    if (!response.ok) throw { errorThrown: response.statusText || 'error', status: response.status }
    return response.json()
}

/** Returns the Errors member of a DataSourceResult-like answer, or null. */
export function responseErrors(response) {
    if (response && typeof response === 'object' && response.Errors) return response.Errors
    return null
}

/**
 * Shows grid errors. Uses display_kendoui_grid_error from admin.common.js when it is
 * loaded so the UX stays identical; otherwise the same logic inline.
 * @param {{errors?: any, errorThrown?: string}} e
 */
export function displayGridError(e) {
    if (typeof window.display_kendoui_grid_error === 'function') {
        window.display_kendoui_grid_error(e)
        return
    }
    if (e.errors) {
        if (typeof e.errors === 'string') {
            window.alert(e.errors)
        } else {
            let message = 'The following errors have occurred:'
            Object.values(e.errors).forEach(value => {
                if (value && value.errors) message += '\n' + value.errors.join('\n')
            })
            window.alert(message)
        }
    } else if (e.errorThrown) {
        window.alert('Error happened')
    }
}
