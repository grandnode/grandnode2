import { formatNumber } from './format.js'

//Pager under the grid. "Full" mirrors pageable { refresh, pageSizes } (first/previous,
//numbers, next/last, page size, "1 - 15 of 40 items", refresh); "Compact" mirrors
//pageable { refresh: true, numeric: false, previousNext: false, info: false } - the
//refresh button only. Rendered with plain buttons so it works in Bootstrap 4 and 5.

const maxNumericButtons = 10

function button(doc, { className, label, icon, text, disabled, onClick, current }) {
    const element = doc.createElement('button')
    element.type = 'button'
    element.className = `grand-grid-pager-button ${className || ''}`.trim()
    if (label) {
        element.title = label
        element.setAttribute('aria-label', label)
    }
    if (icon) {
        const i = doc.createElement('i')
        i.className = icon
        i.setAttribute('aria-hidden', 'true')
        element.appendChild(i)
    }
    if (text != null) element.appendChild(doc.createTextNode(text))
    if (current) {
        element.setAttribute('aria-current', 'page')
        element.classList.add('active')
    }
    element.disabled = Boolean(disabled)
    element.addEventListener('click', e => {
        e.preventDefault()
        if (!element.disabled) onClick()
    })
    return element
}

/** Page numbers to show: a window of up to 10 around the current page, like Kendo. */
export function pageWindow(page, totalPages, size = maxNumericButtons) {
    const start = Math.max(1, Math.min(page - Math.floor((size - 1) / 2), totalPages - size + 1))
    const end = Math.min(totalPages, start + size - 1)
    const pages = []
    for (let p = start; p <= end; p++) pages.push(p)
    return pages
}

/** "{0} - {1} of {2} items" filled for the current page. */
export function pageInfo(template, dataSource, culture) {
    const total = dataSource.total()
    if (total === 0) return ''
    const size = dataSource.pageSize() || total
    const first = (dataSource.page() - 1) * size + 1
    const last = Math.min(total, first + dataSource.data().length - 1)
    const n = value => formatNumber(value, 'n0', culture)
    return String(template || '{0} - {1} / {2}')
        .replace('{0}', n(first))
        .replace('{1}', n(Math.max(first, last)))
        .replace('{2}', n(total))
}

export function renderPager(container, { mode, dataSource, pageSizes, texts, culture, doc = document }) {
    container.textContent = ''
    if (mode === 'None') {
        container.hidden = true
        return
    }
    container.hidden = false
    const t = texts || {}
    const refresh = button(doc, {
        className: 'grand-grid-refresh',
        label: t.refresh,
        icon: 'bi bi-arrow-clockwise',
        onClick: () => dataSource.read()
    })

    if (mode === 'Compact' || !dataSource.pageSize()) {
        container.appendChild(refresh)
        return
    }

    const page = dataSource.page()
    const totalPages = dataSource.totalPages()
    const nav = doc.createElement('div')
    nav.className = 'grand-grid-pager-nav'
    nav.setAttribute('role', 'navigation')
    const go = p => dataSource.page(p)
    nav.appendChild(button(doc, { className: 'grand-grid-first', label: t.firstPage, icon: 'bi bi-chevron-double-left', disabled: page <= 1, onClick: () => go(1) }))
    nav.appendChild(button(doc, { className: 'grand-grid-previous', label: t.previousPage, icon: 'bi bi-chevron-left', disabled: page <= 1, onClick: () => go(page - 1) }))
    const numbers = doc.createElement('span')
    numbers.className = 'grand-grid-pager-numbers'
    for (const p of pageWindow(page, totalPages)) {
        numbers.appendChild(button(doc, {
            className: 'grand-grid-page',
            text: String(p),
            current: p === page,
            onClick: () => go(p)
        }))
    }
    nav.appendChild(numbers)
    nav.appendChild(button(doc, { className: 'grand-grid-next', label: t.nextPage, icon: 'bi bi-chevron-right', disabled: page >= totalPages, onClick: () => go(page + 1) }))
    nav.appendChild(button(doc, { className: 'grand-grid-last', label: t.lastPage, icon: 'bi bi-chevron-double-right', disabled: page >= totalPages, onClick: () => go(totalPages) }))
    container.appendChild(nav)

    if (pageSizes && pageSizes.length > 0) {
        const label = doc.createElement('label')
        label.className = 'grand-grid-page-size'
        const select = doc.createElement('select')
        //Bootstrap 5 styles a select with form-select; form-control resets its appearance
        //and takes the native arrow with it
        select.className = 'form-select form-select-sm'
        const sizes = pageSizes.includes(dataSource.pageSize()) ? pageSizes : [...pageSizes, dataSource.pageSize()].sort((a, b) => a - b)
        for (const size of sizes) {
            const option = doc.createElement('option')
            option.value = String(size)
            option.textContent = String(size)
            option.selected = size === dataSource.pageSize()
            select.appendChild(option)
        }
        select.addEventListener('change', () => dataSource.pageSize(Number(select.value)))
        label.appendChild(select)
        label.appendChild(doc.createTextNode(' ' + (t.itemsPerPage || '')))
        container.appendChild(label)
    }

    const info = doc.createElement('span')
    info.className = 'grand-grid-pager-info'
    info.textContent = pageInfo(t.pageInfo, dataSource, culture)
    container.appendChild(info)
    container.appendChild(refresh)
}
