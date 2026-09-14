import { describe, expect, it } from 'vitest'
import { parseExpressionAt } from 'acorn'
import { maskCshtml, maskScript, scanBalanced, skipCSharpString } from '../lib/razor-mask.mjs'

const kinds = masked => masked.constructs.map(c => c.kind)

describe('C# scanning', () => {
    it('skips interpolated strings with holes', () => {
        const src = '$"{Scope.ResourceKeyPrefix}.Common.Edit"]'
        expect(src[skipCSharpString(src, 0)]).toBe(']')
    })

    it('skips verbatim strings with doubled quotes', () => {
        const src = '@"a""b" rest'
        expect(src.slice(skipCSharpString(src, 0))).toBe(' rest')
    })

    it('balances parentheses around nested calls and strings containing brackets', () => {
        const src = '(Url.Action("Edit)", "Customer", new { area = Constants.AreaAdmin }))/#=Id#'
        expect(src.slice(scanBalanced(src, 0))).toBe('/#=Id#')
    })
})

describe('maskScript', () => {
    it('replaces @Loc inside a JS string with an identifier placeholder', () => {
        const masked = maskScript('title: "@Loc["Admin.Common.Edit"]"')
        expect(masked.text).toBe('title: "__RZ0__"')
        expect(kinds(masked)).toEqual(['expression'])
    })

    it('masks interpolated @Loc keys', () => {
        const masked = maskScript('text: "@Loc[$"{Scope.ResourceKeyPrefix}.Common.AddNewRecord"]" }')
        expect(masked.text).toBe('text: "__RZ0__" }')
    })

    it('masks explicit expressions in value position', () => {
        const masked = maskScript('pageSize: @(adminAreaSettings.DefaultGridPageSize), pageSizes: [@(adminAreaSettings.GridPageSizes)]')
        expect(masked.text).toBe('pageSize: __RZ0__, pageSizes: [__RZ1__]')
    })

    it('masks @Html.Raw(Url.Action(...)) including the nested quotes', () => {
        const masked = maskScript('url: "@Html.Raw(Url.Action("CurrentCarts", "ShoppingCart", new { area = Constants.AreaAdmin }))",')
        expect(masked.text).toBe('url: "__RZ0__",')
    })

    it('stops an implicit expression at a trailing dot or slash', () => {
        const masked = maskScript("href: '@Url.Action(\"Edit\", \"Product\")/#=Id#'. @Model.Name.")
        expect(masked.text).toBe("href: '__RZ0__/#=Id#'. __RZ1__.")
    })

    it('turns partials into comments so the surrounding object still parses', () => {
        const masked = maskScript('{ a: 1, @await Html.PartialAsync("Partials/Categories.LinkTemplate") }')
        expect(masked.text).toBe('{ a: 1, /*__RZP0__*/ }')
        expect(kinds(masked)).toEqual(['partial'])
        expect(() => parseExpressionAt(masked.text, 0, { ecmaVersion: 'latest' })).not.toThrow()
    })

    it('keeps the first branch of @if/<text> and records the block', () => {
        const src = [
            'columns: [{ field: "A" },',
            '@if (!adminAreaSettings.HideStoreColumn)',
            '{',
            '    <text>{ field: "Store", title: "@Loc["Admin.Store"]" },</text>',
            '}',
            '{ field: "B" }]'
        ].join('\n')
        const masked = maskScript(src)
        expect(masked.text.replace(/\s+/g, ' ')).toBe('columns: [{ field: "A" }, { field: "Store", title: "__RZ0__" }, { field: "B" }]')
        const block = masked.constructs.find(c => c.kind === 'block')
        expect(block).toMatchObject({ keyword: 'if', branches: 1, alternativeMarkup: false })
    })

    it('records alternative else branches without emitting them', () => {
        const masked = maskScript('x: @if (a) { <text>1</text> } else { <text>2</text> }, y: 3')
        expect(masked.text).toBe('x: 1, y: 3')
        expect(masked.constructs.find(c => c.kind === 'block')).toMatchObject({ branches: 2, alternativeMarkup: true })
    })

    it('handles @: lines and JS apostrophes inside markup branches', () => {
        const masked = maskScript("@foreach (var s in Model.Items)\n{\n    @:'@s.Name',\n}\nend")
        expect(masked.text.replace(/\s+/g, ' ').trim()).toBe("'__RZ0__', end")
    })

    it('removes Razor comments and unescapes @@', () => {
        const masked = maskScript('a @* comment with "quotes" *@ b @@click c')
        expect(masked.text).toBe('a  b @click c')
    })

    it('does not treat e-mail addresses as transitions', () => {
        expect(maskScript('"support@grandnode.com"').text).toBe('"support@grandnode.com"')
    })

    it('maps masked offsets back to original offsets', () => {
        const src = 'title: "@Loc["X"]", field: "Name"'
        const masked = maskScript(src)
        const maskedIndex = masked.text.indexOf('field')
        expect(masked.toOriginal(maskedIndex)).toBe(src.indexOf('field'))
        expect(masked.toOriginal(masked.text.indexOf('__RZ0__'))).toBe(src.indexOf('@Loc'))
    })
})

describe('maskCshtml', () => {
    it('masks only the content of script elements', () => {
        const src = '<h1>@Loc["Title"]</h1>\n<script>\nvar t = "@Loc["X"]";\n</script>\n<p>@Model.Name</p>'
        const masked = maskCshtml(src)
        expect(masked.text).toBe('<h1>@Loc["Title"]</h1>\n<script>\nvar t = "__RZ0__";\n</script>\n<p>@Model.Name</p>')
        expect(masked.scripts).toHaveLength(1)
    })
})
