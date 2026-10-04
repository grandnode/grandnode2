//GrandAdmin.modal - the replacement for the Kendo Window the panels used for delete
//confirmations, picture editors and the "add product" popups.
//
//Kendo moved the element it was created on into a wrapper appended to <body> and only
//hid that wrapper on close; the panels rely on it (the delete confirmation sits inside
//the page form, and posting it as its own form is what the partial expects). This does
//the same: the element is moved once into the modal body and stays there.

const instances = new WeakMap()

//Bootstrap's own modal classes on an element this widget adopts. `.modal-dialog` carries
//`pointer-events: none` - Bootstrap gives the clickability back on `.modal-content`, which
//these windows never had - so an adopted element keeping the class swallowed every click
//inside it: the file field of an import window could not be opened at all. This widget
//draws its own header, dialog and body, so none of these names has anything left to say
//here; they are dropped when the element is adopted, once, for every window.
const BOOTSTRAP_MODAL_CLASSES = ['modal', 'modal-dialog', 'modal-content', 'modal-dialog-centered', 'modal-dialog-scrollable', 'fade']

function resolve(target, doc) {
    if (!target) return null
    if (typeof target === 'string') return doc.querySelector(target.startsWith('#') || target.startsWith('.') ? target : `#${target}`)
    //a jQuery object, a NodeList or an element
    if (target.nodeType === 1) return target
    return target[0] && target[0].nodeType === 1 ? target[0] : null
}

class Modal {
    constructor(element, options = {}) {
        const doc = element.ownerDocument
        this.doc = doc
        this.element = element
        this.options = options
        this.isOpen = false

        this.overlay = doc.createElement('div')
        this.overlay.className = 'grand-modal'
        this.overlay.hidden = true
        this.overlay.setAttribute('role', 'dialog')
        this.overlay.setAttribute('aria-modal', 'true')

        this.dialog = doc.createElement('div')
        this.dialog.className = 'grand-modal-dialog'
        if (options.width) this.dialog.style.maxWidth = typeof options.width === 'number' ? `${options.width}px` : options.width

        this.header = doc.createElement('div')
        this.header.className = 'grand-modal-header'
        this.titleElement = doc.createElement('h5')
        this.titleElement.className = 'grand-modal-title'
        this.header.appendChild(this.titleElement)

        this.closeButton = doc.createElement('button')
        this.closeButton.type = 'button'
        this.closeButton.className = 'grand-modal-close'
        this.closeButton.innerHTML = '&times;'
        this.closeButton.addEventListener('click', () => this.close())
        this.header.appendChild(this.closeButton)

        this.body = doc.createElement('div')
        this.body.className = 'grand-modal-body'

        this.dialog.appendChild(this.header)
        this.dialog.appendChild(this.body)
        this.overlay.appendChild(this.dialog)
        doc.body.appendChild(this.overlay)

        //the element carried style="display:none" while it waited on the page
        element.style.display = ''
        element.classList.remove(...BOOTSTRAP_MODAL_CLASSES)
        this.body.appendChild(element)

        this.overlay.addEventListener('mousedown', e => {
            if (e.target === this.overlay && this.options.closeOnOverlay !== false) this.close()
        })
        this.onKeyDown = e => {
            if (e.key === 'Escape' && this.isOpen) this.close()
        }

        this.title(options.title)
        this.actions(options.actions)
    }

    title(text) {
        if (text === undefined) return this.titleElement.textContent
        this.titleElement.textContent = text == null ? '' : String(text)
        return this
    }

    /** Kendo actions: only ['Close'] was ever used; anything else keeps the close button. */
    actions(actions) {
        if (Array.isArray(actions)) {
            const close = actions.some(action => String(action).toLowerCase() === 'close')
            this.closeButton.hidden = !close && actions.length > 0
        }
        return this
    }

    /** Kendo center() - the dialog is centred by the stylesheet, so this only chains. */
    center() {
        return this
    }

    open() {
        if (this.isOpen) return this
        this.isOpen = true
        this.overlay.hidden = false
        this.doc.body.classList.add('grand-modal-open')
        this.doc.addEventListener('keydown', this.onKeyDown)
        const focusable = this.element.querySelector('input:not([type=hidden]), select, textarea, button, [href]')
        if (focusable && typeof focusable.focus === 'function') focusable.focus()
        this.element.dispatchEvent(new CustomEvent('grand-modal:open', { bubbles: true }))
        return this
    }

    close() {
        if (!this.isOpen) return this
        this.isOpen = false
        this.overlay.hidden = true
        this.doc.removeEventListener('keydown', this.onKeyDown)
        if (!this.doc.querySelector('.grand-modal:not([hidden])')) this.doc.body.classList.remove('grand-modal-open')
        this.element.dispatchEvent(new CustomEvent('grand-modal:close', { bubbles: true }))
        return this
    }

    destroy() {
        this.close()
        instances.delete(this.element)
        this.overlay.remove()
    }
}

/** The modal of an element, created on first use. Returns null when the element is missing. */
export function getModal(target, options = {}, doc = globalThis.document) {
    const element = resolve(target, doc)
    if (!element) return null
    let modal = instances.get(element)
    if (!modal) {
        modal = new Modal(element, options)
        instances.set(element, modal)
    } else {
        if (options.title !== undefined) modal.title(options.title)
        if (options.actions !== undefined) modal.actions(options.actions)
    }
    return modal
}

/** Opens the modal of an element (creating it), like kendoWindow(...).center().open(). */
export function openModal(target, options = {}, doc = globalThis.document) {
    return getModal(target, options, doc)?.open() ?? null
}

/** Closes the modal of an element; does nothing when it was never opened. */
export function closeModal(target, doc = globalThis.document) {
    const element = resolve(target, doc)
    const modal = element ? instances.get(element) : null
    return modal ? modal.close() : null
}

/**
 * The confirmation a destructive action asks for, in a modal of the panel rather than the
 * browser's own confirm() - which a screen cannot style, which blocks the page, and which the
 * panels still used for "delete selected". Resolves true when confirmed, false otherwise.
 */
export function confirmModal(message, options = {}, doc = globalThis.document) {
    const element = doc.createElement('div')
    element.className = 'grand-confirm'
    const text = doc.createElement('p')
    text.className = 'grand-confirm__message'
    text.textContent = message == null ? '' : String(message)
    const buttons = doc.createElement('div')
    buttons.className = 'grand-confirm__actions'
    const confirm = doc.createElement('button')
    confirm.type = 'button'
    confirm.className = 'btn btn-danger'
    confirm.textContent = options.confirmText || 'OK'
    const cancel = doc.createElement('button')
    cancel.type = 'button'
    cancel.className = 'btn btn-outline-secondary'
    cancel.textContent = options.cancelText || 'Cancel'
    buttons.appendChild(cancel)
    buttons.appendChild(confirm)
    element.appendChild(text)
    element.appendChild(buttons)
    doc.body.appendChild(element)

    const widget = getModal(element, { title: options.title || '' }, doc)
    return new Promise(resolve => {
        let answered = false
        const settle = answer => {
            if (answered) return
            answered = true
            resolve(answer)
            widget.destroy()
            element.remove()
        }
        confirm.addEventListener('click', () => settle(true))
        cancel.addEventListener('click', () => settle(false))
        //the close button, the overlay and Escape all mean "no"
        element.addEventListener('grand-modal:close', () => settle(false))
        widget.open()
    })
}

export const modal = {
    get: getModal,
    open: openModal,
    close: closeModal,
    confirm: confirmModal
}
