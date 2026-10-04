// @vitest-environment jsdom
import { describe, expect, it, beforeAll } from 'vitest'
import { createRequire } from 'node:module'
import { param, serializeFields } from './param.js'

const require = createRequire(import.meta.url)
let $

beforeAll(() => {
    //jquery 2.2.4, the version the panels load; with a global document the module is
    //jQuery itself, without one it is a factory taking a window
    const jquery = require('jquery')
    $ = typeof jquery.param === 'function' ? jquery : jquery(window)
})

const cases = {
    'read request with paging and additional data': {
        take: 15, skip: 15, page: 2, pageSize: 15,
        SearchProductName: 'red shirt & co', SearchIncludeSubCategories: false, SearchStoreId: '',
        __RequestVerificationToken: 'CfDJ8+/=abc'
    },
    'model with numbers, booleans, null and undefined': {
        Id: '5f1', Ratio: 0.45359237, Tiny: 1e-7, DisplayOrder: 0, IsPrimaryWeight: true, Name: null, Other: undefined
    },
    'nested objects and arrays': {
        filter: { logic: 'and', filters: [{ field: 'Name', operator: 'contains', value: 'a b' }] },
        selectedIds: ['1', '2'],
        'explicit[]': ['x', 'y'],
        matrix: [[1, 2], [3]],
        Locales: [{ LanguageId: 'l1', Name: 'Nazwa ąę' }]
    },
    'unicode and reserved characters': {
        Value: 'zażółć gęślą jaźń <b>"quoted"</b> 100% + #hash ?q=1',
        'Search.ResourceName': 'a=b'
    },
    'function values and dates': {
        lazy: () => 'computed',
        when: new Date(Date.UTC(2024, 0, 31, 10, 0, 0))
    },
    'empty object': {}
}

describe('param', () => {
    for (const [name, data] of Object.entries(cases)) {
        it(`matches jQuery.param: ${name}`, () => {
            expect(param(data)).toBe($.param(data))
        })
    }

    it('serializes an array of name/value pairs like jQuery', () => {
        const fields = [{ name: 'a', value: '1' }, { name: 'a', value: '2 3' }, { name: 'b', value: null }]
        expect(param(fields)).toBe($.param(fields))
    })

    it('returns an empty string for null', () => {
        expect(param(null)).toBe('')
    })
})

describe('serializeFields', () => {
    it('matches jQuery serializeArray for a search form', () => {
        document.body.innerHTML = `
            <div id="search">
                <input name="SearchProductName" value="shirt">
                <input type="checkbox" name="SearchIncludeSubCategories" value="true" checked>
                <input type="hidden" name="SearchIncludeSubCategories" value="false">
                <input type="checkbox" name="Unchecked" value="true">
                <input name="Disabled" value="x" disabled>
                <input type="submit" name="go" value="Go">
                <input type="file" name="upload">
                <select name="SearchStoreId"><option value="">All</option><option value="s1" selected>S1</option></select>
                <select name="Multi" multiple><option value="1" selected>1</option><option value="2" selected>2</option></select>
                <textarea name="Notes">line1
line2</textarea>
                <input value="no name">
            </div>`
        const container = document.getElementById('search')
        expect(serializeFields(container)).toEqual($(container).find('input, select, textarea').serializeArray())
    })
})
