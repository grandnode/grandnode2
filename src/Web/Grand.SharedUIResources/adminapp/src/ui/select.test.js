import { describe, it, expect, vi } from 'vitest'
import { fetchOptions } from './select.js'

describe('fetchOptions', () => {
    it('reads the Data rows of a DataSourceResult', async () => {
        const fetchJson = vi.fn().mockResolvedValue({ Data: [{ Id: '1', Name: 'One' }], Total: 1 })
        await expect(fetchOptions('/Search/Brand', '', { fetchJson })).resolves.toEqual([{ value: '1', text: 'One' }])
        expect(fetchJson).toHaveBeenCalledWith('/Search/Brand')
    })

    it('reads a plain array too', async () => {
        const fetchJson = vi.fn().mockResolvedValue([{ Id: 'a', Name: 'A' }])
        await expect(fetchOptions('/x', '', { fetchJson })).resolves.toEqual([{ value: 'a', text: 'A' }])
    })

    it('sends the filter query DataSourceRequestFilterBinder parses', async () => {
        const fetchJson = vi.fn().mockResolvedValue({ Data: [] })
        await fetchOptions('/Search/Vendor?vendorId=7', 'ab', { fetchJson })
        const url = fetchJson.mock.calls[0][0]
        expect(url.startsWith('/Search/Vendor?vendorId=7&')).toBe(true)
        expect(url).toContain('filter%5Blogic%5D=and')
        expect(url).toContain('filter%5Bfilters%5D%5B0%5D%5Bfield%5D=Name')
        expect(url).toContain('filter%5Bfilters%5D%5B0%5D%5Boperator%5D=startswith')
        expect(url).toContain('filter%5Bfilters%5D%5B0%5D%5Bvalue%5D=ab')
    })

    it('answers an empty list for a body that carries none', async () => {
        const fetchJson = vi.fn().mockResolvedValue(null)
        await expect(fetchOptions('/x', '', { fetchJson })).resolves.toEqual([])
    })
})
