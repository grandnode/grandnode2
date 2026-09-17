// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { createTabs } from './tabs.js'

function markup({ config = '{"bindGrid":true,"selectedIndex":0}', active = 0, tabs = 3 } = {}) {
    const items = Array.from({ length: tabs }, (_, i) =>
        `<li class="nav-item${i === active ? ' active k-state-active' : ''}"><a class="nav-link${i === active ? ' active' : ''}" href="#">Tab ${i}</a></li>`).join('')
    const panes = Array.from({ length: tabs }, (_, i) =>
        `<div class="tab-pane${i === active ? ' active show k-state-active' : ''}"><span id="body-${i}">${i}</span></div>`).join('')
    return `<div id="strip" class="grand-tabstrip" style="display:none" data-grand-tabstrip='${config}'>
        <ul class="nav nav-tabs">${items}</ul><div class="tab-content">${panes}</div></div>`
}

describe('tab strip', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
        globalThis.tabstrip_on_tab_select = vi.fn()
        globalThis.tabstrip_on_tab_show = vi.fn()
    })

    afterEach(() => {
        delete globalThis.tabstrip_on_tab_select
        delete globalThis.tabstrip_on_tab_show
    })

    it('shows the strip and activates the tab the markup marks', () => {
        document.body.innerHTML = markup({ active: 1, config: '{"bindGrid":false,"selectedIndex":0}' })
        createTabs({}).init(document)
        const strip = document.getElementById('strip')
        expect(strip.style.display).toBe('')
        expect(strip.grandTabStrip.select()).toBe(1)
        expect(strip.querySelectorAll('.tab-pane')[1].classList.contains('active')).toBe(true)
    })

    it('falls back to the configured selected index', () => {
        document.body.innerHTML = markup({ active: -1, config: '{"bindGrid":false,"selectedIndex":2}' })
        createTabs({}).init(document)
        expect(document.getElementById('strip').grandTabStrip.select()).toBe(2)
    })

    it('clamps a selected index past the last tab, as a nested strip needs', () => {
        document.body.innerHTML = markup({ active: -1, config: '{"bindGrid":false,"selectedIndex":99}' })
        createTabs({}).init(document)
        expect(document.getElementById('strip').grandTabStrip.select()).toBe(2)
    })

    it('calls the select and show hooks in Kendo order and moves k-state-active', () => {
        document.body.innerHTML = markup()
        createTabs({}).init(document)
        const items = document.querySelectorAll('#strip li')
        items[2].querySelector('a').click()
        expect(globalThis.tabstrip_on_tab_select).toHaveBeenCalledTimes(1)
        const selectArgs = globalThis.tabstrip_on_tab_select.mock.calls[0][0]
        expect(selectArgs.item).toBe(items[2])
        const showArgs = globalThis.tabstrip_on_tab_show.mock.calls[0][0]
        expect(showArgs.contentElement).toBe(document.querySelectorAll('#strip .tab-pane')[2])
        expect(items[2].classList.contains('k-state-active')).toBe(true)
        expect(items[0].classList.contains('k-state-active')).toBe(false)
        expect(document.querySelectorAll('#strip .tab-pane')[0].classList.contains('k-state-active')).toBe(false)
    })

    it('does not fire the show hook when the strip is not bound to grids', () => {
        document.body.innerHTML = markup({ config: '{"bindGrid":false,"selectedIndex":0}' })
        createTabs({}).init(document)
        document.querySelectorAll('#strip li')[1].querySelector('a').click()
        expect(globalThis.tabstrip_on_tab_select).toHaveBeenCalled()
        expect(globalThis.tabstrip_on_tab_show).not.toHaveBeenCalled()
    })

    it('appends the tabs a plugin rendered as a template, with the title as text', () => {
        document.body.innerHTML = markup({ config: '{"bindGrid":false,"selectedIndex":0}' }) +
            '<template data-grand-tab-append="strip" data-tab-name="&lt;b&gt;Plug&lt;/b&gt;"><div id="plugin-body">from a plugin</div></template>'
        createTabs({}).init(document)
        const items = document.querySelectorAll('#strip li')
        expect(items.length).toBe(4)
        expect(items[3].textContent).toBe('<b>Plug</b>')
        expect(items[3].querySelector('b')).toBeNull()
        expect(document.querySelector('#strip .tab-content #plugin-body')).not.toBeNull()
        expect(document.querySelector('template[data-grand-tab-append]')).toBeNull()
    })

    it('only builds one strip per element', () => {
        document.body.innerHTML = markup()
        const tabs = createTabs({})
        tabs.init(document)
        tabs.init(document)
        expect(tabs.all().length).toBe(1)
    })
})

describe('tab strip keyboard and aria', () => {
    beforeEach(() => {
        document.body.innerHTML = markup({ config: '{"bindGrid":false,"selectedIndex":0}' })
    })

    const key = (target, name) =>
        target.dispatchEvent(new window.KeyboardEvent('keydown', { key: name, bubbles: true, cancelable: true }))

    it('points each tab at its pane and back', () => {
        createTabs({}).init(document)
        const link = document.querySelectorAll('.nav-link')[1]
        const pane = document.querySelectorAll('.tab-pane')[1]
        expect(link.getAttribute('aria-controls')).toBe(pane.id)
        expect(pane.getAttribute('aria-labelledby')).toBe(link.id)
        expect(pane.id).toBe('strip-pane-1')
    })

    it('moves between the tabs with the arrows, wrapping at the ends', () => {
        const tabs = createTabs({})
        tabs.init(document)
        const strip = tabs.get('strip')
        const links = document.querySelectorAll('.nav-link')
        key(links[0], 'ArrowRight')
        expect(strip.select()).toBe(1)
        expect(document.activeElement).toBe(links[1])
        key(links[1], 'End')
        expect(strip.select()).toBe(2)
        key(links[2], 'ArrowRight')
        expect(strip.select()).toBe(0)
        key(links[0], 'Home')
        expect(strip.select()).toBe(0)
    })

    it('keeps one tab stop in the strip', () => {
        const tabs = createTabs({})
        tabs.init(document)
        tabs.get('strip').select(2)
        const tabIndexes = Array.from(document.querySelectorAll('.nav-link')).map(l => l.getAttribute('tabindex'))
        expect(tabIndexes).toEqual(['-1', '-1', '0'])
    })
})
