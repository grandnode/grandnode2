//GrandAdmin.fileManager - the file manager of the rich text editor ("Manage pictures"), in place
//of elFinder. It lists the folders and files under assets/images/uploaded through the Admin
//FileManager controller, uploads, creates folders, renames and deletes, and hands the file the
//user picks to onPick. Every name is written with textContent: a file name is never markup.
//
//  GrandAdmin.fileManager.open({
//      urls: { list, upload, createFolder, rename, delete },
//      token,                       //antiforgery request token, sent as X-CSRF-TOKEN
//      texts: { title, up, newFolder, upload, rename, delete, insert, cancel, ok, empty, root,
//               folderName, newName, deleteConfirm, deleteFolderConfirm, uploading, failed },
//      onPick(entry)                //{ name, path, url, kind: 'image' | 'video' | 'file' }
//  })

import { getModal, confirmModal } from './modal.js'

const DEFAULT_TEXTS = {
    title: 'Files',
    up: 'Up',
    newFolder: 'New folder',
    upload: 'Upload',
    rename: 'Rename',
    delete: 'Delete',
    insert: 'Insert',
    cancel: 'Cancel',
    ok: 'OK',
    empty: 'This folder is empty',
    root: 'Files',
    folderName: 'Folder name',
    newName: 'New name',
    deleteConfirm: 'Delete {0}?',
    deleteFolderConfirm: 'Delete the folder {0} and everything in it?',
    uploading: 'Uploading...',
    failed: 'The request failed'
}

function el(doc, tag, className, text) {
    const element = doc.createElement(tag)
    if (className) element.className = className
    if (text !== undefined) element.textContent = text
    return element
}

function button(doc, action, icon, text, variant = 'btn-outline-secondary') {
    const element = el(doc, 'button', `btn ${variant} btn-sm`)
    element.type = 'button'
    element.dataset.action = action
    if (icon) {
        const i = el(doc, 'i', `bi ${icon}`)
        i.setAttribute('aria-hidden', 'true')
        element.appendChild(i)
    }
    if (text) element.appendChild(doc.createTextNode(icon ? ` ${text}` : text))
    return element
}

/** 1536 -> "1.5 KB" */
export function formatSize(bytes) {
    if (!Number.isFinite(bytes) || bytes < 1024) return `${Math.max(0, bytes | 0)} B`
    const units = ['KB', 'MB', 'GB']
    let value = bytes / 1024
    let unit = 0
    while (value >= 1024 && unit < units.length - 1) {
        value /= 1024
        unit++
    }
    return `${value >= 10 ? Math.round(value) : Math.round(value * 10) / 10} ${units[unit]}`
}

function format(template, value) {
    return String(template).replace('{0}', value)
}

export class FileManager {
    constructor(options = {}, doc = globalThis.document) {
        this.doc = doc
        this.options = options
        this.urls = options.urls || {}
        this.texts = { ...DEFAULT_TEXTS, ...(options.texts || {}) }
        this.fetch = options.fetch || ((...args) => globalThis.fetch(...args))
        this.confirm = options.confirm || ((message, o) => confirmModal(message, o, doc))
        this.path = ''
        this.entries = []
        this.selected = null
        this.naming = null
        this.build()
    }

    build() {
        const doc = this.doc
        const t = this.texts
        //.grand-page .grand-popup as <admin-popup> renders: the panel's buttons are styled only inside .grand-page
        this.element = el(doc, 'div', 'grand-filemanager grand-page grand-popup')

        const toolbar = el(doc, 'div', 'grand-filemanager__toolbar')
        this.crumbs = el(doc, 'nav', 'grand-filemanager__crumbs')
        this.crumbs.setAttribute('aria-label', t.root)
        const tools = el(doc, 'div', 'grand-filemanager__tools')
        this.upButton = button(doc, 'up', 'bi-arrow-up', '')
        this.upButton.title = t.up
        this.upButton.setAttribute('aria-label', t.up)
        this.folderButton = button(doc, 'folder', 'bi-folder-plus', t.newFolder)
        this.uploadInput = el(doc, 'input', 'grand-filemanager__file')
        this.uploadInput.type = 'file'
        this.uploadInput.multiple = true
        this.uploadInput.hidden = true
        this.uploadButton = button(doc, 'upload', 'bi-upload', t.upload)
        this.renameButton = button(doc, 'rename', 'bi-pencil', t.rename)
        this.deleteButton = button(doc, 'delete', 'bi-trash', t.delete, 'btn-outline-danger')
        tools.append(this.upButton, this.folderButton, this.uploadButton, this.uploadInput, this.renameButton, this.deleteButton)
        toolbar.append(this.crumbs, tools)

        //the name of a new folder or of a renamed entry, typed in place: no second window
        this.nameBar = el(doc, 'form', 'grand-filemanager__name')
        this.nameBar.hidden = true
        this.nameLabel = el(doc, 'label', 'grand-filemanager__name-label')
        this.nameInput = el(doc, 'input', 'form-control form-control-sm')
        this.nameInput.type = 'text'
        this.nameInput.id = `grand-filemanager-name-${Math.random().toString(36).slice(2)}`
        this.nameLabel.htmlFor = this.nameInput.id
        const nameOk = button(doc, 'name-ok', null, t.ok)
        nameOk.type = 'submit'
        const nameCancel = button(doc, 'name-cancel', null, t.cancel)
        this.nameBar.append(this.nameLabel, this.nameInput, nameOk, nameCancel)

        this.message = el(doc, 'div', 'grand-filemanager__message')
        this.message.setAttribute('role', 'status')
        this.message.hidden = true

        this.list = el(doc, 'ul', 'grand-filemanager__list')
        this.list.setAttribute('role', 'listbox')
        this.list.setAttribute('aria-label', t.title)
        this.emptyElement = el(doc, 'div', 'grand-filemanager__empty', t.empty)
        this.emptyElement.hidden = true

        const footer = el(doc, 'div', 'grand-filemanager__footer')
        this.cancelButton = button(doc, 'cancel', null, t.cancel)
        this.insertButton = button(doc, 'insert', 'bi-check-lg', t.insert, 'btn-primary')
        footer.append(this.cancelButton, this.insertButton)

        this.element.append(toolbar, this.nameBar, this.message, this.list, this.emptyElement, footer)

        this.element.addEventListener('click', e => this.onClick(e))
        //closed by Cancel, the close button, Escape or the overlay: the window goes away for good
        this.element.addEventListener('grand-modal:close', () => {
            const modal = this.modal
            this.modal = null
            if (modal) queueMicrotask(() => modal.destroy())
        })
        this.list.addEventListener('dblclick', e => {
            const item = e.target.closest('[data-path]')
            if (item) this.activate(this.entryOf(item))
        })
        this.list.addEventListener('keydown', e => this.onKeyDown(e))
        this.uploadInput.addEventListener('change', () => {
            const files = Array.from(this.uploadInput.files || [])
            this.uploadInput.value = ''
            if (files.length) this.upload(files)
        })
        this.nameBar.addEventListener('submit', e => {
            e.preventDefault()
            this.submitName()
        })
        //files dropped on the list go into the folder shown
        this.list.addEventListener('dragover', e => {
            if (!e.dataTransfer?.types?.includes?.('Files')) return
            e.preventDefault()
            this.list.classList.add('is-drop-target')
        })
        this.list.addEventListener('dragleave', () => this.list.classList.remove('is-drop-target'))
        this.list.addEventListener('drop', e => {
            this.list.classList.remove('is-drop-target')
            const files = Array.from(e.dataTransfer?.files || [])
            if (!files.length) return
            e.preventDefault()
            this.upload(files)
        })
        this.updateButtons()
    }

    onClick(e) {
        const actionElement = e.target.closest('[data-action]')
        const action = actionElement?.dataset.action
        const item = e.target.closest('.grand-filemanager__item')
        if (item && !action) {
            this.select(this.entryOf(item))
            return
        }
        switch (action) {
            case 'up': return this.navigate(this.parentPath())
            case 'crumb': return this.navigate(actionElement.dataset.path)
            case 'folder': return this.askName('folder', '')
            case 'upload': return this.uploadInput.click()
            case 'rename': return this.selected && this.askName('rename', this.selected.name)
            case 'delete': return this.remove()
            case 'name-cancel': return this.hideName()
            case 'cancel': return this.close()
            case 'insert': return this.selected && this.activate(this.selected)
        }
    }

    onKeyDown(e) {
        const items = Array.from(this.list.querySelectorAll('.grand-filemanager__item'))
        const index = items.indexOf(e.target.closest('.grand-filemanager__item'))
        if (index < 0) return
        let next = -1
        if (e.key === 'ArrowRight' || e.key === 'ArrowDown') next = Math.min(items.length - 1, index + 1)
        else if (e.key === 'ArrowLeft' || e.key === 'ArrowUp') next = Math.max(0, index - 1)
        else if (e.key === 'Enter') {
            e.preventDefault()
            this.activate(this.entryOf(items[index]))
            return
        } else if (e.key === 'Delete') {
            this.select(this.entryOf(items[index]))
            this.remove()
            return
        }
        if (next < 0) return
        e.preventDefault()
        items[next].focus()
        this.select(this.entryOf(items[next]))
    }

    entryOf(item) {
        return this.entries.find(entry => entry.path === item.dataset.path) || null
    }

    parentPath() {
        const parts = this.path.split('/').filter(Boolean)
        parts.pop()
        return parts.join('/')
    }

    /** A folder opens, a file is picked. */
    activate(entry) {
        if (!entry) return
        if (entry.isFolder) {
            this.navigate(entry.path)
            return
        }
        this.close()
        if (typeof this.options.onPick === 'function') this.options.onPick(entry)
    }

    select(entry) {
        this.selected = entry
        for (const item of this.list.querySelectorAll('.grand-filemanager__item')) {
            const on = !!entry && item.dataset.path === entry.path
            item.classList.toggle('is-selected', on)
            item.setAttribute('aria-selected', on ? 'true' : 'false')
        }
        this.updateButtons()
    }

    updateButtons() {
        const entry = this.selected
        this.upButton.disabled = !this.path
        this.renameButton.disabled = !entry
        this.deleteButton.disabled = !entry
        this.insertButton.disabled = !entry || entry.isFolder
    }

    showMessage(text, isError = true) {
        this.message.textContent = text || ''
        this.message.hidden = !text
        this.message.classList.toggle('is-error', !!text && isError)
    }

    async request(url, { method = 'GET', params = {}, body } = {}) {
        let target = url
        const init = { method, credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } }
        if (method === 'GET') {
            const query = new URLSearchParams(params).toString()
            if (query) target += (target.includes('?') ? '&' : '?') + query
        } else {
            const form = body || new FormData()
            for (const [name, value] of Object.entries(params)) form.append(name, value)
            init.body = form
            if (this.options.token) init.headers['X-CSRF-TOKEN'] = this.options.token
        }
        try {
            const response = await this.fetch(target, init)
            const data = await response.json()
            if (!data || data.success !== true) {
                this.showMessage(data?.message || this.texts.failed)
                return null
            }
            return data
        } catch {
            this.showMessage(this.texts.failed)
            return null
        }
    }

    async navigate(path = '') {
        const data = await this.request(this.urls.list, { params: { path } })
        if (!data) return false
        this.path = data.path || ''
        this.entries = Array.isArray(data.entries) ? data.entries : []
        this.selected = null
        this.showMessage('')
        this.hideName()
        this.renderCrumbs(Array.isArray(data.crumbs) ? data.crumbs : [])
        this.renderEntries()
        return true
    }

    renderCrumbs(crumbs) {
        const doc = this.doc
        this.crumbs.replaceChildren()
        crumbs.forEach((crumb, index) => {
            if (index) this.crumbs.appendChild(el(doc, 'span', 'grand-filemanager__crumb-separator', '/'))
            const last = index === crumbs.length - 1
            const part = el(doc, last ? 'span' : 'button', 'grand-filemanager__crumb', crumb.name || this.texts.root)
            if (!last) {
                part.type = 'button'
                part.dataset.action = 'crumb'
                part.dataset.path = crumb.path || ''
            } else part.setAttribute('aria-current', 'location')
            this.crumbs.appendChild(part)
        })
    }

    renderEntries() {
        const doc = this.doc
        this.list.replaceChildren()
        for (const entry of this.entries) {
            const item = el(doc, 'li', 'grand-filemanager__item')
            item.dataset.path = entry.path
            item.tabIndex = 0
            item.setAttribute('role', 'option')
            item.setAttribute('aria-selected', 'false')
            item.title = entry.name

            const thumb = el(doc, 'div', 'grand-filemanager__thumb')
            if (entry.kind === 'image' && entry.url) {
                const img = el(doc, 'img')
                img.src = entry.url
                img.alt = ''
                img.loading = 'lazy'
                thumb.appendChild(img)
            } else {
                const icon = entry.isFolder ? 'bi-folder-fill' : entry.kind === 'video' ? 'bi-file-earmark-play' : 'bi-file-earmark'
                const i = el(doc, 'i', `bi ${icon}`)
                i.setAttribute('aria-hidden', 'true')
                thumb.appendChild(i)
            }
            item.append(thumb, el(doc, 'span', 'grand-filemanager__label', entry.name))
            if (!entry.isFolder) item.appendChild(el(doc, 'span', 'grand-filemanager__size', formatSize(entry.size)))
            this.list.appendChild(item)
        }
        this.emptyElement.hidden = this.entries.length > 0
        this.updateButtons()
    }

    askName(mode, value) {
        this.naming = mode
        this.nameLabel.textContent = mode === 'folder' ? this.texts.folderName : this.texts.newName
        this.nameInput.value = value
        this.nameBar.hidden = false
        this.nameInput.focus()
        //a file's name is chosen without its extension, which a rename usually keeps
        const dot = mode === 'rename' && !this.selected?.isFolder ? value.lastIndexOf('.') : -1
        if (typeof this.nameInput.setSelectionRange === 'function')
            this.nameInput.setSelectionRange(0, dot > 0 ? dot : value.length)
    }

    hideName() {
        this.naming = null
        this.nameBar.hidden = true
    }

    async submitName() {
        const name = this.nameInput.value.trim()
        if (!name) return
        const renaming = this.naming === 'rename' && this.selected
        const data = renaming
            ? await this.request(this.urls.rename, { method: 'POST', params: { path: this.selected.path, name } })
            : await this.request(this.urls.createFolder, { method: 'POST', params: { path: this.path, name } })
        if (!data) return
        await this.navigate(this.path)
        const entry = data.entry && this.entries.find(e => e.path === data.entry.path)
        if (entry) this.select(entry)
    }

    async remove() {
        const entry = this.selected
        if (!entry) return
        const question = format(entry.isFolder ? this.texts.deleteFolderConfirm : this.texts.deleteConfirm, entry.name)
        const yes = await this.confirm(question, { title: this.texts.delete, confirmText: this.texts.delete, cancelText: this.texts.cancel })
        if (!yes) return
        if (await this.request(this.urls.delete, { method: 'POST', params: { path: entry.path } }))
            await this.navigate(this.path)
    }

    async upload(files) {
        const form = new FormData()
        for (const file of files) form.append('files', file, file.name)
        this.showMessage(this.texts.uploading, false)
        const data = await this.request(this.urls.upload, { method: 'POST', params: { path: this.path }, body: form })
        if (!data) return
        await this.navigate(this.path)
        const rejected = Array.isArray(data.rejected) ? data.rejected : []
        if (rejected.length) this.showMessage(rejected.map(r => `${r.name}: ${r.message}`).join('\n'))
        const last = Array.isArray(data.uploaded) && data.uploaded.length ? data.uploaded[data.uploaded.length - 1] : null
        const entry = last && this.entries.find(e => e.path === last.path)
        if (entry) this.select(entry)
    }

    async open() {
        this.modal = this.modal || getModal(this.element, { title: this.texts.title, width: 860 }, this.doc)
        this.modal.open()
        await this.navigate(this.path)
        return this
    }

    close() {
        this.modal?.close()
    }
}

/** Opens a new file manager; each editor button opens its own. */
export function openFileManager(options = {}, doc = globalThis.document) {
    const manager = new FileManager(options, doc)
    manager.open()
    return manager
}

export const fileManager = { open: openFileManager }
