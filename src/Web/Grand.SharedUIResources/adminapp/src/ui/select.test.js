import { describe, it, expect, vi, afterEach } from 'vitest'
import { fetchOptions, remote, renderItem } from './select.js'

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

describe('a list read from the server', () => {
    afterEach(() => { vi.useRealTimers() })

    const settingsFor = rowsByQuery => remote({
        url: '/Search/Category',
        fetchJson: url => {
            const match = /value%5D=([^&]*)/.exec(url)
            const query = match ? decodeURIComponent(match[1]) : ''
            return Promise.resolve({ Data: rowsByQuery[query] ?? [] })
        }
    })

    const load = async (settings, query) => {
        vi.useFakeTimers()
        const answer = new Promise(resolve => settings.load(query, resolve))
        await vi.advanceTimersByTimeAsync(300)
        vi.useRealTimers()
        return answer
    }

    it('shows the answer to the query and not the rows of the wider list', async () => {
        const settings = settingsFor({
            '': [{ Id: '1', Name: 'Computers' }, { Id: '2', Name: 'Sport' }],
            'Sp': [{ Id: '2', Name: 'Sport' }]
        })
        await load(settings, '')
        await load(settings, 'Sp')

        const score = settings.score('Sp')
        expect(score({ value: '2' })).toBe(1)
        expect(score({ value: '1' })).toBe(0)
    })

    it('shows the unfiltered list again when the query is cleared', async () => {
        const settings = settingsFor({
            '': [{ Id: '1', Name: 'Computers' }, { Id: '2', Name: 'Sport' }],
            'Sp': [{ Id: '2', Name: 'Sport' }]
        })
        await load(settings, '')
        await load(settings, 'Sp')

        //Tom Select does not ask again for a query it has already loaded, so what the
        //server answered has to keep deciding the list after the box is emptied
        const score = settings.score('')
        expect(score({ value: '1' })).toBe(1)
        expect(score({ value: '2' })).toBe(1)
    })

    it('shows what is there while the answer is still on its way', () => {
        const settings = settingsFor({})
        const score = settings.score('anything')
        expect(score({ value: 'whatever' })).toBe(1)
    })

    it('keeps only the newest query when they are typed one after another', async () => {
        const seen = []
        const answers = []
        const settings = settingsFor({
            'a': [{ Id: 'old', Name: 'a' }],
            'ab': [{ Id: 'new', Name: 'ab' }]
        })
        const spied = { ...settings }
        vi.useFakeTimers()
        spied.load('a', rows => { seen.push('a'); answers.push(rows) })
        spied.load('ab', rows => { seen.push('ab'); answers.push(rows) })
        await vi.advanceTimersByTimeAsync(1000)
        vi.useRealTimers()

        //the debounce drops the request for "a" before it is sent, and the guard in load
        //means that even an answer that arrives late cannot put its rows back
        expect(seen).toEqual(['ab'])
        expect(answers[0]).toEqual([{ value: 'new', text: 'ab' }])
        expect(spied.score('ab')({ value: 'new' })).toBe(1)
        expect(spied.score('ab')({ value: 'old' })).toBe(0)
    })
})

describe('the chosen value in the field', () => {
    const escape = text => String(text).replace(/&/g, '&amp;').replace(/</g, '&lt;')

    it('draws nothing for the empty choice, so the placeholder and the caret have the field', () => {
        expect(renderItem({ value: '', text: 'Select category...' }, escape)).toBe('<div></div>')
    })

    it('draws a real choice', () => {
        expect(renderItem({ value: '7', text: 'Computers' }, escape)).toBe('<div>Computers</div>')
    })

    it('escapes what it draws', () => {
        expect(renderItem({ value: '7', text: '<img src=x>' }, escape)).toBe('<div>&lt;img src=x></div>')
    })
})
