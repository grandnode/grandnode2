/*
 * Bootstrap 4 -> Bootstrap 5 rewriting of a Razor view.
 *
 * Only two things in a .cshtml carry Bootstrap 4 names: the value of a class attribute
 * and the Bootstrap data attributes. Both are rewritten here; everything else in the
 * file is left byte for byte as it was, which is what keeps a four-thousand-element
 * diff reviewable.
 *
 * Razor is not parsed. A class attribute value is scanned with the same nesting rules
 * the Razor masking uses (@(...), @{...}, @Foo(...) hold their own quotes), so the end
 * of the attribute is found correctly even when Razor inside it carries a double quote;
 * inside the value only tokens that are plain class names are touched, plus class names
 * that stand alone inside a quoted C# string, which is how a conditional class is
 * written. Anything else - an interpolation, a helper call, a partial - is left alone
 * and reported so a person can look at it.
 */

//One Bootstrap 4 name for one Bootstrap 5 name.
export const CLASS_MAP = {
    //direction-aware utilities
    'float-left': 'float-start',
    'float-right': 'float-end',
    'text-left': 'text-start',
    'text-right': 'text-end',
    'border-left': 'border-start',
    'border-right': 'border-end',
    'border-left-0': 'border-start-0',
    'border-right-0': 'border-end-0',
    'rounded-left': 'rounded-start',
    'rounded-right': 'rounded-end',
    'dropdown-menu-left': 'dropdown-menu-start',
    'dropdown-menu-right': 'dropdown-menu-end',
    //type utilities
    'font-weight-light': 'fw-light',
    'font-weight-lighter': 'fw-lighter',
    'font-weight-normal': 'fw-normal',
    'font-weight-bold': 'fw-bold',
    'font-weight-bolder': 'fw-bolder',
    'font-italic': 'fst-italic',
    'text-monospace': 'font-monospace',
    //forms
    'form-group': 'mb-3',
    'custom-control': 'form-check',
    'custom-control-inline': 'form-check-inline',
    'custom-control-input': 'form-check-input',
    'custom-control-label': 'form-check-label',
    'custom-switch': 'form-switch',
    'custom-select': 'form-select',
    'custom-select-sm': 'form-select-sm',
    'custom-select-lg': 'form-select-lg',
    'custom-range': 'form-range',
    //components
    'badge-pill': 'rounded-pill',
    'close': 'btn-close',
    'sr-only': 'visually-hidden',
    'sr-only-focusable': 'visually-hidden-focusable',
    'no-gutters': 'g-0',
    'media-body': 'flex-grow-1',
    'btn-block': 'w-100'
}

//Directional spacing utilities, every step and every breakpoint.
const SPACERS = ['0', '1', '2', '3', '4', '5', 'auto']
const BREAKPOINTS = ['', 'sm-', 'md-', 'lg-', 'xl-']
for (const bp of BREAKPOINTS) {
    for (const step of SPACERS) {
        CLASS_MAP[`ml-${bp}${step}`] = `ms-${bp}${step}`
        CLASS_MAP[`mr-${bp}${step}`] = `me-${bp}${step}`
        if (step === 'auto') continue
        CLASS_MAP[`pl-${bp}${step}`] = `ps-${bp}${step}`
        CLASS_MAP[`pr-${bp}${step}`] = `pe-${bp}${step}`
        CLASS_MAP[`ml-n${step}`] = `ms-n${step}`
        CLASS_MAP[`mr-n${step}`] = `me-n${step}`
    }
}

//badge-info and friends became a background utility next to .badge.
const BADGE_COLOURS = ['primary', 'secondary', 'success', 'danger', 'warning', 'info', 'light', 'dark']
for (const colour of BADGE_COLOURS) CLASS_MAP[`badge-${colour}`] = `text-bg-${colour}`

//Names Bootstrap 5 has no equivalent for and that styled nothing on their own: the
//wrapper classes of an input group (its children are laid out directly now), the
//Bootstrap 4 checkbox and radio shapes (the control itself is styled now) and the
//inline form (a flex row, written with utilities where a view needs one).
export const CLASS_DROP = new Set([
    'input-group-append',
    'input-group-prepend',
    'custom-checkbox',
    'custom-radio',
    'form-inline'
])

//One name that has to become several.
export const CLASS_EXPAND = {
    //.media was a flex row with an aligned start
    'media': ['d-flex'],
    //.form-row was a .row with a narrower gutter
    'form-row': ['row', 'g-2']
}

const PLAIN_CLASS = /^[A-Za-z][A-Za-z0-9_-]*$/

//form-group only ever supplied a bottom margin, so on an element that already carries a
//margin utility it has to disappear rather than become a second, conflicting one - the
//views write class="form-group mb-0" where they want no margin at all. The same holds for
//a .media that is already a flex row.
const REDUNDANT_WITH = {
    'form-group': /^mb(-(sm|md|lg|xl|xxl))?-(0|1|2|3|4|5|auto)$/,
    'media': /^d-(flex|inline-flex)$/
}

/**
 * Rewrites one whitespace-separated class token. Returns an array of tokens (possibly
 * empty). `siblings` is every other token of the same class attribute.
 */
export function convertClassToken(token, siblings = []) {
    const redundant = REDUNDANT_WITH[token]
    if (redundant && siblings.some(other => redundant.test(other))) return []
    if (CLASS_EXPAND[token]) return CLASS_EXPAND[token]
    if (CLASS_DROP.has(token)) return []
    if (CLASS_MAP[token]) return [CLASS_MAP[token]]
    return [token]
}

/**
 * Rewrites the value of a class attribute. Whitespace between tokens is preserved so a
 * multi-line class attribute keeps its shape; a dropped token takes its leading
 * whitespace with it.
 */
export function convertClassValue(value) {
    const parts = value.split(/(\s+)/)
    const tokens = parts.filter((_, i) => i % 2 === 0).filter(Boolean)
    const out = []
    let changed = false
    for (let i = 0; i < parts.length; i++) {
        const part = parts[i]
        if (i % 2 === 1) { out.push(part); continue } //whitespace
        if (part === '') { out.push(part); continue }
        const converted = convertToken(part, tokens.filter(t => t !== part))
        if (converted === null) { out.push(part); continue }
        changed = true
        if (converted.length === 0) {
            //drop the token and the whitespace that preceded it
            if (out.length && /^\s+$/.test(out[out.length - 1])) out.pop()
            else if (parts[i + 1] !== undefined && /^\s+$/.test(parts[i + 1])) i++
        } else {
            out.push(converted.join(' '))
        }
    }
    const result = out.join('')
    return { value: changed ? result : value, changed }
}

//A token is either a plain class name, or a class name alone inside a C# string
//literal - class="@(Model.Ok ? "text-right" : "")" - which is how the views write a
//conditional class. Anything else is left alone.
function convertToken(token, siblings) {
    if (PLAIN_CLASS.test(token)) {
        const converted = convertClassToken(token, siblings)
        if (converted.length === 1 && converted[0] === token) return null
        return converted
    }
    //a class name inside a C# string literal, wherever it sits in the token:
    //@(Model.Ok ? "text-right" : "text-left") arrives as three tokens, the last of
    //which carries the closing parenthesis
    const rewritten = token.replace(/(["'])([A-Za-z][A-Za-z0-9_ -]*)\1/g, (whole, quote, inner) => {
        const converted = inner.split(' ').flatMap(convertClassToken).join(' ')
        return `${quote}${converted}${quote}`
    })
    return rewritten === token ? null : [rewritten]
}

// ---------------------------------------------------------------------------
// Bootstrap data attributes
// ---------------------------------------------------------------------------
const TOGGLE_VALUES = new Set(['modal', 'collapse', 'dropdown', 'tab', 'pill', 'list', 'tooltip', 'popover', 'button', 'buttons'])
const TRIGGER_ATTRS = ['toggle', 'dismiss', 'ride', 'spy', 'slide', 'slide-to']
const OPTION_ATTRS = [
    'target', 'parent', 'content', 'placement', 'trigger', 'html', 'offset', 'boundary',
    'animation', 'delay', 'container', 'template', 'backdrop', 'keyboard', 'focus',
    'interval', 'pause', 'wrap', 'touch', 'autohide', 'display', 'original-title'
]

/**
 * Renames the Bootstrap 4 data attributes of one start tag. data-target and data-parent
 * are renamed only on a tag that also carries a Bootstrap trigger attribute, because both
 * names are used outside Bootstrap in these views (the admin search box, the grids).
 */
export function convertTagAttributes(tag) {
    let hasTrigger = false
    for (const name of TRIGGER_ATTRS) {
        const match = new RegExp(`\\bdata-${name}="([^"]*)"`).exec(tag)
        if (!match) continue
        if (name === 'toggle' && !TOGGLE_VALUES.has(match[1])) continue
        hasTrigger = true
        tag = tag.replace(new RegExp(`\\bdata-${name}=`), `data-bs-${name}=`)
    }
    if (!hasTrigger) return tag
    for (const name of OPTION_ATTRS) {
        tag = tag.replace(new RegExp(`\\bdata-${name}=`), `data-bs-${name}=`)
    }
    return tag
}

// ---------------------------------------------------------------------------
// Whole file
// ---------------------------------------------------------------------------

/**
 * Finds the end of an attribute value that started at `start` (the character after the
 * opening quote), honouring Razor constructs that may carry the quote character.
 */
function findValueEnd(src, start, quote) {
    let i = start
    while (i < src.length) {
        const c = src[i]
        if (c === quote) return i
        if (c === '\n' && quote === '"' && !src.slice(start, i).includes('@')) return -1
        if (c === '@') {
            const next = src[i + 1]
            if (next === '@') { i += 2; continue }
            if (next === '(' || next === '{') { i = skipBalanced(src, i + 1); continue }
            //@Foo.Bar(...) / @Foo["..."]
            let j = i + 1
            while (j < src.length && /[A-Za-z0-9_.]/.test(src[j])) j++
            while (src[j] === '(' || src[j] === '[') j = skipBalanced(src, j)
            i = j
            continue
        }
        i++
    }
    return -1
}

function skipBalanced(src, open) {
    const pairs = { '(': ')', '{': '}', '[': ']' }
    const close = pairs[src[open]]
    let depth = 0
    for (let i = open; i < src.length; i++) {
        const c = src[i]
        if (c === '"' || c === "'") { i = skipString(src, i); continue }
        if (c === src[open]) depth++
        else if (c === close) { depth--; if (depth === 0) return i + 1 }
    }
    return src.length
}

function skipString(src, i) {
    const quote = src[i]
    for (let j = i + 1; j < src.length; j++) {
        if (src[j] === '\\') { j++; continue }
        if (src[j] === quote) return j
        if (src[j] === '\n') return i
    }
    return src.length
}

/**
 * Rewrites a whole view. Returns the output, how many class tokens and attributes were
 * changed, and the class attribute values that contain Razor a person should look at.
 */
export function convertView(src) {
    const stats = { classes: 0, attributes: 0, dropped: 0 }
    const notes = []

    //1. class attributes
    let out = ''
    let i = 0
    const pattern = /\bclass\s*=\s*(["'])/g
    let match
    while ((match = pattern.exec(src)) !== null) {
        const quote = match[1]
        const valueStart = match.index + match[0].length
        const valueEnd = findValueEnd(src, valueStart, quote)
        if (valueEnd < 0) continue
        const value = src.slice(valueStart, valueEnd)
        const { value: converted, changed } = convertClassValue(value)
        if (changed) {
            const before = value.split(/\s+/).filter(Boolean).length
            const after = converted.split(/\s+/).filter(Boolean).length
            stats.classes += 1
            if (after < before) stats.dropped += before - after
            out += src.slice(i, valueStart) + converted
            i = valueEnd
        }
        if (value.includes('@') && /(?:^|\s)(?:ml|mr|pl|pr|text|float|form|custom|badge|close|sr-only|media|no-gutters)/.test(value)) {
            notes.push(value.trim().replace(/\s+/g, ' '))
        }
        pattern.lastIndex = valueEnd
    }
    out += src.slice(i)

    //2. Bootstrap data attributes, one start tag at a time
    out = out.replace(/<[a-zA-Z][^<>]*>/g, tag => {
        if (!/\bdata-(toggle|dismiss|ride|spy|slide)=/.test(tag)) return tag
        const converted = convertTagAttributes(tag)
        if (converted !== tag) stats.attributes++
        return converted
    })

    return { output: out, stats, notes }
}
