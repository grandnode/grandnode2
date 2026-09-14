import { describe, expect, it, vi } from 'vitest'
import { createKendoApi } from './kendo-api.js'

function fakeGrid() {
    return {
        dataSource: { read: vi.fn(), page: vi.fn(), total: vi.fn(() => 3) },
        resize: vi.fn(),
        refresh: vi.fn(),
        cancelEdit: vi.fn()
    }
}

describe('createKendoApi', () => {
    it('exposes resize for kendo.resize, which the Kendo TabStrip calls on shown tabs', () => {
        const grid = fakeGrid()
        const api = createKendoApi(grid)
        api.resize(true)
        expect(grid.resize).toHaveBeenCalledTimes(1)
    })

    it('forwards the dataSource calls views make', () => {
        const grid = fakeGrid()
        const api = createKendoApi(grid)
        api.dataSource.read()
        api.dataSource.page(1)
        expect(grid.dataSource.read).toHaveBeenCalled()
        expect(grid.dataSource.page).toHaveBeenCalledWith(1)
        expect(api.dataSource.total()).toBe(3)
    })
})
