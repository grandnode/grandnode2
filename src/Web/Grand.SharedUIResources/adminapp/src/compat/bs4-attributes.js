//Bootstrap 4 data attributes, renamed to data-bs-* in Bootstrap 5.
//
//The views of this repository are rewritten by scripts/codemods/bootstrap4-to-bootstrap5.mjs,
//but a third-party plugin view still ships the Bootstrap 4 spelling, and so does markup a
//view loads over AJAX into a modal. Bootstrap 5 reads the attributes off the DOM when the
//click happens, so renaming them as the elements appear is enough - no widget has to be
//constructed here.

//data-target and data-parent also exist outside Bootstrap (the admin search box, the
//grid adapter), so only the ones on an element that also carries a Bootstrap 4 toggle,
//dismiss or ride attribute are renamed.
const TRIGGERS = ['data-toggle', 'data-dismiss', 'data-ride', 'data-spy', 'data-slide', 'data-slide-to']
const OPTIONS = [
    'data-target', 'data-parent', 'data-content', 'data-placement', 'data-trigger',
    'data-html', 'data-offset', 'data-boundary', 'data-animation', 'data-delay',
    'data-container', 'data-template', 'data-title', 'data-backdrop', 'data-keyboard',
    'data-focus', 'data-interval', 'data-pause', 'data-wrap', 'data-touch',
    'data-autohide', 'data-display', 'data-fallback-placement', 'data-popper-config',
    'data-reference', 'data-selector', 'data-custom-class', 'data-sanitize',
    'data-allow-list', 'data-scroll', 'data-root-margin', 'data-smooth-scroll'
]

//The Bootstrap 4 names that are Bootstrap's alone and are safe to rename wherever they sit.
const TOGGLE_VALUES = new Set([
    'modal', 'collapse', 'dropdown', 'tab', 'pill', 'list', 'tooltip', 'popover', 'button', 'buttons'
])

function renameOn(element) {
    let renamed = false
    for (const name of TRIGGERS) {
        const value = element.getAttribute(name)
        if (value === null || element.hasAttribute(`data-bs-${name.slice(5)}`)) continue
        if (name === 'data-toggle' && !TOGGLE_VALUES.has(value)) continue
        element.setAttribute(`data-bs-${name.slice(5)}`, value)
        renamed = true
    }
    if (!renamed) return
    for (const name of OPTIONS) {
        const value = element.getAttribute(name)
        if (value === null || element.hasAttribute(`data-bs-${name.slice(5)}`)) continue
        element.setAttribute(`data-bs-${name.slice(5)}`, value)
    }
}

const SELECTOR = TRIGGERS.map(name => `[${name}]`).join(',')

export function renameBootstrap4Attributes(root) {
    const scope = root || document
    if (scope.nodeType === 1 && scope.matches?.(SELECTOR)) renameOn(scope)
    scope.querySelectorAll?.(SELECTOR).forEach(renameOn)
}

export function watchBootstrap4Attributes() {
    renameBootstrap4Attributes(document)
    new MutationObserver(records => {
        for (const record of records) {
            for (const node of record.addedNodes) {
                if (node.nodeType === 1) renameBootstrap4Attributes(node)
            }
        }
    }).observe(document.documentElement, { childList: true, subtree: true })
}
