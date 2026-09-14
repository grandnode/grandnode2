import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { cleanupScripts, convertCshtml, parameterMapDecimals, razorForMarkup } from '../lib/kendo-grid-convert.mjs'
import { convertCondition, convertKendoTemplate, negateCondition, singleElement, tokenizeKendoTemplate } from '../lib/kendo-template.mjs'
import { unifiedDiff } from '../lib/diff.mjs'
import { parseArgs } from '../kendo-grid-to-admin-grid.mjs'

const fixture = name => readFileSync(fileURLToPath(new URL(`./fixtures/${name}`, import.meta.url)), 'utf8').replace(/\r\n/g, '\n')

describe('convertCondition', () => {
    it('translates paths, literals, comparisons and && / ||', () => {
        expect(convertCondition('IsRequired')).toBe('IsRequired')
        expect(convertCondition('!data.Published')).toBe('!Published')
        expect(convertCondition('OrderStatusId === 10')).toBe('OrderStatusId == 10')
        expect(convertCondition("StoreId !== '' && StoreId != null")).toBe("StoreId != '' && StoreId != null")
        expect(convertCondition('AttributeInfo && AttributeInfo.length > 0 || Warnings')).toBe('AttributeInfo && AttributeInfo.length > 0 || Warnings')
        expect(convertCondition('Warnings !== undefined')).toBe('Warnings != null')
    })

    it('restores Razor placeholders', () => {
        const restore = s => s.replace('__RZ1__', '@((int)ProductType.GroupedProduct)').replace('__RZ2__', '@Model.StoreId')
        expect(convertCondition('ProductTypeId != __RZ1__', restore)).toBe('ProductTypeId != @((int)ProductType.GroupedProduct)')
        expect(convertCondition("StoreId == '__RZ2__'", restore)).toBe("StoreId == '@Model.StoreId'")
    })

    it('refuses what the expression language cannot say', () => {
        expect(convertCondition('(a || b) && c')).toBeNull()
        expect(convertCondition('!(a && b)')).toBeNull()
        expect(convertCondition('getName(Area)')).toBeNull()
        expect(convertCondition("typeof X !== 'undefined'")).toBeNull()
    })

    it('negates simple conditions only', () => {
        expect(negateCondition('Published')).toBe('!Published')
        expect(negateCondition('A >= 3')).toBe('A < 3')
        expect(negateCondition('A && B')).toBeNull()
    })
})

describe('convertKendoTemplate', () => {
    it('tokenizes escaped hashes', () => {
        expect(tokenizeKendoTemplate('<a href="\\#tab">#:Name#</a>').parts.map(p => p.type)).toEqual(['html', 'encoded', 'html'])
    })

    it('puts if/else on single elements and wraps text in spans', () => {
        const result = convertKendoTemplate('# if(Published) {# <i class="fa fa-check"></i> #} else {# <i class="fa fa-times"></i> #} #')
        expect(result.lines).toEqual(['<i data-if="Published" class="fa fa-check"></i>', '<i data-else class="fa fa-times"></i>'])
        expect(convertKendoTemplate('# if(ProductTypeId != 10) {# #:Price# #}  #').lines).toEqual(['<span data-if="ProductTypeId != 10">{{ Price }}</span>'])
    })

    it('encodes raw output and flags fields that look like markup', () => {
        const result = convertKendoTemplate('<a href="Edit/#=Id#">#=kendo.htmlEncode(Name)#</a> #=AttributeInfo#')
        expect(result.lines).toEqual(['<a href="Edit/{{ Id }}">{{ Name }}</a> {{ AttributeInfo }}'])
        expect(result.rawFields).toEqual(['AttributeInfo'])
        expect(result.markers[0]).toContain('{{{ AttributeInfo }}}')
    })

    it('formats kendo.toString and moves localized texts to grid-text', () => {
        const texts = []
        const result = convertKendoTemplate('#=kendo.toString(Price, "n2")# __RZ0__', {
            restore: s => s,
            textFor: p => {
                texts.push(p)
                return '{{ $texts.view }}'
            }
        })
        expect(result.lines).toEqual(['{{ Price | n2 }} {{ $texts.view }}'])
        expect(texts).toEqual(['__RZ0__'])
    })

    it('reports code it cannot translate', () => {
        expect(convertKendoTemplate('#=ratingCellTemplate(Rating)#').ok).toBe(false)
        expect(convertKendoTemplate('# for (var i = 0; i < 3; i++) { # x # } #').ok).toBe(false)
    })

    it('refuses conditions that build attribute values', () => {
        const result = convertKendoTemplate('<span class="label label-# if(OrderStatusId == 10) {#warning #} #">#=OrderStatus#</span>')
        expect(result.ok).toBe(false)
        expect(result.markers[0]).toContain('inside a tag')
    })

    it('translates nested conditions', () => {
        expect(convertKendoTemplate('# if(!CalculateByPlugin) { if(UsePercentage) {# #=DiscountPercentage# % #} else {# #=DiscountAmount# #} } #').lines)
            .toEqual(['<span data-if="!CalculateByPlugin"><span data-if="UsePercentage">{{ DiscountPercentage }} %</span> <span data-else>{{ DiscountAmount }}</span></span>'])
    })

    it('detects single elements', () => {
        expect(singleElement('<a href="x"><span>1</span></a>')).toBe(true)
        expect(singleElement('<br/>')).toBe(true)
        expect(singleElement('<i></i> <i></i>')).toBe(false)
        expect(singleElement('text <b>x</b>')).toBe(false)
    })
})

describe('helpers', () => {
    it('turns Html.Raw(Url.Action(...)) into markup Razor', () => {
        expect(razorForMarkup('@Html.Raw(Url.Action("List", "Blog", new { area }))')).toBe('@Url.Action("List", "Blog", new { area })')
        expect(razorForMarkup('@Html.Raw(Model.Json)')).toBe('@(Model.Json)')
        expect(razorForMarkup('@Loc["Admin.Common.Edit"]')).toBe('@Loc["Admin.Common.Edit"]')
    })

    it('reads decimals from the numeric parameterMap pattern and refuses others', () => {
        const decimals = parameterMapDecimals('function (data, operation) { if (operation != "read") { data.Ratio = kendo.toString(data.Ratio, "n8"); return data; } else { return data; } }')
        expect([...decimals]).toEqual([['Ratio', 8]])
        expect(() => parameterMapDecimals('function (data, operation) { return JSON.stringify(data); }')).toThrow('parameterMap')
    })

    it('removes ready handlers and script elements left empty', () => {
        expect(cleanupScripts('<p></p>\n<script>\n    $(document).ready(function () {\n    });\n</script>\n<b></b>\n')).toBe('<p></p>\n<b></b>\n')
        expect(cleanupScripts('<script>\n    $(document).ready(function () {\n        go();\n    });\n</script>\n')).toContain('go();')
    })

    it('prints a unified diff', () => {
        expect(unifiedDiff('a\nb\nc\n', 'a\nB\nc\n', 'x.cshtml')).toBe('--- a/x.cshtml\n+++ b/x.cshtml\n@@ -1,4 +1,4 @@\n a\n-b\n+B\n c\n \n')
        expect(unifiedDiff('same', 'same', 'x')).toBe('')
    })

    it('parses the command line', () => {
        expect(parseArgs(['--path', 'Grand.Web.AdminShared\\Views', '--write'])).toMatchObject({ paths: ['Grand.Web.AdminShared/Views'], write: true })
        expect(() => parseArgs(['--nope'])).toThrow('unknown option')
    })
})

//Fixtures: the pilot views as they were before the hand conversion (git history) and the
//codemod output a reviewer starts from. Compare with the committed pilots: markup and
//attribute order follow them; what the pilots fixed by hand is marked CODEMOD-REVIEW.
describe('convertCshtml fixtures', () => {
    for (const name of ['address-attributes', 'current-carts', 'weights', 'product-prices', 'vendor-shipments']) {
        it(`converts ${name}`, () => {
            const { output } = convertCshtml(fixture(`${name}.before.cshtml`), { file: name })
            expect(output).toBe(fixture(`${name}.expected.cshtml`))
        })
    }

    it('converts a clean grid without markers and removes its script', () => {
        const { output, grids } = convertCshtml(fixture('address-attributes.before.cshtml'))
        expect(grids).toMatchObject([{ grid: '#addressattributes-grid', class: 'A', action: 'converted', markers: [] }])
        expect(output).not.toContain('kendoGrid')
        expect(output).not.toContain('<script>')
        expect(output).toContain('<grid-text name="view" value="@Loc["Admin.Common.View"]"/>')
    })

    it('keeps @if column blocks of detail grids and the settings they use', () => {
        const { output, grids } = convertCshtml(fixture('current-carts.before.cshtml'))
        expect(grids).toHaveLength(1)
        expect(output).toContain('@inject AdminAreaSettings adminAreaSettings')
        expect(output).toMatch(/@if \(!adminAreaSettings\.HideStoreColumn\)\n\s+\{\n\s+<grid-column field="Store"/)
        expect(output).not.toContain('detailInitCart')
    })

    it('marks what needs a reviewer', () => {
        const { output, grids } = convertCshtml(fixture('vendor-shipments.before.cshtml'))
        expect(grids[0].action).toBe('review')
        expect(output).toContain('@* CODEMOD-REVIEW: checkbox column')
        expect(output).toContain('selectable="Checkbox"')
    })

    it('names popup checkboxes, keeps a pager without page sizes and refuses data objects', () => {
        const src = [
            '<div id="g"></div>',
            '<script>',
            '    function additionalData() { return {}; }',
            '    $("#g").kendoGrid({',
            '        dataSource: { transport: { read: { url: "/r", data: additionalData } }, pageSize: 5 },',
            '        pageable: { refresh: true },',
            '        columns: [',
            '            { field: "Id", headerTemplate: "<input id=\'mastercheckbox\' type=\'checkbox\'/>", template: "<input type=\'checkbox\' name=\'SelectedProductIds\' value=\'#=Id#\' class=\'checkboxGroups\' />" },',
            '            { field: "Name" }',
            '        ]',
            '    });',
            '</script>',
            ''
        ].join('\n')
        const { output, grids } = convertCshtml(src)
        expect(grids[0].action).toBe('review')
        expect(output).toContain('selectable="Checkbox"')
        expect(output).toContain('selection-name="SelectedProductIds"')
        expect(output).toContain('page-size="5"')
        expect(output).toContain('page-sizes=""')
        expect(output).toContain('additional-data="additionalData"')
        const objectData = convertCshtml(src.replace('function additionalData() { return {}; }', 'var additionalData = { id: "1" };'))
        expect(objectData.grids[0].action).toBe('skipped')
    })

    it('skips grids configured twice in Razor branches and partials injecting columns', () => {
        const twice = `@if (a)\n{\n<div id="g"></div>\n<script>$("#g").kendoGrid({ dataSource: { transport: { read: { url: "/r" } } }, columns: [{ field: "A" }] });</script>\n}\nelse\n{\n<script>$("#g").kendoGrid({ dataSource: { transport: { read: { url: "/r" } } }, columns: [{ field: "B" }] });</script>\n}\n`
        const result = convertCshtml(twice)
        expect(result.output).toBe(twice)
        expect(result.grids.every(g => g.action === 'skipped')).toBe(true)

        const partial = '<div id="g"></div>\n<script>\n$("#g").kendoGrid({ dataSource: { transport: { read: { url: "/r" } } }, columns: [@await Html.PartialAsync("Cols") { field: "A" }] });\n</script>\n'
        const skipped = convertCshtml(partial)
        expect(skipped.output).toBe(partial)
        expect(skipped.grids[0].reasons[0]).toContain('partial')
    })
})
