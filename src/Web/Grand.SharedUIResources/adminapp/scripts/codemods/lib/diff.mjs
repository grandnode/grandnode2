/*
 * Minimal line-based unified diff for the codemod's dry run (no dependency). Views are a
 * few hundred lines, so the O(n*m) longest common subsequence table is cheap.
 */

function lcsTable(a, b) {
    const rows = a.length + 1
    const cols = b.length + 1
    const table = new Array(rows)
    for (let i = 0; i < rows; i++) table[i] = new Uint32Array(cols)
    for (let i = a.length - 1; i >= 0; i--) {
        for (let j = b.length - 1; j >= 0; j--) {
            table[i][j] = a[i] === b[j] ? table[i + 1][j + 1] + 1 : Math.max(table[i + 1][j], table[i][j + 1])
        }
    }
    return table
}

/** Edit script: [{ type: ' ' | '-' | '+', line, a, b }] with 0-based line numbers. */
export function diffLines(a, b) {
    const table = lcsTable(a, b)
    const ops = []
    let i = 0
    let j = 0
    while (i < a.length && j < b.length) {
        if (a[i] === b[j]) {
            ops.push({ type: ' ', line: a[i], a: i++, b: j++ })
        } else if (table[i + 1][j] >= table[i][j + 1]) {
            ops.push({ type: '-', line: a[i], a: i++, b: j })
        } else {
            ops.push({ type: '+', line: b[j], a: i, b: j++ })
        }
    }
    while (i < a.length) ops.push({ type: '-', line: a[i], a: i++, b: j })
    while (j < b.length) ops.push({ type: '+', line: b[j], a: i, b: j++ })
    return ops
}

export function unifiedDiff(before, after, name, context = 3) {
    const a = before.split(/\r?\n/)
    const b = after.split(/\r?\n/)
    const ops = diffLines(a, b)
    const changed = ops.map((op, index) => (op.type !== ' ' ? index : -1)).filter(index => index >= 0)
    if (!changed.length) return ''
    const hunks = []
    let current = null
    for (const index of changed) {
        const start = Math.max(0, index - context)
        const end = Math.min(ops.length - 1, index + context)
        if (current && start <= current.end + 1) current.end = end
        else {
            current = { start, end }
            hunks.push(current)
        }
    }
    const out = [`--- a/${name}`, `+++ b/${name}`]
    for (const hunk of hunks) {
        const slice = ops.slice(hunk.start, hunk.end + 1)
        const aStart = slice[0].a + 1
        const bStart = slice[0].b + 1
        const aCount = slice.filter(op => op.type !== '+').length
        const bCount = slice.filter(op => op.type !== '-').length
        out.push(`@@ -${aStart},${aCount} +${bStart},${bCount} @@`)
        for (const op of slice) out.push(op.type + op.line)
    }
    return out.join('\n') + '\n'
}
