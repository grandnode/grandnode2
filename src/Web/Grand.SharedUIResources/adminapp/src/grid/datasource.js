import { param } from './param.js'
import { displayGridError, postForm, responseErrors, withAntiForgeryToken } from './transport.js'

//Server-paged data source with the subset of the Kendo DataSource API the views and
//plugins call from outside: read(), page(n), pageSize(n), total(), data(), view(), get(id),
//at(i), remove(item), sync(), cancelChanges(). Requests and responses follow the unchanged
//server contract: DataSourceRequest {Page, PageSize} in, DataSourceResult
//{Data, Total, Errors, ExtraData} out.

function appendQuery(url, query) {
    if (!query) return url
    return url + (url.includes('?') ? '&' : '?') + query
}

export class DataSource {
    /**
     * @param {object} options
     * @param {{read: string, create?: string, update?: string, destroy?: string}} options.transport
     * @param {string} [options.key] id field
     * @param {number} [options.pageSize] omitted from requests when not set, like Kendo
     * @param {() => object} [options.additionalData] extra fields for read requests
     * @param {(item: object, operation: string) => object} [options.serializeItem]
     * @param {(e: object) => void} [options.onRequestStart]
     * @param {(e: object) => void} [options.onRequestEnd]
     * @param {(e: object) => boolean|void} [options.onError] return false to skip the default alert
     * @param {() => void} [options.onChange]
     * @param {(data: object, operation: string) => object} [options.parameterMap] Kendo shim
     * @param {{data?: string|Function, total?: string|Function, errors?: string|Function}} [options.schema] Kendo shim
     * @param {Function} [options.post] injectable for tests
     */
    constructor(options) {
        this.options = options
        this.transport = options.transport || {}
        this.key = options.key || 'Id'
        this._page = 1
        this._pageSize = options.pageSize || null
        this._data = []
        this._total = 0
        this._extraData = null
        this._destroyed = []
        this._post = options.post || postForm
        this._readSequence = 0
        //serverPaging: false - the server returns every row and pages are cut here (Kendo
        //dataSource without serverPaging)
        this._clientPaging = options.serverPaging === false
        this._all = null
    }

    _slice() {
        const all = this._all || []
        this._total = all.length
        if (this._pageSize) {
            if (this._page > this.totalPages()) this._page = this.totalPages()
            const start = (this._page - 1) * this._pageSize
            this._data = all.slice(start, start + this._pageSize)
        } else {
            this._data = all.slice()
        }
    }

    _requestData(operation, data) {
        const payload = withAntiForgeryToken({ ...data })
        return this.options.parameterMap ? this.options.parameterMap(payload, operation) ?? payload : payload
    }

    _url(operation, payload) {
        const url = this.transport[operation]
        //the Kendo shim allows transport urls computed from the request data
        return typeof url === 'function' ? url(payload) : url
    }

    _schema(member, response, fallback) {
        const selector = this.options.schema?.[member]
        if (typeof selector === 'function') return selector(response)
        const name = selector || fallback
        return response && typeof response === 'object' ? response[name] : undefined
    }

    _errors(response) {
        if (this.options.schema?.errors) return this._schema('errors', response) || null
        return responseErrors(response)
    }

    _error(e) {
        const handled = this.options.onError ? this.options.onError(e) : undefined
        if (handled !== false) displayGridError(e)
    }

    /** Builds the read payload: additional data, antiforgery, take/skip/page/pageSize. */
    readPayload() {
        const additional = this.options.additionalData ? this.options.additionalData() || {} : {}
        const payload = { ...additional }
        if (this._pageSize && !this._clientPaging) {
            payload.take = this._pageSize
            payload.skip = (this._page - 1) * this._pageSize
            payload.page = this._page
            payload.pageSize = this._pageSize
        }
        return this._requestData('read', payload)
    }

    /** Reloads the current page. Resolves when the data is applied (or the read failed). */
    async read() {
        if (!this.transport.read) {
            //local rows rendered into the page by the server (<admin-grid data="...">)
            if (Array.isArray(this.options.data)) {
                this._data = this.options.data.slice()
                this._total = this._data.length
                this._destroyed = []
                this.options.onChange?.()
            }
            return
        }
        const sequence = ++this._readSequence
        const payload = this.readPayload()
        this.options.onRequestStart?.({ type: 'read', data: payload })
        let response
        try {
            response = await this._post(this._url('read', payload), payload)
        } catch (failure) {
            this.options.onRequestEnd?.({ type: 'read', response: undefined })
            this._error({ ...failure, type: 'read' })
            return
        }
        //a newer read started meanwhile (fast paging, search clicked twice): drop this one
        if (sequence !== this._readSequence) return
        const errors = this._errors(response)
        this.options.onRequestEnd?.({ type: 'read', response })
        if (errors) {
            this._error({ errors, type: 'read' })
            return
        }
        const rows = this._schema('data', response, 'Data')
        const total = this._schema('total', response, 'Total')
        this._data = Array.isArray(rows) ? rows : []
        this._total = typeof total === 'number' ? total : this._data.length
        this._extraData = response && typeof response === 'object' ? response.ExtraData ?? null : null
        this._destroyed = []
        if (this._clientPaging) {
            this._all = this._data
            this._slice()
            this.options.onChange?.()
            return
        }
        //deleting the last row of the last page leaves an empty page behind
        if (this._data.length === 0 && this._page > 1 && this._pageSize && this._total > 0) {
            this._page = this.totalPages()
            return this.read()
        }
        this.options.onChange?.()
    }

    /** Getter without an argument; with one, moves to that page and reads it. */
    page(value) {
        if (value === undefined) return this._page
        this._page = Math.max(1, Number(value) || 1)
        if (this._clientPaging && this._all) {
            this._slice()
            this.options.onChange?.()
            return Promise.resolve()
        }
        return this.read()
    }

    pageSize(value) {
        if (value === undefined) return this._pageSize
        this._pageSize = Number(value) || null
        this._page = 1
        if (this._clientPaging && this._all) {
            this._slice()
            this.options.onChange?.()
            return Promise.resolve()
        }
        return this.read()
    }

    totalPages() {
        if (!this._pageSize) return 1
        return Math.max(1, Math.ceil(this._total / this._pageSize))
    }

    total() {
        return this._total
    }

    data() {
        return this._data
    }

    view() {
        return this._data
    }

    extraData() {
        return this._extraData
    }

    at(index) {
        return this._data[index]
    }

    get(id) {
        return this._data.find(item => item != null && String(item[this.key]) === String(id))
    }

    indexOf(item) {
        return this._data.indexOf(item)
    }

    /** Removes an item locally; sync() sends the destroy requests. */
    remove(item) {
        const index = this._data.indexOf(item)
        if (index < 0) return
        this._data.splice(index, 1)
        this._total = Math.max(0, this._total - 1)
        if (this._all) this._all.splice(this._all.indexOf(item), 1)
        this._destroyed.push({ item, index })
        this.options.onChange?.()
    }

    /** Restores removed items, the way the views call it from their error handlers. */
    cancelChanges() {
        if (this._destroyed.length === 0) return
        this._destroyed.slice().reverse().forEach(({ item, index }) => {
            this._data.splice(Math.min(index, this._data.length), 0, item)
            this._total++
        })
        this._destroyed = []
        this.options.onChange?.()
    }

    hasChanges() {
        return this._destroyed.length > 0
    }

    /** Sends pending destroy requests. */
    async sync() {
        const pending = this._destroyed
        this._destroyed = []
        for (const entry of pending) {
            const ok = await this.save('destroy', entry.item)
            if (!ok) {
                this._destroyed = pending.slice(pending.indexOf(entry))
                this.cancelChanges()
                return false
            }
        }
        return true
    }

    /**
     * Posts one create/update/destroy request with the item serialized like the Kendo
     * transports (all fields of the row, jQuery.param encoding).
     * @returns {Promise<boolean>} false when the server answered with Errors or failed
     */
    async save(operation, item) {
        if (!this.transport[operation]) return false
        const fields = this.options.serializeItem ? this.options.serializeItem(item, operation) : { ...item }
        return this.saveFields(operation, fields)
    }

    /**
     * Posts already serialized fields, e.g. several rows flattened as products[0].Name for
     * a batch grid (the Kendo batch parameterMap pattern).
     * @returns {Promise<boolean>}
     */
    async saveFields(operation, fields) {
        if (!this.transport[operation]) return false
        const payload = this._requestData(operation, fields)
        const url = this._url(operation, payload)
        this.options.onRequestStart?.({ type: operation, data: payload })
        let response
        try {
            response = await this._post(url, payload)
        } catch (failure) {
            this.options.onRequestEnd?.({ type: operation, response: undefined })
            this._error({ ...failure, type: operation })
            return false
        }
        const errors = this._errors(response)
        this.options.onRequestEnd?.({ type: operation, response })
        if (errors) {
            this._error({ errors, type: operation })
            return false
        }
        return true
    }

    /** The detail grid pattern: read URL with a query parameter taken from the master row. */
    static urlWithParams(url, params) {
        return appendQuery(url, param(params))
    }
}
