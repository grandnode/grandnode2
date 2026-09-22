// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest'
import { createBulkBar, getBulkBar, initBulkBars } from './bulkbar.js'

function build(gridId = 'products-grid') {
    document.body.innerHTML = `
        <div class="grand-card">
            <div class="grand-bulkbar" data-grand-bulkbar="${gridId}">
                <span class="grand-bulkbar__count" data-bulkbar-count="{0} selected"></span>
                <button type="button" data-bulkbar-action>Delete</button>
            </div>
            <div id="${gridId}" data-role="grid"></div>
        </div>`
    return {
        bar: document.querySelector('.grand-bulkbar'),
        grid: document.getElementById(gridId)
    }
}

function select(grid, selectedIds) {
    grid.dispatchEvent(new CustomEvent('grand-grid:selection', { bubbles: true, detail: { selectedIds } }))
}

describe('bulk bar', () => {
    beforeEach(() => { document.body.innerHTML = '' })

    it('stays hidden until rows are ticked', () => {
        const { bar } = build()
        createBulkBar(bar)
        expect(bar.classList.contains('is-active')).toBe(false)
    })

    it('shows itself and counts the selection', () => {
        const { bar, grid } = build()
        createBulkBar(bar)
        select(grid, ['1', '2', '3'])
        expect(bar.classList.contains('is-active')).toBe(true)
        expect(bar.querySelector('[data-bulkbar-count]').textContent).toBe('3 selected')
    })

    it('hides again when the selection is cleared', () => {
        const { bar, grid } = build()
        createBulkBar(bar)
        select(grid, ['1'])
        select(grid, [])
        expect(bar.classList.contains('is-active')).toBe(false)
    })

    it('ignores the selection of another grid', () => {
        const { bar } = build()
        createBulkBar(bar)
        const other = document.createElement('div')
        other.id = 'other-grid'
        other.setAttribute('data-role', 'grid')
        document.body.appendChild(other)
        select(other, ['1', '2'])
        expect(bar.classList.contains('is-active')).toBe(false)
    })

    it('is created once per element and found again', () => {
        const { bar } = build()
        const widget = createBulkBar(bar)
        expect(createBulkBar(bar)).toBe(widget)
        expect(getBulkBar(bar)).toBe(widget)
    })

    it('initialises every bar of a scope', () => {
        build()
        expect(initBulkBars(document)).toBe(1)
    })

    it('stops listening once destroyed', () => {
        const { bar, grid } = build()
        createBulkBar(bar).destroy()
        select(grid, ['1'])
        expect(bar.classList.contains('is-active')).toBe(false)
        expect(getBulkBar(bar)).toBe(null)
    })
})
