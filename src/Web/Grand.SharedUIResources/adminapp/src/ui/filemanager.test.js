// @vitest-environment jsdom
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { FileManager, formatSize } from './filemanager.js'

const urls = { list: '/fm/list', upload: '/fm/upload', createFolder: '/fm/folder', rename: '/fm/rename', delete: '/fm/delete' }

const folders = {
    '': {
        success: true, path: '', crumbs: [{ name: '', path: '' }],
        entries: [
            { name: 'banners', path: 'banners', isFolder: true, kind: 'folder', size: 0 },
            { name: 'logo.png', path: 'logo.png', isFolder: false, kind: 'image', url: '/assets/images/uploaded/logo.png', size: 2048 },
            { name: 'clip.mp4', path: 'clip.mp4', isFolder: false, kind: 'video', url: '/assets/images/uploaded/clip.mp4', size: 10 }
        ]
    },
    banners: {
        success: true, path: 'banners', crumbs: [{ name: '', path: '' }, { name: 'banners', path: 'banners' }],
        entries: [{ name: 'top.jpg', path: 'banners/top.jpg', isFolder: false, kind: 'image', url: '/assets/images/uploaded/banners/top.jpg', size: 1 }]
    }
}

function server(overrides = {}) {
    const calls = []
    const fetch = vi.fn(async (url, init) => {
        const [path, query] = url.split('?')
        const params = init.method === 'GET' ? Object.fromEntries(new URLSearchParams(query)) : Object.fromEntries(init.body.entries())
        calls.push({ path, method: init.method, params, headers: init.headers })
        const answer = overrides[path] ? overrides[path](params) : path === urls.list ? folders[params.path || ''] : { success: true }
        return { json: async () => answer }
    })
    return { fetch, calls }
}

const flush = () => new Promise(resolve => setTimeout(resolve, 0))
const items = () => Array.from(document.querySelectorAll('.grand-filemanager__item'))
const byName = name => items().find(item => item.querySelector('.grand-filemanager__label').textContent === name)
const click = (selector, root = document) => root.querySelector(selector).dispatchEvent(new MouseEvent('click', { bubbles: true }))

describe('fileManager', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
    })

    it('lists folders and files of the root, a picture with its own thumbnail', async () => {
        const { fetch } = server()
        await new FileManager({ urls, fetch }).open()
        expect(items().map(i => i.querySelector('.grand-filemanager__label').textContent)).toEqual(['banners', 'logo.png', 'clip.mp4'])
        expect(byName('logo.png').querySelector('img').getAttribute('src')).toBe('/assets/images/uploaded/logo.png')
        expect(byName('banners').querySelector('.bi-folder-fill')).not.toBeNull()
        expect(byName('logo.png').querySelector('.grand-filemanager__size').textContent).toBe('2 KB')
    })

    it('opens a folder on double click and goes back through the breadcrumb', async () => {
        const { fetch } = server()
        const manager = await new FileManager({ urls, fetch }).open()
        byName('banners').dispatchEvent(new MouseEvent('dblclick', { bubbles: true }))
        await flush()
        expect(manager.path).toBe('banners')
        expect(items().map(i => i.dataset.path)).toEqual(['banners/top.jpg'])
        click('.grand-filemanager__crumb[data-action="crumb"]')
        await flush()
        expect(manager.path).toBe('')
    })

    it('picks a selected file with Insert and closes', async () => {
        const { fetch } = server()
        const onPick = vi.fn()
        await new FileManager({ urls, fetch, onPick }).open()
        const insert = document.querySelector('[data-action="insert"]')
        expect(insert.disabled).toBe(true)
        byName('logo.png').dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(insert.disabled).toBe(false)
        click('[data-action="insert"]')
        await flush()
        expect(onPick).toHaveBeenCalledWith(expect.objectContaining({ url: '/assets/images/uploaded/logo.png', kind: 'image' }))
        expect(document.querySelector('.grand-modal')).toBeNull()
    })

    it('cannot insert a folder', async () => {
        const { fetch } = server()
        await new FileManager({ urls, fetch }).open()
        byName('banners').dispatchEvent(new MouseEvent('click', { bubbles: true }))
        expect(document.querySelector('[data-action="insert"]').disabled).toBe(true)
    })

    it('posts with the antiforgery token and the folder shown', async () => {
        const { fetch, calls } = server()
        const manager = await new FileManager({ urls, fetch, token: 'tok' }).open()
        manager.askName('folder', '')
        document.querySelector('.grand-filemanager__name input').value = 'new'
        document.querySelector('.grand-filemanager__name').dispatchEvent(new Event('submit', { cancelable: true }))
        await flush()
        const post = calls.find(c => c.path === urls.createFolder)
        expect(post.method).toBe('POST')
        expect(post.headers['X-CSRF-TOKEN']).toBe('tok')
        expect(post.params).toEqual({ path: '', name: 'new' })
    })

    it('renames the selected entry under its own path', async () => {
        const { fetch, calls } = server()
        await new FileManager({ urls, fetch }).open()
        byName('logo.png').dispatchEvent(new MouseEvent('click', { bubbles: true }))
        click('[data-action="rename"]')
        const input = document.querySelector('.grand-filemanager__name input')
        expect(input.value).toBe('logo.png')
        input.value = 'brand.png'
        document.querySelector('.grand-filemanager__name').dispatchEvent(new Event('submit', { cancelable: true }))
        await flush()
        expect(calls.find(c => c.path === urls.rename).params).toEqual({ path: 'logo.png', name: 'brand.png' })
    })

    it('deletes only after the confirmation says yes', async () => {
        const { fetch, calls } = server()
        const confirm = vi.fn().mockResolvedValueOnce(false).mockResolvedValueOnce(true)
        await new FileManager({ urls, fetch, confirm }).open()
        byName('banners').dispatchEvent(new MouseEvent('click', { bubbles: true }))
        click('[data-action="delete"]')
        await flush()
        expect(calls.some(c => c.path === urls.delete)).toBe(false)
        click('[data-action="delete"]')
        await flush()
        expect(confirm.mock.calls[1][0]).toContain('banners')
        expect(calls.find(c => c.path === urls.delete).params).toEqual({ path: 'banners' })
    })

    it('uploads into the folder shown and reports the rejected files', async () => {
        const { fetch, calls } = server({
            [urls.upload]: () => ({ success: true, uploaded: [], rejected: [{ name: 'run.exe', message: 'This file type is not allowed' }] })
        })
        const manager = await new FileManager({ urls, fetch }).open()
        await manager.upload([new File(['x'], 'run.exe')])
        const post = calls.find(c => c.path === urls.upload)
        expect(post.params.path).toBe('')
        expect(post.params.files.name).toBe('run.exe')
        expect(document.querySelector('.grand-filemanager__message').textContent).toBe('run.exe: This file type is not allowed')
    })

    it('shows the server message of a refused request', async () => {
        const { fetch } = server({ [urls.list]: () => ({ success: false, message: 'Access denied' }) })
        await new FileManager({ urls, fetch }).open()
        const message = document.querySelector('.grand-filemanager__message')
        expect(message.hidden).toBe(false)
        expect(message.textContent).toBe('Access denied')
    })

    it('writes a file name as text, never as markup', async () => {
        const evil = '<img src=x onerror=alert(1)>.png'
        const { fetch } = server({
            [urls.list]: () => ({ success: true, path: '', crumbs: [], entries: [{ name: evil, path: evil, isFolder: false, kind: 'file', size: 1 }] })
        })
        await new FileManager({ urls, fetch }).open()
        const label = document.querySelector('.grand-filemanager__label')
        expect(label.textContent).toBe(evil)
        expect(label.querySelector('img')).toBeNull()
    })

    it('removes its window when closed with Escape', async () => {
        const { fetch } = server()
        await new FileManager({ urls, fetch }).open()
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
        await flush()
        expect(document.querySelector('.grand-modal')).toBeNull()
    })
})

describe('formatSize', () => {
    it('writes bytes, kilobytes and megabytes', () => {
        expect(formatSize(512)).toBe('512 B')
        expect(formatSize(1536)).toBe('1.5 KB')
        expect(formatSize(4 * 1024 * 1024)).toBe('4 MB')
    })
})
