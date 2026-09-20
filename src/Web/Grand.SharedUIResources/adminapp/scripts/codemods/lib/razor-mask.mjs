/*
 * Razor masking for the Kendo grid codemod.
 *
 * Admin views build their kendoGrid({ ... }) configuration with Razor woven into the
 * JavaScript: @Loc["..."] inside string literals, @(settings.PageSize) as values,
 * @if (...) { <text>{ column },</text> } inside the columns array and
 * @await Html.PartialAsync(...) injecting whole columns. None of that is JavaScript,
 * so before a JS parser can look at the object every Razor construct inside a
 * <script> element is replaced:
 *
 * - expressions (@Loc[..], @Url.Action(..), @(..), @await ..)  -> identifier __RZn__,
 *   valid both inside a string literal and in value position
 * - partial/component invocations                            -> comment /*__RZPn__*\/,
 *   so the surrounding JS still parses when the partial adds its own commas
 * - @if/@foreach/... blocks                                  -> the markup of the first
 *   branch (<text>, @: lines, HTML elements), itself masked recursively
 * - @{ } code blocks                                          -> their markup, usually none
 * - @* comments *@                                            -> removed
 * - @@                                                         -> @
 *
 * Every construct is recorded with its position in the masked text, and the masked
 * text keeps a map back to original offsets so findings report real line numbers.
 */

const CONTROL_KEYWORDS = new Set(['if', 'foreach', 'for', 'while', 'switch', 'using', 'lock', 'try', 'do'])
const LINE_DIRECTIVES = new Set(['model', 'inject', 'addTagHelper', 'removeTagHelper', 'layout', 'page', 'namespace', 'inherits', 'implements'])
const VOID_ELEMENTS = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'source', 'track', 'wbr'])
const PARTIAL_PATTERN = /Html\s*\.\s*(?:Render)?Partial(?:Async)?\s*\(|Component\s*\.\s*InvokeAsync\s*\(/

const isIdentStart = c => c !== undefined && /[A-Za-z_]/.test(c)
const isIdentPart = c => c !== undefined && /[A-Za-z0-9_]/.test(c)
const isSpace = c => c === ' ' || c === '\t' || c === '\r' || c === '\n'

function skipSpaces(src, i) {
    while (i < src.length && isSpace(src[i])) i++
    return i
}

function readIdent(src, i) {
    let j = i
    while (j < src.length && isIdentPart(src[j])) j++
    return src.slice(i, j)
}

/**
 * Skips a C# string or char literal starting at i. Handles "..", @"..", $"..",
 * $@"..", @$".." and '.'. Returns the index just after the literal.
 */
export function skipCSharpString(src, i) {
    let j = i
    let verbatim = false
    let interpolated = false
    while (src[j] === '@' || src[j] === '$') {
        if (src[j] === '@') verbatim = true
        else interpolated = true
        j++
    }
    const quote = src[j]
    if (quote === "'") {
        //a char literal is at most a short escape; anything longer is not one
        const limit = Math.min(src.length, j + 10)
        for (let k = j + 1; k < limit; k++) {
            if (src[k] === '\\') { k++; continue }
            if (src[k] === '\n') break
            if (src[k] === "'") return k + 1
        }
        return j + 1
    }
    j++
    while (j < src.length) {
        const c = src[j]
        if (!verbatim && c === '\\') { j += 2; continue }
        if (c === '"') {
            if (verbatim && src[j + 1] === '"') { j += 2; continue }
            return j + 1
        }
        if (interpolated && c === '{') {
            if (src[j + 1] === '{') { j += 2; continue }
            const end = scanBalanced(src, j)
            if (end < 0) return src.length
            j = end
            continue
        }
        if (!verbatim && c === '\n') return j
        j++
    }
    return j
}

function isStringStart(src, i) {
    const c = src[i]
    if (c === '"' || c === "'") return true
    if (c === '@' || c === '$') {
        let j = i
        while (src[j] === '@' || src[j] === '$') j++
        return src[j] === '"' && j - i <= 2
    }
    return false
}

/**
 * Given src[i] is ( [ or {, returns the index just after its matching close,
 * skipping C# strings and comments. Returns -1 when unbalanced.
 */
export function scanBalanced(src, i) {
    const pairs = { '(': ')', '[': ']', '{': '}' }
    const stack = [pairs[src[i]]]
    let j = i + 1
    while (j < src.length) {
        const c = src[j]
        if (isStringStart(src, j)) { j = skipCSharpString(src, j); continue }
        if (c === '/' && src[j + 1] === '/') { const nl = src.indexOf('\n', j); j = nl < 0 ? src.length : nl; continue }
        if (c === '/' && src[j + 1] === '*') { const end = src.indexOf('*/', j + 2); j = end < 0 ? src.length : end + 2; continue }
        if (pairs[c]) { stack.push(pairs[c]); j++; continue }
        if (c === ')' || c === ']' || c === '}') {
            if (stack[stack.length - 1] !== c) return -1
            stack.pop()
            j++
            if (stack.length === 0) return j
            continue
        }
        j++
    }
    return -1
}

/** Finds the end of the HTML element whose start tag begins at i (src[i] === '<'). */
function scanElement(src, i) {
    const name = readIdent(src, i + 1).toLowerCase()
    const startTagEnd = findTagEnd(src, i)
    if (startTagEnd < 0) return -1
    if (src[startTagEnd - 2] === '/' || VOID_ELEMENTS.has(name)) return startTagEnd
    const open = new RegExp(`<${name}\\b|</${name}\\s*>`, 'gi')
    open.lastIndex = startTagEnd
    let depth = 1
    let m
    while ((m = open.exec(src))) {
        if (m[0][1] === '/') {
            depth--
            if (depth === 0) return m.index + m[0].length
        } else {
            const end = findTagEnd(src, m.index)
            if (end > 0 && src[end - 2] !== '/') depth++
        }
    }
    return -1
}

function findTagEnd(src, i) {
    let j = i + 1
    while (j < src.length) {
        const c = src[j]
        if (c === '"' || c === "'") {
            const end = src.indexOf(c, j + 1)
            if (end < 0) return -1
            j = end + 1
            continue
        }
        if (c === '>') return j + 1
        j++
    }
    return -1
}

/**
 * Scans a C# code block whose opening brace is at i. Returns the index after the
 * closing brace plus the markup ranges found inside it (<text>, @: lines, elements).
 */
export function scanCodeBlock(src, i) {
    const markups = []
    let depth = 1
    let j = i + 1
    let lastSignificant = '{'
    while (j < src.length) {
        const c = src[j]
        if (c === '@' && src[j + 1] === '*') {
            const end = src.indexOf('*@', j + 2)
            j = end < 0 ? src.length : end + 2
            continue
        }
        if (c === '@' && src[j + 1] === ':') {
            const nl = src.indexOf('\n', j)
            const end = nl < 0 ? src.length : nl
            markups.push({ start: j + 2, end, kind: 'line' })
            j = end
            lastSignificant = ';'
            continue
        }
        if (isStringStart(src, j)) { j = skipCSharpString(src, j); lastSignificant = '"'; continue }
        if (c === '/' && src[j + 1] === '/') { const nl = src.indexOf('\n', j); j = nl < 0 ? src.length : nl; continue }
        if (c === '/' && src[j + 1] === '*') { const end = src.indexOf('*/', j + 2); j = end < 0 ? src.length : end + 2; continue }
        if (c === '<' && /^<text\s*>/i.test(src.slice(j, j + 7))) {
            const contentStart = src.indexOf('>', j) + 1
            const end = scanElement(src, j)
            if (end < 0) return { end: -1, markups }
            const closeStart = src.lastIndexOf('</', end)
            markups.push({ start: contentStart, end: closeStart, kind: 'text' })
            j = end
            lastSignificant = '}'
            continue
        }
        if (c === '<' && isIdentStart(src[j + 1]) && '{};)'.includes(lastSignificant)) {
            const end = scanElement(src, j)
            if (end < 0) return { end: -1, markups }
            markups.push({ start: j, end, kind: 'element' })
            j = end
            lastSignificant = '}'
            continue
        }
        if (c === '{') depth++
        if (c === '}') {
            depth--
            if (depth === 0) return { end: j + 1, markups }
        }
        if (!isSpace(c)) lastSignificant = c
        j++
    }
    return { end: -1, markups }
}

function scanImplicitExpression(src, i) {
    let j = i
    while (j < src.length && isIdentPart(src[j])) j++
    for (;;) {
        const c = src[j]
        if (c === '(' || c === '[') {
            const end = scanBalanced(src, j)
            if (end < 0) return j
            j = end
            continue
        }
        if (c === '.' && isIdentStart(src[j + 1])) {
            j++
            while (j < src.length && isIdentPart(src[j])) j++
            continue
        }
        return j
    }
}

/** Parses @if/@foreach/... starting after the keyword; returns branches or null. */
function scanControl(src, afterKeyword, keyword) {
    const branches = []
    let j = skipSpaces(src, afterKeyword)
    if (keyword !== 'try' && keyword !== 'do') {
        if (src[j] !== '(') return null
        j = scanBalanced(src, j)
        if (j < 0) return null
        j = skipSpaces(src, j)
    }
    if (src[j] !== '{') return null
    let block = scanCodeBlock(src, j)
    if (block.end < 0) return null
    branches.push(block)
    j = block.end

    for (;;) {
        const k = skipSpaces(src, j)
        const word = readIdent(src, k)
        let n = k + word.length
        if (keyword === 'if' && word === 'else') {
            n = skipSpaces(src, n)
            if (readIdent(src, n) === 'if') {
                n = skipSpaces(src, n + 2)
                if (src[n] !== '(') break
                n = scanBalanced(src, n)
                if (n < 0) break
                n = skipSpaces(src, n)
            }
        } else if (keyword === 'try' && (word === 'catch' || word === 'finally')) {
            n = skipSpaces(src, n)
            if (src[n] === '(') {
                n = scanBalanced(src, n)
                if (n < 0) break
                n = skipSpaces(src, n)
            }
        } else if (keyword === 'do' && word === 'while') {
            n = skipSpaces(src, n)
            if (src[n] !== '(') break
            n = scanBalanced(src, n)
            if (n < 0) break
            if (src[n] === ';') n++
            return { branches, end: n }
        } else {
            break
        }
        if (src[n] !== '{') break
        block = scanCodeBlock(src, n)
        if (block.end < 0) break
        branches.push(block)
        j = block.end
    }
    return { branches, end: j }
}

class MaskedOutput {
    constructor() {
        this.parts = []
        this.chunks = []
        this.length = 0
    }

    verbatim(src, start, end) {
        if (end <= start) return
        this.chunks.push({ outStart: this.length, origStart: start, length: end - start, verbatim: true })
        this.parts.push(src.slice(start, end))
        this.length += end - start
    }

    synthetic(text, origPos) {
        if (!text) return
        this.chunks.push({ outStart: this.length, origStart: origPos, length: text.length, verbatim: false })
        this.parts.push(text)
        this.length += text.length
    }

    toOriginal(offset) {
        let lo = 0
        let hi = this.chunks.length - 1
        while (lo <= hi) {
            const mid = (lo + hi) >> 1
            const ch = this.chunks[mid]
            if (offset < ch.outStart) hi = mid - 1
            else if (offset >= ch.outStart + ch.length) lo = mid + 1
            else return ch.verbatim ? ch.origStart + (offset - ch.outStart) : ch.origStart
        }
        const last = this.chunks[this.chunks.length - 1]
        return last ? last.origStart + last.length : offset
    }
}

/**
 * Masks Razor inside src[start, end) treated as markup (the content of a <script>
 * element, of <text>, of an HTML element in a code block).
 */
function maskMarkup(src, start, end, out, state) {
    let j = start
    let copyFrom = start
    const flush = upTo => { out.verbatim(src, copyFrom, upTo) }

    while (j < end) {
        const at = src.indexOf('@', j)
        if (at < 0 || at >= end) break
        const next = src[at + 1]
        const prev = src[at - 1]

        //e-mail addresses and the like: Razor does not transition after an identifier character
        if (isIdentPart(prev) && isIdentPart(next)) { j = at + 1; continue }

        if (next === '@') {
            flush(at)
            out.synthetic('@', at)
            j = copyFrom = at + 2
            continue
        }
        if (next === '*') {
            flush(at)
            const close = src.indexOf('*@', at + 2)
            const stop = close < 0 ? end : Math.min(end, close + 2)
            state.record('comment', at, stop, out.length, out.length)
            j = copyFrom = stop
            continue
        }
        if (next === '(') {
            const stop = scanBalanced(src, at + 1)
            if (stop < 0 || stop > end) { j = at + 1; continue }
            flush(at)
            emitExpression(src, at, stop, out, state)
            j = copyFrom = stop
            continue
        }
        if (next === '{') {
            const block = scanCodeBlock(src, at + 1)
            if (block.end < 0 || block.end > end) { j = at + 1; continue }
            flush(at)
            const outStart = out.length
            const code = src.slice(at, block.end)
            for (const m of block.markups) maskMarkup(src, m.start, m.end, out, state)
            state.record(PARTIAL_PATTERN.test(code) ? 'partial' : 'code', at, block.end, outStart, out.length)
            j = copyFrom = block.end
            continue
        }
        if (isIdentStart(next)) {
            const word = readIdent(src, at + 1)
            if (CONTROL_KEYWORDS.has(word)) {
                const control = scanControl(src, at + 1 + word.length, word)
                if (control && control.end <= end) {
                    flush(at)
                    const outStart = out.length
                    const first = control.branches[0]
                    const markups = word === 'switch' ? first.markups.slice(0, 1) : first.markups
                    for (const m of markups) maskMarkup(src, m.start, m.end, out, state)
                    const kind = PARTIAL_PATTERN.test(src.slice(at, control.end)) ? 'partial' : 'block'
                    state.record(kind, at, control.end, outStart, out.length, {
                        keyword: word,
                        branches: control.branches.length,
                        alternativeMarkup: control.branches.slice(1).some(b => b.markups.length > 0)
                    })
                    j = copyFrom = control.end
                    continue
                }
                j = at + 1
                continue
            }
            if (LINE_DIRECTIVES.has(word)) {
                flush(at)
                const nl = src.indexOf('\n', at)
                const stop = nl < 0 || nl > end ? end : nl
                state.record('directive', at, stop, out.length, out.length)
                j = copyFrom = stop
                continue
            }
            let exprStart = at + 1
            if (word === 'await') exprStart = skipSpaces(src, at + 6)
            const stop = scanImplicitExpression(src, exprStart)
            flush(at)
            emitExpression(src, at, stop, out, state)
            j = copyFrom = stop
            continue
        }
        j = at + 1
    }
    flush(end)
}

function emitExpression(src, start, stop, out, state) {
    const text = src.slice(start, stop)
    const outStart = out.length
    if (PARTIAL_PATTERN.test(text)) {
        const id = state.next++
        out.synthetic(`/*__RZP${id}__*/`, start)
        state.record('partial', start, stop, outStart, out.length, { placeholder: `__RZP${id}__` })
    } else {
        const id = state.next++
        out.synthetic(`__RZ${id}__`, start)
        state.record('expression', start, stop, outStart, out.length, { placeholder: `__RZ${id}__` })
    }
}

function createState(src) {
    const constructs = []
    return {
        next: 0,
        constructs,
        record(kind, origStart, origEnd, outStart, outEnd, extra = {}) {
            constructs.push({ kind, origStart, origEnd, outStart, outEnd, text: src.slice(origStart, origEnd), ...extra })
        }
    }
}

/** Masks a fragment that is entirely script content (used by tests and callers with snippets). */
export function maskScript(src) {
    const out = new MaskedOutput()
    const state = createState(src)
    maskMarkup(src, 0, src.length, out, state)
    return {
        text: out.parts.join(''),
        constructs: state.constructs,
        toOriginal: offset => out.toOriginal(offset),
        scripts: [{ outStart: 0, outEnd: out.length, origStart: 0, origEnd: src.length }]
    }
}

/**
 * Masks Razor inside every <script> element of a .cshtml file. Text outside script
 * elements is copied unchanged.
 */
export function maskCshtml(src) {
    const out = new MaskedOutput()
    const state = createState(src)
    const scripts = []
    const openTag = /<script\b[^>]*>/gi
    let copyFrom = 0
    let m
    while ((m = openTag.exec(src))) {
        const contentStart = m.index + m[0].length
        const close = src.toLowerCase().indexOf('</script', contentStart)
        const contentEnd = close < 0 ? src.length : close
        out.verbatim(src, copyFrom, contentStart)
        const outStart = out.length
        maskMarkup(src, contentStart, contentEnd, out, state)
        scripts.push({ outStart, outEnd: out.length, origStart: contentStart, origEnd: contentEnd })
        copyFrom = contentEnd
        openTag.lastIndex = contentEnd
    }
    out.verbatim(src, copyFrom, src.length)
    return {
        text: out.parts.join(''),
        constructs: state.constructs,
        toOriginal: offset => out.toOriginal(offset),
        scripts
    }
}

export function lineOf(src, offset) {
    let line = 1
    for (let i = 0; i < offset && i < src.length; i++) if (src[i] === '\n') line++
    return line
}
