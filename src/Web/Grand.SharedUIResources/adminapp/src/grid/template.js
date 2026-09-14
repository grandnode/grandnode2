import { compileCondition, readPath } from './expression.js'
import { formatValue } from './format.js'

//Cell templates for <admin-grid>. The markup lives in an inert <template> element emitted
//by <cell-template>; it is parsed by the browser once, and each render clones it and
//fills placeholders through the DOM (text nodes and setAttribute), never through string
//concatenation, eval or new Function. Syntax:
//
//  {{ Field }}              text, HTML-encoded by construction (a text node)
//  {{ Field | n2 }}         formatted with a column format (see format.js)
//  {{{ Field }}}            raw HTML - opt-in, only for fields that carry server-built HTML
//  {{ $texts.Name }}        a localized text passed through <grid-text>
//  data-if="expr"           keeps the element only when the condition holds (expression.js)
//  data-else                on the next element sibling of a data-if element
//  data-grid-click="fn"     click calls window.fn(dataItem, event, grid)
//
//Placeholders inside URL attributes (href, src, action, formaction...) are checked for
//script schemes; placeholders inside on* event attributes, srcdoc and style are removed.
//Values inserted from data are never scanned for placeholders again.

const placeholderPattern = /\{\{\{\s*([^{}]+?)\s*\}\}\}|\{\{\s*([^{}]+?)\s*\}\}/g
const urlAttributes = new Set(['href', 'src', 'action', 'formaction', 'poster', 'cite', 'background', 'xlink:href', 'data'])
const blockedSchemes = /^(?:javascript|vbscript|data|file):/i
const imageDataUrl = /^data:image\/(?:png|gif|jpe?g|webp|bmp);base64,[a-z0-9+/=\s]*$/i

/** Returns the URL, or null when it could run script (the attribute is then dropped). */
export function sanitizeUrl(value, attributeName) {
    const url = String(value ?? '')
    //browsers ignore control characters and whitespace inside the scheme
    // eslint-disable-next-line no-control-regex
    const normalized = url.replace(/[\u0000-\u0020\u007f-\u009f]/g, '')
    if (!blockedSchemes.test(normalized)) return url
    if (attributeName === 'src' && imageDataUrl.test(url.trim())) return url
    return null
}

function parsePlaceholder(expression) {
    const [path, ...formatParts] = expression.split('|')
    return { path: path.trim(), format: formatParts.join('|').trim() || null }
}

function resolve(placeholder, item, scope) {
    const value = readPath(item, placeholder.path, scope)
    if (value == null) return ''
    if (placeholder.format) return formatValue(value, placeholder.format, scope?.culture)
    return typeof value === 'object' ? '' : String(value)
}

function fillText(textNode, item, scope) {
    const source = textNode.nodeValue
    if (!source.includes('{{')) return
    const doc = textNode.ownerDocument
    const fragment = doc.createDocumentFragment()
    let last = 0
    placeholderPattern.lastIndex = 0
    let match
    while ((match = placeholderPattern.exec(source)) !== null) {
        if (match.index > last) fragment.appendChild(doc.createTextNode(source.slice(last, match.index)))
        if (match[1] !== undefined) {
            const html = resolve(parsePlaceholder(match[1]), item, scope)
            const holder = doc.createElement('template')
            holder.innerHTML = html
            fragment.appendChild(holder.content)
        } else {
            fragment.appendChild(doc.createTextNode(resolve(parsePlaceholder(match[2]), item, scope)))
        }
        last = placeholderPattern.lastIndex
    }
    if (last < source.length) fragment.appendChild(doc.createTextNode(source.slice(last)))
    textNode.parentNode.replaceChild(fragment, textNode)
}

function fillAttributes(element, item, scope) {
    for (const attribute of Array.from(element.attributes)) {
        const name = attribute.name.toLowerCase()
        if (!attribute.value.includes('{{')) continue
        if (name.startsWith('on') || name === 'srcdoc' || name === 'style') {
            element.removeAttribute(attribute.name)
            console.warn(`[admin-grid] placeholders are not allowed in the "${attribute.name}" attribute; it was removed`)
            continue
        }
        placeholderPattern.lastIndex = 0
        let value = attribute.value.replace(placeholderPattern, (_, raw, encoded) =>
            resolve(parsePlaceholder(raw ?? encoded), item, scope))
        if (urlAttributes.has(name)) value = sanitizeUrl(value, name)
        if (value === null) element.removeAttribute(attribute.name)
        else element.setAttribute(attribute.name, value)
    }
}

const conditionCache = new Map()

function condition(source) {
    let compiled = conditionCache.get(source)
    if (!compiled) {
        compiled = compileCondition(source)
        conditionCache.set(source, compiled)
    }
    return compiled
}

function walk(node, item, scope) {
    let child = node.firstChild
    while (child) {
        const next = child.nextSibling
        if (child.nodeType === 1) {
            const element = child
            if (element.hasAttribute('data-if')) {
                const keep = condition(element.getAttribute('data-if'))(item, scope)
                element.removeAttribute('data-if')
                const sibling = element.nextElementSibling
                const elseElement = sibling && sibling.hasAttribute('data-else') ? sibling : null
                if (elseElement) {
                    //the else branch is processed on its own turn, as plain content
                    elseElement.removeAttribute('data-else')
                    if (keep) elseElement.remove()
                }
                if (!keep) {
                    child = element.nextSibling
                    element.remove()
                    continue
                }
            }
            if (element.hasAttribute('data-else')) {
                //an orphan data-else (no data-if before it) is kept as plain content
                element.removeAttribute('data-else')
            }
            fillAttributes(element, item, scope)
            walk(element.tagName === 'TEMPLATE' ? element.content : element, item, scope)
            child = element.nextSibling
            continue
        }
        if (child.nodeType === 3) fillText(child, item, scope)
        child = next
    }
}

/**
 * Wraps a <template> element (or its HTML) as a renderer.
 * @param {HTMLTemplateElement|string} template
 */
export function compileTemplate(template, doc = globalThis.document) {
    let element = template
    if (typeof template === 'string') {
        element = doc.createElement('template')
        element.innerHTML = template
    }
    return {
        /**
         * @param {object} item data item of the row
         * @param {{texts?: object, culture?: object}} scope
         * @returns {DocumentFragment}
         */
        render(item, scope) {
            const fragment = element.content.cloneNode(true)
            walk(fragment, item, scope)
            return fragment
        }
    }
}
