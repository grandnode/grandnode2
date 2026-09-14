import { describe, expect, it } from 'vitest'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { parseExpressionAt } from 'acorn'
import { analyzeCshtml, analyzeGridConfig, analyzeKendoTemplate, externalUsages } from '../lib/kendo-grid-analysis.mjs'
import { projectOf } from '../analyze-kendo-grids.mjs'

const fixture = name => readFileSync(fileURLToPath(new URL(`./fixtures/${name}`, import.meta.url)), 'utf8')

const analyze = code => {
    const node = parseExpressionAt(code, 0, { ecmaVersion: 'latest' })
    return analyzeGridConfig(node, code)
}

const readTransport = `transport: { read: { url: "/Admin/Measure/Weights", type: "POST", dataType: "json", data: addAntiForgeryToken } }`

describe('analyzeKendoTemplate', () => {
    it('counts encoded, raw and code parts', () => {
        const t = analyzeKendoTemplate('<a href="/Edit/#=Id#">#:Name#</a># if (Published) { #<b>#=kendo.htmlEncode(Sku)#</b># } #')
        expect(t).toMatchObject({ encoded: 2, raw: 1, rawNonTrivial: [], code: 2, complexCode: [], balanced: true })
    })

    it('reports raw output of fields that are not identifiers', () => {
        const t = analyzeKendoTemplate('<img src="#=PictureThumbnailUrl#" /> #=AttributeInfo#')
        expect(t.rawNonTrivial).toEqual(['PictureThumbnailUrl', 'AttributeInfo'])
    })

    it('flags loops as complex code and unbalanced hashes', () => {
        expect(analyzeKendoTemplate('# for (var i = 0; i < 3; i++) { # x # } #').complexCode).toEqual(['for (var i = 0; i < 3; i++) {'])
        expect(analyzeKendoTemplate('#=Name').balanced).toBe(false)
    })
})

describe('analyzeGridConfig', () => {
    it('classifies a read-only grid with encoded templates as A', () => {
        const result = analyze(`{
            dataSource: { ${readTransport}, schema: { data: "Data", total: "Total", errors: "Errors" },
                error: function (e) { display_kendoui_grid_error(e); this.cancelChanges(); },
                serverPaging: true, serverFiltering: true, serverSorting: true },
            pageable: { refresh: true },
            scrollable: false,
            columns: [
                { field: "Name", title: "__RZ0__", template: '<a href="Edit/#=Id#">#:Name#</a>' },
                { field: "Ratio", format: "{0:n8}", minScreenWidth: 500 }
            ]
        }`)
        expect(result.classification).toBe('A')
        expect(result.features).toMatchObject({ columns: 2, templatesEncoded: 1, templatesRaw: 1, minScreenWidth: 1, formats: ['{0:n8}'], transports: ['read'] })
    })

    it('treats inline CRUD with the standard requestEnd reload and edit/destroy commands as A', () => {
        const result = analyze(`{
            dataSource: {
                transport: { read: { url: "r" }, create: { url: "c" }, update: { url: "u" }, destroy: { url: "d" } },
                requestEnd: function (e) { if (e.type == "create" || e.type == "update") { this.read(); } }
            },
            toolbar: [{ name: "create", text: "__RZ1__" }],
            editable: { confirmation: true, mode: "inline" },
            columns: [{ field: "Name" }, { command: [{ name: "edit", text: { edit: "e" } }, { name: "destroy", text: "d" }] }]
        }`)
        expect(result.classification).toBe('A')
        expect(result.features).toMatchObject({ editMode: 'inline', toolbarCreate: true, commands: ['edit', 'destroy'] })
        expect(result.features.transports).toEqual(['read', 'create', 'update', 'destroy'])
    })

    it('sends editors, parameterMap, dataBound and custom click commands to B', () => {
        const result = analyze(`{
            dataSource: { transport: { read: { url: "r" }, parameterMap: function (data) { return data } } },
            dataBound: onDataBound,
            columns: [
                { field: "CategoryId", editor: categoryDropDownEditor },
                { command: { name: "primary", click: markAsPrimary } }
            ]
        }`)
        expect(result.classification).toBe('B')
        expect(result.reasonsB).toEqual(expect.arrayContaining(['parameterMap', 'event:dataBound', 'column-editor', 'custom-command-click']))
    })

    it('sends raw output of non-trivial fields and detail grids to B', () => {
        const result = analyze(`{
            dataSource: { transport: { read: { url: "r" } } },
            detailInit: detailInitCart,
            columns: [{ field: "Picture", template: '<img src="#=PictureThumbnailUrl#" />' }]
        }`)
        expect(result.classification).toBe('B')
        expect(result.reasonsB).toEqual(expect.arrayContaining(['detail', 'raw-template-output']))
    })

    it('sends dynamic columns and data sources to C', () => {
        const result = analyze('{ dataSource: makeDataSource(url), columns: gridColumns }')
        expect(result.classification).toBe('C')
        expect(result.reasonsC).toEqual(['dynamic-datasource', 'dynamic-columns'])
    })

    it('sends grids with injected partials to C', () => {
        const code = '{ dataSource: { transport: { read: { url: "r" } } }, columns: [{ field: "A" }] }'
        const node = parseExpressionAt(code, 0, { ecmaVersion: 'latest' })
        const result = analyzeGridConfig(node, code, [{ kind: 'partial' }])
        expect(result.classification).toBe('C')
        expect(result.reasonsC).toContain('razor-partial')
    })
})

describe('analyzeCshtml', () => {
    it('finds, masks and classifies every grid in a view', () => {
        const { grids, external } = analyzeCshtml(fixture('current-carts.cshtml'))

        expect(grids.map(g => [g.name, g.line])).toEqual([['#carts-grid', 6], ['(detail grid)', 56]])

        const [main, detail] = grids
        expect(main.analysis.classification).toBe('B')
        expect(main.analysis.reasonsB).toEqual(expect.arrayContaining(['razor-conditional', 'detail']))
        expect(main.analysis.features).toMatchObject({ columns: 3, templatesEncoded: 1, templatesRaw: 1, razorBlocks: 1 })
        expect(main.analysis.features.razorExpressions).toBeGreaterThanOrEqual(6)

        expect(detail.analysis.classification).toBe('B')
        expect(detail.analysis.reasonsB).toContain('raw-template-output')

        expect(external).toEqual({ count: 1, methods: { 'dataSource.read': 1 } })
    })

    it('reports a grid that does not parse after masking as C', () => {
        const { grids } = analyzeCshtml('<script>$("#g").kendoGrid({ columns: [ { field: "A" } @Html.Raw(extraColumns) ] });</script>')
        expect(grids).toHaveLength(1)
        expect(grids[0].analysis.classification).toBe('C')
        expect(grids[0].analysis.reasonsC).toContain('parse-error')
        expect(grids[0].parseError).toMatch(/line 1/)
    })

    it('reports grids that only exist in an @else branch', () => {
        const src = '<script>\n@if (a) { <text>var x = 1;</text> } else { <text>$("#g").kendoGrid({ columns: [] });</text> }\n</script>'
        const { grids } = analyzeCshtml(src)
        expect(grids).toHaveLength(1)
        expect(grids[0]).toMatchObject({ name: '#g', line: 2 })
        expect(grids[0].analysis.reasonsC).toEqual(['inside-alternative-razor-branch'])
    })
})

describe('helpers', () => {
    it('groups external instance usages by member, chained or through a variable', () => {
        const src = [
            `$("#a").data('kendoGrid').dataSource.page(1);`,
            `var g = $("#b").data("kendoGrid");`,
            `var row = g.dataItem(tr); g.select(); g.select(); notg.refresh();`,
            `callSomething($("#c").data("kendoGrid"));`
        ].join('\n')
        expect(externalUsages(src)).toEqual({
            count: 3,
            methods: { 'dataSource.page': 1, dataItem: 1, select: 1, '(instance)': 1 }
        })
    })

    it('derives the project from the path', () => {
        expect(projectOf('src/Web/Grand.Web.Admin/Areas/Admin/Views/Measure/Partials/Weights.cshtml')).toBe('Grand.Web.Admin')
        expect(projectOf('src/Plugins/Shipping.ByWeight/Areas/Admin/Views/Configure.cshtml')).toBe('Plugins/Shipping.ByWeight')
    })
})
