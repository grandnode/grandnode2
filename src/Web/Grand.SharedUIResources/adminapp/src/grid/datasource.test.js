// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { DataSource } from './datasource.js'
import { param } from './param.js'
import { postForm } from './transport.js'

function fakePost(responses) {
    const calls = []
    const post = vi.fn(async (url, data) => {
        calls.push({ url, data, body: param(data) })
        const next = responses.shift()
        if (next instanceof Error) throw { errorThrown: next.message, status: 500 }
        return typeof next === 'function' ? next(url, data) : next
    })
    post.calls = calls
    return post
}

beforeEach(() => {
    document.body.innerHTML = '<input name="__RequestVerificationToken" type="hidden" value="token-1">'
    window.alert = vi.fn()
    delete window.display_kendoui_grid_error
})

afterEach(() => {
    vi.restoreAllMocks()
})

describe('DataSource read', () => {
    it('posts take, skip, page, pageSize, additional data and the antiforgery token', async () => {
        const post = fakePost([{ Data: [{ Id: '1' }], Total: 31 }])
        const ds = new DataSource({
            transport: { read: '/Admin/Product/ProductList' },
            pageSize: 15,
            additionalData: () => ({ SearchProductName: 'shirt', SearchIncludeSubCategories: false }),
            post
        })
        ds._page = 2
        await ds.read()
        expect(post.calls[0].url).toBe('/Admin/Product/ProductList')
        expect(post.calls[0].data).toEqual({
            SearchProductName: 'shirt', SearchIncludeSubCategories: false,
            take: 15, skip: 15, page: 2, pageSize: 15,
            __RequestVerificationToken: 'token-1'
        })
        expect(ds.data()).toEqual([{ Id: '1' }])
        expect(ds.total()).toBe(31)
        expect(ds.totalPages()).toBe(3)
    })

    it('omits paging fields when no page size is set, like a Kendo grid without pageSize', async () => {
        const post = fakePost([{ Data: [], Total: 0 }])
        const ds = new DataSource({ transport: { read: '/r' }, post })
        await ds.read()
        expect(post.calls[0].body).toBe('__RequestVerificationToken=token-1')
    })

    it('page(n) reads that page and page() returns the current one', async () => {
        const post = fakePost([{ Data: [{ Id: 'a' }], Total: 40 }])
        const ds = new DataSource({ transport: { read: '/r' }, pageSize: 10, post })
        await ds.page(3)
        expect(ds.page()).toBe(3)
        expect(post.calls[0].data.skip).toBe(20)
    })

    it('shows Errors from the server and keeps the previous data', async () => {
        const post = fakePost([{ Data: [{ Id: '1' }], Total: 1 }, { Errors: 'No permission' }])
        const onChange = vi.fn()
        const ds = new DataSource({ transport: { read: '/r' }, post, onChange })
        await ds.read()
        await ds.read()
        expect(window.alert).toHaveBeenCalledWith('No permission')
        expect(ds.data()).toEqual([{ Id: '1' }])
        expect(onChange).toHaveBeenCalledTimes(1)
    })

    it('uses display_kendoui_grid_error when admin.common.js is loaded', async () => {
        window.display_kendoui_grid_error = vi.fn()
        const errors = { Name: { errors: ['Name is required'] } }
        const ds = new DataSource({ transport: { read: '/r' }, post: fakePost([{ Errors: errors }]) })
        await ds.read()
        expect(window.display_kendoui_grid_error).toHaveBeenCalledWith({ errors, type: 'read' })
        expect(window.alert).not.toHaveBeenCalled()
    })

    it('reports failed requests like errorThrown', async () => {
        const ds = new DataSource({ transport: { read: '/r' }, post: fakePost([new Error('Internal Server Error')]) })
        await ds.read()
        expect(window.alert).toHaveBeenCalledWith('Error happened')
    })

    it('drops a response that arrives after a newer read', async () => {
        let releaseFirst
        const post = vi.fn()
            .mockImplementationOnce(() => new Promise(resolve => { releaseFirst = () => resolve({ Data: [{ Id: 'old' }], Total: 1 }) }))
            .mockImplementationOnce(async () => ({ Data: [{ Id: 'new' }], Total: 1 }))
        const ds = new DataSource({ transport: { read: '/r' }, post })
        const first = ds.read()
        await ds.read()
        releaseFirst()
        await first
        expect(ds.data()[0].Id).toBe('new')
    })

    it('moves back when the current page became empty', async () => {
        const post = fakePost([{ Data: [], Total: 20 }, { Data: [{ Id: 'x' }], Total: 20 }])
        const ds = new DataSource({ transport: { read: '/r' }, pageSize: 10, post })
        ds._page = 3
        await ds.read()
        expect(ds.page()).toBe(2)
        expect(post.calls[1].data.page).toBe(2)
    })
})

describe('DataSource items', () => {
    it('get, at, remove, cancelChanges and sync', async () => {
        const post = fakePost([{ Data: [{ Id: 1, Name: 'a' }, { Id: 2, Name: 'b' }], Total: 2 }, '', { Errors: 'fail' }])
        const ds = new DataSource({ transport: { read: '/r', destroy: '/d' }, post })
        await ds.read()
        expect(ds.get('2').Name).toBe('b')
        expect(ds.at(0).Name).toBe('a')

        const b = ds.get(2)
        ds.remove(b)
        expect(ds.total()).toBe(1)
        ds.cancelChanges()
        expect(ds.data().map(i => i.Id)).toEqual([1, 2])

        ds.remove(ds.get(1))
        expect(await ds.sync()).toBe(true)
        expect(post.calls[1]).toMatchObject({ url: '/d', body: 'Id=1&Name=a&__RequestVerificationToken=token-1' })

        ds.remove(ds.get(2))
        expect(await ds.sync()).toBe(false)
        expect(ds.data().map(i => i.Id)).toEqual([2])
    })

    it('save posts the serialized item and reports server errors', async () => {
        const post = fakePost(['', { Errors: { Ratio: { errors: ['Invalid'] } } }])
        const ds = new DataSource({
            transport: { update: '/u' },
            post,
            serializeItem: item => ({ ...item, Ratio: '0,5' })
        })
        expect(await ds.save('update', { Id: 'w1', Ratio: 0.5, Locales: [{ LanguageId: 'l', Name: 'n' }] })).toBe(true)
        expect(post.calls[0].body).toBe('Id=w1&Ratio=0%2C5&Locales%5B0%5D%5BLanguageId%5D=l&Locales%5B0%5D%5BName%5D=n&__RequestVerificationToken=token-1')
        expect(await ds.save('update', { Id: 'w1' })).toBe(false)
        expect(window.alert).toHaveBeenCalledWith('The following errors have occurred:\nInvalid')
    })

    it('builds detail URLs with query parameters', () => {
        expect(DataSource.urlWithParams('/Admin/ShoppingCart/GetCartDetails', { customerId: 'a b&c' })).toBe('/Admin/ShoppingCart/GetCartDetails?customerId=a+b%26c')
        expect(DataSource.urlWithParams('/x?y=1', { id: '2' })).toBe('/x?y=1&id=2')
    })

    it('supports Kendo shim options: url functions, parameterMap and schema', async () => {
        const post = fakePost([{ items: [{ id: 1 }], count: 9 }])
        const ds = new DataSource({
            transport: { read: data => `/r?page=${data.page}` },
            pageSize: 5,
            parameterMap: (data, type) => ({ ...data, type }),
            schema: { data: 'items', total: r => r.count },
            post
        })
        await ds.read()
        expect(post.calls[0].url).toBe('/r?page=1')
        expect(post.calls[0].data.type).toBe('read')
        expect(ds.total()).toBe(9)
    })
})

describe('postForm', () => {
    it('sends a form-urlencoded POST with the jQuery headers and toggles the page loader', async () => {
        window.StartPageLoading = vi.fn()
        window.StopPageLoading = vi.fn()
        const fetchImpl = vi.fn(async () => ({ ok: true, status: 200, text: async () => '{"Data":[],"Total":0}' }))
        const result = await postForm('/r', { a: 1, b: [1, 2] }, { fetchImpl })
        expect(result).toEqual({ Data: [], Total: 0 })
        const [url, init] = fetchImpl.mock.calls[0]
        expect(url).toBe('/r')
        expect(init.method).toBe('POST')
        expect(init.body).toBe('a=1&b%5B%5D=1&b%5B%5D=2')
        expect(init.headers['Content-Type']).toBe('application/x-www-form-urlencoded; charset=UTF-8')
        expect(init.headers['X-Requested-With']).toBe('XMLHttpRequest')
        expect(window.StartPageLoading).toHaveBeenCalledTimes(1)
        expect(window.StopPageLoading).toHaveBeenCalledTimes(1)
    })

    it('treats an empty body (return new JsonResult("")) as success and rejects HTTP errors', async () => {
        expect(await postForm('/u', {}, { fetchImpl: async () => ({ ok: true, status: 200, text: async () => '""' }), loader: false })).toBe('')
        await expect(postForm('/u', {}, { fetchImpl: async () => ({ ok: false, status: 400, statusText: 'Bad Request' }), loader: false }))
            .rejects.toEqual({ errorThrown: 'Bad Request', status: 400 })
    })
})

describe('DataSource client paging (serverPaging: false)', () => {
    it('reads once without paging fields and cuts the pages locally', async () => {
        const rows = Array.from({ length: 5 }, (_, i) => ({ Id: String(i + 1) }))
        const post = fakePost([{ Data: rows, Total: 5 }, { Data: rows.slice(0, 4), Total: 4 }])
        const changes = vi.fn()
        const ds = new DataSource({ transport: { read: '/list' }, pageSize: 2, serverPaging: false, onChange: changes, post })
        await ds.read()
        expect(post.calls[0].body).toBe('__RequestVerificationToken=token-1')
        expect(ds.data().map(r => r.Id)).toEqual(['1', '2'])
        expect(ds.total()).toBe(5)
        expect(ds.totalPages()).toBe(3)

        await ds.page(3)
        expect(post).toHaveBeenCalledTimes(1)
        expect(ds.data().map(r => r.Id)).toEqual(['5'])

        ds.remove(ds.data()[0])
        expect(ds.total()).toBe(4)
        await ds.read()
        expect(ds.page()).toBe(2)
        expect(ds.data().map(r => r.Id)).toEqual(['3', '4'])
        expect(changes).toHaveBeenCalled()
    })
})
