/*
 * $bvToast.toast() - the toast helper the view scripts still call, rendered
 * with plain Bootstrap 5 toast markup.
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

