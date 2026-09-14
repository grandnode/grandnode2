//Kendo template compiler for the $.fn.kendoGrid shim only. It reproduces kendo.template:
//
//  #= expr #   raw output
//  #: expr #   HTML-encoded output
//  # code #    JavaScript statements (if/else/for)
//  \#          a literal hash
//
//Like Kendo it compiles to a function (with a `with (data)` block), because plugin
//templates contain arbitrary JavaScript. <admin-grid> cell templates never go through
//here; they use the DOM-based, eval-free src/grid/template.js.

export function htmlEncode(value) {
    return ('' + value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
}

function escapeLiteral(text) {
    return text
        .replace(/\\/g, '\\\\')
        .replace(/'/g, '\\\'')
        .replace(/\n/g, '\\n')
        .replace(/\r/g, '\\r')
        .replace(/\t/g, '\\t')
        //line and paragraph separators end a string literal in older engines
        .replace(new RegExp(String.fromCharCode(0x2028), 'g'), '\\u2028')
        .replace(new RegExp(String.fromCharCode(0x2029), 'g'), '\\u2029')
}

/** Returns the generated function body (exported for tests). */
export function kendoTemplateSource(template, { useWithBlock = true, paramName = 'data' } = {}) {
    const SHARP = '__GRAND_SHARP__'
    const parts = String(template)
        .replace(/\\#/g, SHARP)
        .replace(/#:(.*?)#/gs, '#=$kendoHtmlEncode($1)#')
        .split('#')
    let body = 'var $kendoOutput=\'\';'
    if (useWithBlock) body += `with(${paramName}){`
    parts.forEach((part, index) => {
        if (index % 2 === 0) {
            if (part) body += `$kendoOutput+='${escapeLiteral(part).split(SHARP).join('#')}';`
        } else if (part.startsWith('=')) {
            body += `$kendoOutput+=(${part.slice(1).split(SHARP).join('#')});`
        } else {
            body += part.split(SHARP).join('#') + ';'
        }
    })
    if (useWithBlock) body += '}'
    body += 'return $kendoOutput;'
    return body
}

const cache = new Map()

/**
 * Compiles a Kendo template string; functions are returned unchanged.
 * @returns {(data: object) => string}
 */
export function compileKendoTemplate(template, options) {
    if (typeof template === 'function') return template
    const key = String(template) + '|' + JSON.stringify(options || {})
    if (!cache.has(key)) {
        const paramName = options?.paramName || 'data'
        const fn = new Function(paramName, '$kendoHtmlEncode', kendoTemplateSource(template, options))
        cache.set(key, data => fn(data, htmlEncode))
    }
    return cache.get(key)
}
