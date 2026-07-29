/*
 * BootstrapVue-compatible runtime services rebuilt for Bootstrap 5:
 *  - $bvToast.toast()            (plain-DOM Bootstrap 5 toasts)
 *  - $bvModal.show()/hide()      (registry of <b-modal> instances by id)
 *  - toggle registry             (<b-collapse>/<b-sidebar> for v-b-toggle)
 *  - tooltip engine              (shared by v-b-tooltip and <b-tooltip>)
 */

/* ---------------------------------- toasts -------------------------------- */

let toasterEl = null

function toaster() {
    if (!toasterEl) {
        toasterEl = document.createElement('div')
        toasterEl.className = 'toast-container position-fixed top-0 end-0 p-3'
        toasterEl.style.zIndex = '1090'
        document.body.appendChild(toasterEl)
    }
    return toasterEl
}

export const $bvToast = {
    toast(message, options = {}) {
        const variant = options.variant || 'info'
        const el = document.createElement('div')
        el.className = 'toast show align-items-center border-' + variant
        el.setAttribute('role', 'alert')

        const header = document.createElement('div')
        header.className = 'toast-header text-bg-' + variant
        const title = document.createElement('strong')
        title.className = 'me-auto'
        title.innerText = options.title || ''
        const closeBtn = document.createElement('button')
        closeBtn.type = 'button'
        closeBtn.className = 'btn-close btn-close-white ms-2'
        closeBtn.addEventListener('click', () => el.remove())
        header.appendChild(title)
        header.appendChild(closeBtn)
        el.appendChild(header)

        const body = document.createElement('div')
        body.className = 'toast-body'
        if (options.href) {
            const link = document.createElement('a')
            link.href = options.href
            link.innerText = message
            body.appendChild(link)
        } else {
            body.innerText = message
        }
        el.appendChild(body)

        toaster().appendChild(el)
        const delay = options.autoHideDelay || 5000
        if (!options.noAutoHide) {
            setTimeout(() => el.remove(), delay)
        }
    }
}

/* ---------------------------------- modals -------------------------------- */

const modalRegistry = new Map()

export function registerModal(id, instance) {
    if (id) modalRegistry.set(id, instance)
}

export function unregisterModal(id, instance) {
    if (id && modalRegistry.get(id) === instance) modalRegistry.delete(id)
}

export const $bvModal = {
    show(id) {
        const m = modalRegistry.get(id)
        if (m) m.show()
    },
    hide(id) {
        const m = modalRegistry.get(id)
        if (m) m.hide()
    }
}

/* ------------------------- collapse / sidebar toggling --------------------- */

const toggleRegistry = new Map()
const toggleListeners = new Map()

export function registerToggle(id, instance) {
    if (!id) return
    toggleRegistry.set(id, instance)
    notifyToggleState(id, instance.isShown())
}

export function unregisterToggle(id, instance) {
    if (id && toggleRegistry.get(id) === instance) toggleRegistry.delete(id)
}

export function toggleTarget(id) {
    const t = toggleRegistry.get(id)
    if (t) t.toggle()
}

export function notifyToggleState(id, shown) {
    const listeners = toggleListeners.get(id)
    if (listeners) listeners.forEach(fn => fn(shown))
}

export function onToggleState(id, fn) {
    if (!toggleListeners.has(id)) toggleListeners.set(id, new Set())
    toggleListeners.get(id).add(fn)
    const t = toggleRegistry.get(id)
    if (t) fn(t.isShown())
}

/* --------------------------------- tooltips -------------------------------- */

let tipEl = null

export function showTooltip(target, content, placement = 'top', html = false) {
    hideTooltip()
    if (!content) return
    tipEl = document.createElement('div')
    tipEl.className = 'tooltip show bs-tooltip-' + placement
    tipEl.setAttribute('role', 'tooltip')
    const inner = document.createElement('div')
    inner.className = 'tooltip-inner'
    if (html) inner.innerHTML = content
    else inner.innerText = content
    tipEl.appendChild(inner)
    tipEl.style.position = 'fixed'
    tipEl.style.zIndex = '1080'
    document.body.appendChild(tipEl)

    const rect = target.getBoundingClientRect()
    const tip = tipEl.getBoundingClientRect()
    let top, left
    switch (placement) {
        case 'bottom':
            top = rect.bottom + 6
            left = rect.left + rect.width / 2 - tip.width / 2
            break
        case 'right':
            top = rect.top + rect.height / 2 - tip.height / 2
            left = rect.right + 6
            break
        case 'left':
            top = rect.top + rect.height / 2 - tip.height / 2
            left = rect.left - tip.width - 6
            break
        default:
            top = rect.top - tip.height - 6
            left = rect.left + rect.width / 2 - tip.width / 2
    }
    tipEl.style.top = Math.max(2, top) + 'px'
    tipEl.style.left = Math.max(2, left) + 'px'
}

export function hideTooltip() {
    if (tipEl) {
        tipEl.remove()
        tipEl = null
    }
}
