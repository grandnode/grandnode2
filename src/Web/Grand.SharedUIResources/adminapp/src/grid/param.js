//Form serialization identical to jQuery.param(obj) (non-traditional), which is what the
//Kendo transports post today and what the MVC model binders expect: nested objects in
//bracket notation (a[b]=1), arrays of scalars as a[]=1, arrays of objects as a[0][b]=1,
//null/undefined as an empty value, functions invoked, spaces as '+'.

const bracketSuffix = /\[\]$/

function isObjectLike(value) {
    //jQuery.type(value) === "object": plain objects and class instances, but not
    //Date, RegExp, arrays or boxed primitives
    return Object.prototype.toString.call(value) === '[object Object]'
}

function buildParams(prefix, value, add) {
    if (Array.isArray(value)) {
        value.forEach((item, index) => {
            if (bracketSuffix.test(prefix)) {
                add(prefix, item)
            } else {
                buildParams(prefix + '[' + (typeof item === 'object' && item != null ? index : '') + ']', item, add)
            }
        })
    } else if (isObjectLike(value)) {
        for (const name in value) {
            buildParams(prefix + '[' + name + ']', value[name], add)
        }
    } else {
        add(prefix, value)
    }
}

/**
 * Serializes an object to application/x-www-form-urlencoded exactly like jQuery.param.
 * @param {object|Array<{name: string, value: any}>} data
 * @returns {string}
 */
export function param(data) {
    const parts = []
    const add = (key, value) => {
        const resolved = typeof value === 'function' ? value() : (value == null ? '' : value)
        parts.push(encodeURIComponent(key) + '=' + encodeURIComponent(resolved))
    }
    if (data == null) return ''
    if (Array.isArray(data)) {
        data.forEach(field => add(field.name, field.value))
    } else {
        for (const prefix in data) {
            buildParams(prefix, data[prefix], add)
        }
    }
    return parts.join('&').replace(/%20/g, '+')
}

/**
 * Collects the successful controls inside a container, like jQuery's serializeArray:
 * named, enabled, not a button or file input, checkboxes and radios only when checked.
 * @param {Element} container
 * @returns {Array<{name: string, value: string}>}
 */
export function serializeFields(container) {
    const fields = []
    if (!container) return fields
    const controls = container.querySelectorAll('input, select, textarea')
    controls.forEach(control => {
        const type = (control.type || '').toLowerCase()
        if (!control.name || control.disabled) return
        if (['submit', 'button', 'image', 'reset', 'file'].includes(type)) return
        if ((type === 'checkbox' || type === 'radio') && !control.checked) return
        if (control.tagName === 'SELECT' && control.multiple) {
            Array.from(control.selectedOptions).forEach(option => fields.push({ name: control.name, value: option.value }))
            return
        }
        fields.push({ name: control.name, value: String(control.value).replace(/\r?\n/g, '\r\n') })
    })
    return fields
}
