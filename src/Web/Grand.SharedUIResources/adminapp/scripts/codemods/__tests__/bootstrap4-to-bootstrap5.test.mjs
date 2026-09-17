import { describe, it, expect } from 'vitest'
import { convertClassValue, convertTagAttributes, convertView } from '../lib/bootstrap4-to-bootstrap5.mjs'

describe('class values', () => {
    it('renames the direction-aware utilities', () => {
        expect(convertClassValue('text-right float-left ml-2 pr-3').value)
            .toBe('text-end float-start ms-2 pe-3')
    })

    it('renames form-group to the margin utility that replaced it', () => {
        expect(convertClassValue('form-group row').value).toBe('mb-3 row')
    })

    it('drops form-group where a margin utility already says what the margin is', () => {
        expect(convertClassValue('form-group mb-0').value).toBe('mb-0')
        expect(convertClassValue('col-12 form-group mb-0 d-flex').value).toBe('col-12 mb-0 d-flex')
    })

    it('renames the custom form controls', () => {
        expect(convertClassValue('custom-control custom-checkbox').value).toBe('form-check')
        expect(convertClassValue('custom-control-input').value).toBe('form-check-input')
        expect(convertClassValue('custom-select form-control').value).toBe('form-select form-control')
    })

    it('drops the input group wrappers', () => {
        expect(convertClassValue('input-group-append').value).toBe('')
        expect(convertClassValue('btn input-group-prepend').value).toBe('btn')
    })

    it('turns a badge colour into a background utility', () => {
        expect(convertClassValue('badge badge-success badge-pill').value)
            .toBe('badge text-bg-success rounded-pill')
    })

    it('expands the names that became several', () => {
        expect(convertClassValue('media').value).toBe('d-flex')
        expect(convertClassValue('form-row').value).toBe('row g-2')
    })

    it('leaves a class attribute with nothing to change untouched', () => {
        const value = ' btn btn-primary  '
        expect(convertClassValue(value)).toEqual({ value, changed: false })
    })

    it('keeps the shape of a multi-line class attribute', () => {
        const value = 'form-group\n         col-md-6'
        expect(convertClassValue(value).value).toBe('mb-3\n         col-md-6')
    })

    it('rewrites a class name written as a C# string', () => {
        expect(convertClassValue('@(Model.Ok ? "text-right" : "text-left")').value)
            .toBe('@(Model.Ok ? "text-end" : "text-start")')
    })

    it('leaves a Razor expression it cannot read alone', () => {
        const value = 'btn @Model.ExtraClass'
        expect(convertClassValue(value)).toEqual({ value, changed: false })
    })
})

describe('data attributes', () => {
    it('renames a Bootstrap trigger and the options next to it', () => {
        expect(convertTagAttributes('<a data-toggle="modal" data-target="#x" data-backdrop="static">'))
            .toBe('<a data-bs-toggle="modal" data-bs-target="#x" data-bs-backdrop="static">')
    })

    it('leaves a data-toggle that is not a Bootstrap widget alone', () => {
        const tag = '<div data-toggle="grid" data-target="#products-grid">'
        expect(convertTagAttributes(tag)).toBe(tag)
    })

    it('leaves a data-target without a trigger alone', () => {
        const tag = '<input data-target="#search-results">'
        expect(convertTagAttributes(tag)).toBe(tag)
    })
})

describe('a whole view', () => {
    it('rewrites class attributes and tags and nothing else', () => {
        const src = [
            '@model ProductModel',
            '<div class="form-group row">',
            '    <label class="col-form-label col-md-3 text-right">@Loc["Name"]</label>',
            '    <div class="col-md-9">',
            '        <div class="input-group">',
            '            <input class="form-control"/>',
            '            <div class="input-group-append"><span class="input-group-text">kg</span></div>',
            '        </div>',
            '    </div>',
            '</div>',
            '<a href="#" data-toggle="collapse" data-target="#more">@Loc["More"]</a>',
            '<script>var cssClass = "form-group";</script>'
        ].join('\n')
        const { output, stats } = convertView(src)
        expect(output).toContain('<div class="mb-3 row">')
        expect(output).toContain('class="col-form-label col-md-3 text-end"')
        expect(output).toContain('<div class=""><span class="input-group-text">kg</span></div>')
        expect(output).toContain('data-bs-toggle="collapse" data-bs-target="#more"')
        //a class name inside a script is not a class attribute and is left alone
        expect(output).toContain('var cssClass = "form-group";')
        expect(stats.attributes).toBe(1)
        expect(stats.classes).toBe(3)
    })

    it('finds the end of a class attribute that carries Razor with a quote in it', () => {
        const src = '<td class="@(Model.Ok ? "ok" : "no") text-right">x</td><td class="ml-1">y</td>'
        const { output } = convertView(src)
        expect(output).toBe('<td class="@(Model.Ok ? "ok" : "no") text-end">x</td><td class="ms-1">y</td>')
    })

    it('is idempotent', () => {
        const src = '<div class="form-group text-right"><a data-toggle="tab" data-target="#t">x</a></div>'
        const once = convertView(src).output
        expect(convertView(once).output).toBe(once)
    })
})
