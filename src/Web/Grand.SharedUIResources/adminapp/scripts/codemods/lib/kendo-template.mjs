/*
 * Kendo template and condition conversion for the kendoGrid -> <admin-grid> codemod.
 *
 * Kendo cell templates are strings of HTML with #: expr # (encoded), #= expr # (raw) and
 * # code # parts evaluated as JavaScript. <admin-grid> cell templates are markup that is
 * never evaluated (adminapp/src/grid/template.js):
 *
 *   #: Field #, #= kendo.htmlEncode(Field) #   -> {{ Field }}
 *   #= Field #                                 -> {{ Field }}  (encoded by decision; review
 *                                                 fields that look like server-built HTML)
 *   #= kendo.toString(Field, "n2") #           -> {{ Field | n2 }}
 *   # if (cond) { # a # } else { # b # } #     -> <x data-if="cond">a</x><y data-else>b</y>
 *
 * Conditions are translated into the expression language of adminapp/src/grid/expression.js
 * (paths, literals, ! on a path, comparisons, && binding tighter than ||, no parentheses).
 * Anything else is reported instead of guessed.
 */
import { parseExpressionAt } from 'acorn'

const PLACEHOLDER = /__RZ\d+__/g

/** Field names whose raw Kendo output probably is server-built markup. */
export const HTML_LIKE_FIELD = /(Html|Info|Warnings?|Description|Note|Comment|Text|Body|Message|ValueRaw|Attributes|Content|Rules?String)$/i

/** Splits a Kendo template into html / encoded / raw / code parts. */
export function tokenizeKendoTemplate(template) {
    const parts = []
    let buf = ''
    let inside = false
    for (let i = 0; i < template.length; i++) {
        const c = template[i]
        if (c === '\\' && template[i + 1] === '#') {
            buf += '#'
            i++
            continue
        }
        if (c === '#') {
            if (inside) {
                if (buf.startsWith(':')) parts.push({ type: 'encoded', expr: buf.slice(1).trim() })
                else if (buf.startsWith('=')) parts.push({ type: 'raw', expr: buf.slice(1).trim() })
                else parts.push({ type: 'code', code: buf.trim() })
            } else if (buf) {
                parts.push({ type: 'html', text: buf })
            }
            buf = ''
            inside = !inside
            continue
        }
        buf += c
    }
    if (inside) return { parts, balanced: false }
    if (buf) parts.push({ type: 'html', text: buf })
    return { parts, balanced: true }
}

function parseJs(code) {
    const node = parseExpressionAt(code, 0, { ecmaVersion: 'latest' })
    if (code.slice(node.end).trim() !== '') throw new Error(`unexpected "${code.slice(node.end).trim()}"`)
    return node
}

/** Dotted path of an identifier/member chain, without a leading "data."; null otherwise. */
export function pathOf(node) {
    if (node.type === 'Identifier') return node.name
    if (node.type === 'MemberExpression' && !node.computed && node.property.type === 'Identifier') {
        const object = pathOf(node.object)
        if (!object) return null
        return object === 'data' ? node.property.name : `${object}.${node.property.name}`
    }
    return null
}

function quote(value) {
    return `'${String(value).replace(/\\/g, '\\\\').replace(/'/g, "\\'")}'`
}

/**
 * Converts a JavaScript condition to the admin-grid expression language.
 * `restore` turns Razor placeholders (__RZn__) back into Razor source.
 * @returns {string|null} null when the condition cannot be expressed
 */
export function convertCondition(code, restore = s => s) {
    let node
    try {
        node = parseJs(code)
    } catch {
        return null
    }
    const operand = n => {
        if (n.type === 'Literal') {
            if (typeof n.value === 'string') return quote(restore(n.value))
            if (n.value === null || typeof n.value === 'number' || typeof n.value === 'boolean') return String(n.value)
            return null
        }
        if (n.type === 'Identifier' && /^__RZ\d+__$/.test(n.name)) return restore(n.name)
        if (n.type === 'Identifier' && n.name === 'undefined') return 'null'
        return pathOf(n)
    }
    const term = n => {
        if (n.type === 'UnaryExpression' && n.operator === '!') {
            const inner = n.argument
            if (inner.type === 'UnaryExpression' && inner.operator === '!') return term(inner.argument)
            const path = pathOf(inner)
            return path ? `!${path}` : null
        }
        if (n.type === 'BinaryExpression' && ['==', '===', '!=', '!==', '<', '>', '<=', '>='].includes(n.operator)) {
            const left = operand(n.left)
            const right = operand(n.right)
            if (left == null || right == null) return null
            return `${left} ${n.operator.replace('===', '==').replace('!==', '!=')} ${right}`
        }
        return operand(n)
    }
    const and = n => {
        if (n.type === 'LogicalExpression' && n.operator === '&&') {
            const left = and(n.left)
            const right = and(n.right)
            return left && right ? `${left} && ${right}` : null
        }
        if (n.type === 'LogicalExpression') return null
        return term(n)
    }
    const or = n => {
        if (n.type === 'LogicalExpression' && n.operator === '||') {
            const left = or(n.left)
            const right = or(n.right)
            return left && right ? `${left} || ${right}` : null
        }
        return and(n)
    }
    return or(node)
}

/** Negates a converted condition when the language can express it. */
export function negateCondition(condition) {
    if (/&&|\|\|/.test(condition)) return null
    const comparison = /^(.+?) (==|!=|<=|>=|<|>) (.+)$/.exec(condition)
    if (comparison) {
        const opposite = { '==': '!=', '!=': '==', '<': '>=', '>=': '<', '>': '<=', '<=': '>' }
        return `${comparison[1]} ${opposite[comparison[2]]} ${comparison[3]}`
    }
    return condition.startsWith('!') ? condition.slice(1) : `!${condition}`
}

/**
 * Converts one #: # / #= # expression. Returns { text, raw?, field?, error? }.
 */
export function convertOutput(part, restore = s => s) {
    const source = part.expr
    if (/^__RZ\d+__$/.test(source)) return { text: restore(source) }
    let node
    try {
        node = parseJs(source)
    } catch {
        return { error: `unsupported template expression #${part.type === 'raw' ? '=' : ':'}${source}#` }
    }
    let raw = part.type === 'raw'
    if (node.type === 'CallExpression' && node.callee.type === 'MemberExpression' && pathOf(node.callee) === 'kendo.htmlEncode' && node.arguments.length === 1) {
        node = node.arguments[0]
        raw = false
    }
    if (node.type === 'CallExpression' && pathOf(node.callee) === 'kendo.toString' && node.arguments.length === 2 &&
        node.arguments[1].type === 'Literal' && typeof node.arguments[1].value === 'string') {
        const path = pathOf(node.arguments[0])
        if (!path) return { error: `unsupported template expression #=${source}#` }
        const format = node.arguments[1].value.replace(/^\{0:(.*)\}$/, '$1')
        return { text: `{{ ${path} | ${format} }}`, field: path }
    }
    const path = pathOf(node)
    if (!path) return { error: `unsupported template expression #${part.type === 'raw' ? '=' : ':'}${source}#` }
    return { text: `{{ ${path} }}`, field: path, raw }
}

/** Parses the code parts into a tree of if/else nodes. */
function buildTree(parts, restore, report) {
    const root = { children: [] }
    const stack = [{ node: root, target: root.children }]
    const top = () => stack[stack.length - 1]
    for (const part of parts) {
        if (part.type !== 'code') {
            top().target.push(part)
            continue
        }
        let code = part.code
        //several statements in one # # part, e.g. "} else {" or "} }"
        while (code.length) {
            let m
            if ((m = /^\}\s*else\s+if\s*\((.*)\)\s*\{\s*/s.exec(code))) {
                const frame = top()
                if (!frame.ifNode) return report(`unbalanced template code "${part.code}"`)
                const nested = { type: 'if', condition: m[1], then: [], else: [] }
                frame.ifNode.else.push(nested)
                stack.pop()
                stack.push({ node: nested, ifNode: nested, target: nested.then, chained: (frame.chained || 0) + 1 })
                code = code.slice(m[0].length)
            } else if ((m = /^\}\s*else\s*\{\s*/.exec(code))) {
                const frame = top()
                if (!frame.ifNode) return report(`unbalanced template code "${part.code}"`)
                frame.target = frame.ifNode.else
                code = code.slice(m[0].length)
            } else if ((m = /^if\s*\((.*?)\)\s*\{\s*/s.exec(code)) && balancedParens(m[1])) {
                const ifNode = { type: 'if', condition: m[1], then: [], else: [] }
                top().target.push(ifNode)
                stack.push({ node: ifNode, ifNode, target: ifNode.then })
                code = code.slice(m[0].length)
            } else if ((m = /^\}\s*/.exec(code))) {
                if (stack.length === 1) return report(`unbalanced template code "${part.code}"`)
                stack.pop()
                code = code.slice(m[0].length)
            } else {
                return report(`unsupported template code "# ${part.code} #"`)
            }
        }
    }
    if (stack.length !== 1) return report('unbalanced template code')
    return root
}

function balancedParens(text) {
    let depth = 0
    for (const c of text) {
        if (c === '(') depth++
        if (c === ')' && --depth < 0) return false
    }
    return depth === 0
}

const VOID = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'source', 'track', 'wbr'])

/** True when the markup is exactly one element (with its content). */
export function singleElement(html) {
    const text = html.trim()
    if (!text.startsWith('<') || text.startsWith('</')) return false
    const tag = /<\/?([a-zA-Z][\w-]*)(?:\s[^>]*?)?(\/?)>/g
    let depth = 0
    let m
    let first = true
    while ((m = tag.exec(text))) {
        if (first && m.index !== 0) return false
        first = false
        const name = m[1].toLowerCase()
        const closing = m[0][1] === '/'
        const selfClosing = m[2] === '/' || VOID.has(name)
        if (closing) depth--
        else if (!selfClosing) depth++
        if (depth === 0) return tag.lastIndex === text.length
        if (depth < 0) return false
    }
    return false
}

function addAttribute(html, attribute) {
    const text = html.trim()
    return text.replace(/^<([a-zA-Z][\w-]*)/, `<$1 ${attribute}`)
}

function wrap(html, attribute) {
    const text = html.trim()
    if (singleElement(text)) return addAttribute(text, attribute)
    return `<span ${attribute}>${text}</span>`
}

/**
 * Converts a Kendo template to admin-grid cell template markup.
 * @param {string} template the cooked template string (Razor masked as __RZn__)
 * @param {object} options
 * @param {(s: string) => string} options.restore turns Razor placeholders back into Razor
 * @param {(s: string) => string|null} [options.textFor] maps a Razor placeholder that is a
 *   localized text (@Loc[...]) to a {{ $texts.name }} placeholder
 * @returns {{ lines: string[], markers: string[], ok: boolean, rawFields: string[] }}
 */
export function convertKendoTemplate(template, { restore = s => s, textFor = () => null } = {}) {
    const markers = []
    const rawFields = []
    const { parts, balanced } = tokenizeKendoTemplate(template)
    if (!balanced) return { lines: [], markers: ['unbalanced # in template'], ok: false, rawFields }
    let failure = null
    const report = message => {
        failure = message
        return null
    }
    const tree = buildTree(parts, restore, report)
    if (!tree) return { lines: [], markers: [failure], ok: false, rawFields }

    const restoreHtml = text => text.replace(PLACEHOLDER, placeholder => textFor(placeholder) ?? restore(placeholder))

    const render = nodes => {
        //returns a list of top-level chunks; conditionals are separate chunks
        const chunks = []
        let run = ''
        const flush = () => {
            if (run.trim()) chunks.push(run.trim())
            run = ''
        }
        for (const node of nodes) {
            if (node.type === 'html') {
                run += restoreHtml(node.text)
            } else if (node.type === 'encoded' || node.type === 'raw') {
                const out = convertOutput(node, restore)
                if (out.error) {
                    failure = out.error
                    return []
                }
                if (out.raw && out.field && HTML_LIKE_FIELD.test(out.field)) rawFields.push(out.field)
                run += out.text
            } else if (node.type === 'if') {
                //a condition inside a tag builds attribute values (class="label-# if #...") -
                //data-if works on elements only
                if (run.lastIndexOf('<') > run.lastIndexOf('>')) {
                    failure = `template code inside a tag or attribute "${node.condition}"`
                    return []
                }
                flush()
                const condition = convertCondition(node.condition, restore)
                if (!condition) {
                    failure = `unsupported template condition "${node.condition}"`
                    return []
                }
                const thenHtml = render(node.then).join(' ')
                const elseHtml = render(node.else).join(' ')
                if (failure) return []
                if (thenHtml && elseHtml) {
                    chunks.push(wrap(thenHtml, `data-if="${escapeAttribute(condition)}"`))
                    chunks.push(wrap(elseHtml, 'data-else'))
                } else if (thenHtml) {
                    chunks.push(wrap(thenHtml, `data-if="${escapeAttribute(condition)}"`))
                } else if (elseHtml) {
                    const negated = negateCondition(condition)
                    if (negated) chunks.push(wrap(elseHtml, `data-if="${escapeAttribute(negated)}"`))
                    else {
                        chunks.push(`<span data-if="${escapeAttribute(condition)}"></span>`)
                        chunks.push(wrap(elseHtml, 'data-else'))
                    }
                }
            }
        }
        flush()
        return chunks
    }
    const lines = render(tree.children)
    if (failure) return { lines: [], markers: [failure], ok: false, rawFields }
    for (const field of rawFields) {
        markers.push(`#=${field}# was raw HTML in Kendo and is encoded now; use {{{ ${field} }}} only if the server builds it as markup`)
    }
    return { lines, markers, ok: true, rawFields }
}

/** Attribute value inside double quotes; && stays readable (an ampersand not followed by a name is literal). */
export function escapeAttribute(value) {
    return String(value).replace(/"/g, '&quot;')
}
