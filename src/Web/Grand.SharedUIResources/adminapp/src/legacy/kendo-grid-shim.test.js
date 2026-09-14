// @vitest-environment jsdom
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { createRequire } from 'node:module'
import { compileKendoTemplate, htmlEncode, kendoTemplateSource } from './kendo-template.js'
import { installKendoGridShim, mapKendoGridOptions } from './kendo-grid-shim.js'

const require = createRequire(import.meta.url)
let $

beforeAll(() => {
    const jquery = require('jquery')
    $ = typeof jquery.param === 'function' ? jquery : jquery(window)
})

afterEach(() => {
    vi.restoreAllMocks()
})

describe('kendo template compiler', () => {
    it('supports #= #, #: # and code blocks', () => {
        const template = compileKendoTemplate('<a href="Edit/#=Id#">#:Name#</a># if (Published) {# <i class="fa fa-check"></i> #} else {# <i class="fa fa-times"></i> #}#')
        expect(template({ Id: 5, Name: '<b>x</b>', Published: true })).toBe('<a href="Edit/5">&lt;b&gt;x&lt;/b&gt;</a> <i class="fa fa-check"></i> ')
        expect(template({ Id: 5, Name: 'y', Published: false })).toBe('<a href="Edit/5">y</a> <i class="fa fa-times"></i> ')
    })

    it('keeps #= # raw like Kendo', () => {
        expect(compileKendoTemplate('#=AttributeInfo#')({ AttributeInfo: 'Color: <b>Red</b>' })).toBe('Color: <b>Red</b>')
    })

    it('handles escaped hashes, quotes, backslashes and newlines in literals', () => {
        const template = compileKendoTemplate("<a href='\\#tab' onclick=\"go('#=Id#')\">C:\\dir\nline</a>")
        expect(template({ Id: 'a1' })).toBe("<a href='#tab' onclick=\"go('a1')\">C:\\dir\nline</a>")
    })

    it('calls functions and reads globals from expressions', () => {
        window.getName = id => ({ 1: 'Admin' })[id]
        expect(compileKendoTemplate('#=getName(Area)#')({ Area: 1 })).toBe('Admin')
        delete window.getName
    })

    it('encodes the five HTML characters', () => {
        expect(htmlEncode(`<&>"'`)).toBe('&lt;&amp;&gt;&quot;&#39;')
    })

    it('generates a with block by default', () => {
        expect(kendoTemplateSource('#=a#')).toContain('with(data){')
        expect(kendoTemplateSource('#=data.a#', { useWithBlock: false })).not.toContain('with(')
    })
})

//Measure/Weights before its conversion, trimmed
function weightsOptions() {
    return {
        dataSource: {
            transport: {
                read: { url: '/Admin/Measure/Weights', type: 'POST', dataType: 'json', data: window.addAntiForgeryToken },
                create: { url: '/Admin/Measure/WeightAdd', type: 'POST', dataType: 'json', data: window.addAntiForgeryToken },
                update: { url: '/Admin/Measure/WeightUpdate', type: 'POST', dataType: 'json', data: window.addAntiForgeryToken },
                destroy: { url: '/Admin/Measure/WeightDelete', type: 'POST', dataType: 'json', data: window.addAntiForgeryToken },
                parameterMap: data => data
            },
            schema: {
                data: 'Data', total: 'Total', errors: 'Errors',
                model: {
                    id: 'Id',
                    fields: {
                        Name: { editable: true, type: 'string' },
                        Ratio: { editable: true, type: 'number' },
                        DisplayOrder: { editable: true, type: 'number' },
                        IsPrimaryWeight: { editable: false, type: 'boolean' },
                        Id: { editable: false, type: 'string' }
                    }
                }
            },
            requestEnd: function () { },
            error: function () { },
            serverPaging: true,
            serverFiltering: true,
            serverSorting: true
        },
        pageable: { refresh: true, numeric: false, previousNext: false, info: false },
        toolbar: [{ name: 'create', text: 'Add new record' }],
        editable: { confirmation: false, mode: 'inline' },
        scrollable: false,
        columns: [
            { field: 'Name', title: 'Name', width: 300 },
            { field: 'Ratio', title: 'Ratio', width: 200, editor: function () { } },
            { field: 'DisplayOrder', title: 'Display order', format: '{0:0}', width: 100, attributes: { style: 'text-align:center' } },
            { field: 'Id', title: 'Primary', width: 150, template: '# if(IsPrimaryWeight) {#yes#} else {#no#} #' },
            { command: [{ name: 'edit', text: { edit: 'Edit', update: 'Update', cancel: 'Cancel' } }, { name: 'destroy', text: 'Delete' }], width: 200 }
        ]
    }
}

describe('mapKendoGridOptions', () => {
    function context() {
        return { texts: {}, kendoApi: () => null, registerColumnEditor: vi.fn(() => 'kendoShimEditor1') }
    }

    it('maps the Measure/Weights configuration', () => {
        const warn = vi.spyOn(console, 'warn').mockImplementation(() => { })
        const ctx = context()
        const config = mapKendoGridOptions(weightsOptions(), ctx)
        expect(warn).not.toHaveBeenCalled()
        expect(config.key).toBe('Id')
        expect(config.transport).toEqual({ read: '/Admin/Measure/Weights', create: '/Admin/Measure/WeightAdd', update: '/Admin/Measure/WeightUpdate', destroy: '/Admin/Measure/WeightDelete' })
        expect(config.pager).toBe('Compact')
        expect(config.editMode).toBe('Inline')
        expect(config.confirmDestroy).toBe(false)
        expect(config.toolbar.create).toBe('Add new record')
        expect(config.commands).toMatchObject({ edit: true, destroy: true, width: 200 })
        expect(ctx.texts).toMatchObject({ edit: 'Edit', update: 'Update', cancel: 'Cancel', delete: 'Delete' })
        expect(typeof config.parameterMap).toBe('function')
        expect(config.events.requestEnd).toBeTypeOf('function')
        expect(config.events.error).toBeTypeOf('function')

        const [name, ratio, order, primary] = config.columns
        expect(name).toMatchObject({ field: 'Name', editor: 'Text', editable: true, width: 300, postRaw: true })
        expect(ratio).toMatchObject({ field: 'Ratio', editor: 'Custom', editorName: 'kendoShimEditor1' })
        expect(order).toMatchObject({ editor: 'Numeric', format: '{0:0}', align: 'center' })
        expect(primary.editable).toBe(false)
        expect(primary.renderHtml({ IsPrimaryWeight: true })).toBe('yes')
    })

    it('warns about unsupported options', () => {
        const warn = vi.spyOn(console, 'warn').mockImplementation(() => { })
        mapKendoGridOptions({
            groupable: true,
            selectable: 'multiple',
            editable: 'popup',
            dataSource: { batch: true, aggregate: [] },
            toolbar: ['save'],
            columns: [{ field: 'A', aggregates: ['sum'] }, { command: [{ name: 'custom' }] }]
        }, context())
        const messages = warn.mock.calls.map(call => call[0])
        expect(messages).toEqual(expect.arrayContaining([
            '[GrandGrid shim] unsupported: groupable',
            '[GrandGrid shim] unsupported: selectable',
            '[GrandGrid shim] unsupported: editable "popup"',
            '[GrandGrid shim] unsupported: dataSource.batch',
            '[GrandGrid shim] unsupported: dataSource.aggregate',
            '[GrandGrid shim] unsupported: toolbar "save"',
            '[GrandGrid shim] unsupported: columns.aggregates',
            '[GrandGrid shim] unsupported: columns.command "custom"'
        ]))
    })

    it('maps full pagers with page sizes and editable true with confirmation', () => {
        const config = mapKendoGridOptions({
            dataSource: { pageSize: 15, transport: { read: '/r' } },
            pageable: { refresh: true, pageSizes: [10, 15, 20] },
            editable: true,
            columns: [{ field: 'Name', headerTemplate: '<b>x</b>', encoded: false }]
        }, context())
        expect(config).toMatchObject({ pager: 'Full', pageSizes: [10, 15, 20], pageSize: 15, confirmDestroy: true })
        expect(config.columns[0]).toMatchObject({ titleHtml: '<b>x</b>', encoded: false })
    })

    it('passes the grid API as this to custom command clicks', () => {
        const api = { dataItem: vi.fn() }
        const click = vi.fn(function () { return this })
        const config = mapKendoGridOptions({
            columns: [{ command: [{ name: 'details', text: 'Details', click }] }]
        }, { ...context(), kendoApi: () => api })
        const event = { currentTarget: {} }
        config.commands.custom[0].click({ Id: 1 }, event)
        expect(click).toHaveBeenCalledWith(event)
        expect(click.mock.results[0].value).toBe(api)
    })
})

describe('installKendoGridShim', () => {
    it('does not replace a real kendoGrid', () => {
        const kendoGrid = function () { }
        const win = { jQuery: { fn: { kendoGrid } } }
        expect(installKendoGridShim(win)).toBe(false)
        expect(win.jQuery.fn.kendoGrid).toBe(kendoGrid)
    })

    it('registers $.fn.kendoGrid and builds grids through GrandAdmin.grids', async () => {
        document.body.innerHTML = '<div id="g"></div>'
        delete $.fn.kendoGrid
        const read = vi.fn()
        const grid = { api: { marker: true }, ready: Promise.resolve(), dataSource: { read } }
        const construct = vi.fn(() => grid)
        const win = { jQuery: $, GrandAdmin: { grids: { construct, defaults: { culture: { name: 'pl-PL' }, texts: { edit: 'Edytuj' } }, editors: { register: vi.fn() } } } }
        expect(installKendoGridShim(win)).toBe(true)
        $('#g').kendoGrid({ dataSource: { transport: { read: '/r' } }, columns: [{ field: 'Name' }] })
        await grid.ready
        expect(construct).toHaveBeenCalledTimes(1)
        const [element, config] = construct.mock.calls[0]
        expect(element.id).toBe('g')
        expect(config.culture.name).toBe('pl-PL')
        expect(config.texts.edit).toBe('Edytuj')
        expect($('#g').data('kendoGrid')).toBe(grid.api)
        expect(read).toHaveBeenCalledTimes(1)
        delete $.fn.kendoGrid
    })
})
