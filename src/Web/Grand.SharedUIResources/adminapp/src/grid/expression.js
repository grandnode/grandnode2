//Tiny condition language for cell templates (data-if) and command visibility
//(visible-if). Deliberately not JavaScript and never evaluated with eval/new Function:
//
//  Field                       truthy (empty strings, empty arrays, 0, null are false)
//  !Field                      negation
//  Field == 'text'             comparison with a literal: == != > >= < <=
//  Field != 5 && Other         && binds tighter than ||, no parentheses
//
//Literals: numbers, 'single' or "double" quoted strings, true, false, null.
//Field paths may be dotted (Customer.Email); $texts.Name reads the grid's texts.

const tokenPattern = /\s*(\|\||&&|==|!=|>=|<=|>|<|!|'(?:[^'\\]|\\.)*'|"(?:[^"\\]|\\.)*"|-?\d+(?:\.\d+)?|[A-Za-z_$][\w$]*(?:\.[A-Za-z_$][\w$]*)*)/y

function tokenize(source) {
    const tokens = []
    tokenPattern.lastIndex = 0
    let position = 0
    while (position < source.length) {
        if (/^\s*$/.test(source.slice(position))) break
        tokenPattern.lastIndex = position
        const match = tokenPattern.exec(source)
        if (!match) throw new Error(`Invalid expression near "${source.slice(position)}"`)
        tokens.push(match[1])
        position = tokenPattern.lastIndex
    }
    return tokens
}

/** Reads a dotted path from a data item; "$texts.X" reads from scope.texts. */
export function readPath(item, path, scope) {
    let target = item
    let parts = path.split('.')
    if (parts[0] === '$texts') {
        target = scope?.texts
        parts = parts.slice(1)
    }
    for (const part of parts) {
        if (target == null) return undefined
        if (!Object.prototype.hasOwnProperty.call(Object(target), part)) return undefined
        target = target[part]
    }
    return target
}

function literal(token) {
    if (token === 'true') return { value: true }
    if (token === 'false') return { value: false }
    if (token === 'null') return { value: null }
    if (/^-?\d/.test(token)) return { value: Number(token) }
    if (token[0] === '\'' || token[0] === '"') return { value: token.slice(1, -1).replace(/\\(.)/g, '$1') }
    return null
}

function operand(token, item, scope) {
    const lit = literal(token)
    return lit ? lit.value : readPath(item, token, scope)
}

function truthy(value) {
    if (Array.isArray(value)) return value.length > 0
    return Boolean(value)
}

function compare(left, op, right) {
    //loose equality between numbers and numeric strings, like the Kendo templates did
    const l = typeof right === 'number' && typeof left === 'string' && left !== '' ? Number(left) : left
    switch (op) {
        case '==': return l === right || (l == null && right == null)
        case '!=': return !(l === right || (l == null && right == null))
        case '>': return l > right
        case '>=': return l >= right
        case '<': return l < right
        case '<=': return l <= right
    }
    return false
}

/**
 * Compiles a condition once; the result evaluates it against a data item.
 * Throws for syntax errors so a broken template fails loudly during development.
 */
export function compileCondition(source) {
    const tokens = tokenize(String(source ?? ''))
    if (tokens.length === 0) throw new Error('Empty expression')
    //split into OR groups of AND terms
    const groups = [[]]
    let current = []
    for (const token of tokens) {
        if (token === '||' || token === '&&') {
            if (current.length === 0) throw new Error(`Invalid expression "${source}"`)
            groups[groups.length - 1].push(current)
            current = []
            if (token === '||') groups.push([])
        } else {
            current.push(token)
        }
    }
    if (current.length === 0) throw new Error(`Invalid expression "${source}"`)
    groups[groups.length - 1].push(current)

    const terms = groups.map(group => group.map(term => {
        let negate = false
        let rest = term
        while (rest[0] === '!') {
            negate = !negate
            rest = rest.slice(1)
        }
        if (rest.length === 1 && !/^(==|!=|>=|<=|>|<)$/.test(rest[0])) {
            return (item, scope) => negate !== truthy(operand(rest[0], item, scope))
        }
        if (rest.length === 3 && /^(==|!=|>=|<=|>|<)$/.test(rest[1]) && !negate) {
            const [left, op, right] = rest
            return (item, scope) => compare(operand(left, item, scope), op, operand(right, item, scope))
        }
        throw new Error(`Invalid expression "${source}"`)
    }))

    return (item, scope) => terms.some(group => group.every(term => term(item, scope)))
}
