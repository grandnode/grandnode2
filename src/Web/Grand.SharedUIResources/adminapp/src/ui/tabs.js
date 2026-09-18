//GrandAdmin.tabs - the replacement for the Kendo TabStrip the admin-tabstrip and
//localized-editor tag helpers rendered.
//
//The markup is the Bootstrap tab markup (nav-tabs / tab-content / tab-pane), which looks
//the same in Bootstrap 4 and 5, but the behaviour is this file's own: neither version's
//tab plugin is used, so the panels keep working across the Bootstrap 5 migration
//(phase 5) without a second rewrite, and no jQuery plugin has to be loaded for it.
//
//What the panels rely on and this keeps:
//  - the global hooks tabstrip_on_tab_select(e) / tabstrip_on_tab_show(e, load), with
//    e.item (the <li>) and e.contentElement (the pane) - admin.common.js reads them to
//    save the selected tab and to load the grids of a tab the first time it is shown;
//  - k-state-active on the active <li> and pane, which tabstrip_on_tab_show looks up
//    globally on the initial load;
//  - #selected-tab-index, posted with the form and restored by the tag helper.

const OPEN_TAB_EVENT = 'grand-tabstrip:show'

/**
 * The selected-tab-index input a strip owns, or null. <admin-tabstrip> renders the input
 * straight before its strip, so only the strip it stands in front of remembers its tab: the
 * page's own. A strip inside a pane (Product > Product attributes) renders a second one the
 * server never reads - it takes the first value posted - and a localized editor's language
 * strip has none. Both used to write their own index into the page's input through
 * $("#selected-tab-index"), so saving the form reopened the page on the wrong tab.
 * @param {Element} node a strip, or an element inside one (the <li> of a tab)
 */
export function selectedTabIndexInput(node) {
    const strip = node?.closest?.('[data-grand-tabstrip]')
    const input = strip?.previousElementSibling
    return input?.matches('input[name="selected-tab-index"]') ? input : null
}

function call(name, ...args) {
    const fn = globalThis[name]
    if (typeof fn === 'function') return fn(...args)
    return undefined
}

function jq(element) {
    const $ = window.jQuery
    return $ ? $(element) : element
}

export class TabStrip {
    constructor(element, config = {}) {
        this.element = element
        this.config = config
        this.list = element.querySelector(':scope > ul.nav')
        this.content = element.querySelector(':scope > .tab-content')
        this.items = this.list ? Array.from(this.list.querySelectorAll(':scope > li')) : []
        this.panes = this.content ? Array.from(this.content.querySelectorAll(':scope > .tab-pane')) : []
        this.selectedIndex = -1

        this.list?.addEventListener('click', e => {
            const item = e.target.closest('li')
            if (!item || !this.items.includes(item)) return
            e.preventDefault()
            this.select(this.items.indexOf(item))
        })
        this.list?.addEventListener('keydown', e => {
            const item = e.target.closest('li')
            if (!item || !this.items.includes(item)) return
            const at = this.items.indexOf(item)
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault()
                this.select(at)
                return
            }
            //a tablist is one tab stop: the arrows move between the tabs inside it
            const mirror = this.rtl ? -1 : 1
            let next = -1
            if (e.key === 'ArrowRight') next = at + mirror
            else if (e.key === 'ArrowLeft') next = at - mirror
            else if (e.key === 'ArrowDown' && this.vertical) next = at + 1
            else if (e.key === 'ArrowUp' && this.vertical) next = at - 1
            else if (e.key === 'Home') next = 0
            else if (e.key === 'End') next = this.items.length - 1
            else return
            e.preventDefault()
            next = (next + this.items.length) % this.items.length
            this.select(next)
            this.items[next].querySelector('.nav-link')?.focus()
        })

        this.appendPending()
        //the markup decides: a nested strip forces its active tab with k-state-active
        //(Product/ProductAttributes), the rest carry the restored selected-tab-index
        const marked = this.items.findIndex(item => item.classList.contains('k-state-active'))
        const initial = marked >= 0
            ? marked
            : Math.min(Math.max(config.selectedIndex || 0, 0), Math.max(this.items.length - 1, 0))
        this.activate(initial)
        //the tag helper renders the strip hidden so the unstyled panes never flash
        element.style.display = ''

        if (config.bindGrid && initial > 0) {
            //Kendo loaded the grids of a restored tab on window load; the handler reads
            //.k-state-active itself when the second argument is passed
            const fire = () => call('tabstrip_on_tab_show', this.eventArgs(initial), true)
            if (document.readyState === 'complete') fire()
            else window.addEventListener('load', fire, { once: true })
        }
    }

    /** true while the strip is laid out right to left, so the arrow keys mirror. */
    get rtl() {
        const view = this.element.ownerDocument?.defaultView
        if (!view?.getComputedStyle) return false
        return view.getComputedStyle(this.element).direction === 'rtl'
    }

    /** The selected-tab-index input of this strip; null for a nested or language strip. */
    get indexInput() {
        return selectedTabIndexInput(this.element)
    }

    /** tab-position="left": the list is beside the panes, so up and down move between tabs. */
    get vertical() {
        return this.element.classList.contains('grand-tabstrip-left')
    }

    /**
     * Points each tab at its pane and back. The tag helper cannot do it - it renders the
     * items and the panes in two passes and neither carries an id - so a screen reader had
     * no way to tell which panel a tab opened.
     */
    linkPanes() {
        const base = this.element.id || 'grand-tabstrip'
        this.items.forEach((item, i) => {
            const link = item.querySelector('.nav-link')
            const pane = this.panes[i]
            if (!link || !pane) return
            if (!link.id) link.id = `${base}-tab-${i}`
            if (!pane.id) pane.id = `${base}-pane-${i}`
            link.setAttribute('aria-controls', pane.id)
            pane.setAttribute('aria-labelledby', link.id)
        })
    }

    eventArgs(index) {
        return { item: this.items[index], contentElement: this.panes[index], index, sender: this }
    }

    /** Index of the active tab (Kendo select() without an argument). */
    select(index) {
        if (index === undefined) return this.selectedIndex
        if (index < 0 || index >= this.items.length || index === this.selectedIndex) return this
        call('tabstrip_on_tab_select', this.eventArgs(index))
        this.activate(index)
        if (this.config.bindGrid) call('tabstrip_on_tab_show', this.eventArgs(index))
        this.resizeGrids(index)
        this.panes[index]?.dispatchEvent(new CustomEvent(OPEN_TAB_EVENT, { bubbles: true, detail: { index } }))
        return this
    }

    activate(index) {
        this.selectedIndex = index
        this.linkPanes()
        this.items.forEach((item, i) => {
            const active = i === index
            item.classList.toggle('active', active)
            //the Kendo class the panels still select on
            item.classList.toggle('k-state-active', active)
            const link = item.querySelector('.nav-link')
            link?.classList.toggle('active', active)
            link?.setAttribute('aria-selected', String(active))
            link?.setAttribute('tabindex', active ? '0' : '-1')
        })
        this.panes.forEach((pane, i) => {
            const active = i === index
            pane.classList.toggle('active', active)
            pane.classList.toggle('show', active)
            pane.classList.toggle('k-state-active', active)
            //the class drives visibility (see ui.css); the attribute would fight Bootstrap's
            //.tab-content > .active { display: block }
            pane.setAttribute('aria-hidden', String(!active))
        })
        //a screen full of tabs scrolls sideways rather than wrapping (see ui.css), so the
        //one that has just become active is brought into view
        this.items[index]?.scrollIntoView?.({ block: 'nearest', inline: 'nearest' })
    }

    /** Tabulator measures nothing while its pane is hidden, so shown grids are redrawn. */
    resizeGrids(index) {
        const pane = this.panes[index]
        if (!pane) return
        for (const element of pane.querySelectorAll('[data-role="grid"]')) {
            const api = element.grandGrid?.api
            if (api && typeof api.resize === 'function') api.resize()
        }
    }

    /**
     * Appends a tab. Kendo's append({ text, content }) took HTML strings; here both are
     * markup rendered by the server, never interpolated into a script.
     */
    append({ text, content, contentNode } = {}) {
        if (!this.list || !this.content) return this
        const doc = this.element.ownerDocument
        const item = doc.createElement('li')
        item.className = 'nav-item'
        item.setAttribute('role', 'presentation')
        const link = doc.createElement('a')
        link.className = 'nav-link'
        link.href = '#'
        link.setAttribute('role', 'tab')
        //text, not markup: an appended tab's title comes from a plugin and used to be
        //interpolated into a script, which is the hole this replacement closes
        link.textContent = text == null ? '' : String(text)
        item.appendChild(link)
        this.list.appendChild(item)

        const pane = doc.createElement('div')
        pane.className = 'tab-pane'
        pane.setAttribute('role', 'tabpanel')
        pane.setAttribute('aria-hidden', 'true')
        if (contentNode) pane.appendChild(contentNode)
        else pane.innerHTML = content == null ? '' : String(content)
        this.content.appendChild(pane)

        this.items.push(item)
        this.panes.push(pane)
        return this
    }

    /** Consumes the <template data-grand-tab-append="stripId"> blocks plugins render. */
    appendPending(doc = this.element.ownerDocument) {
        const id = this.element.id
        if (!id) return
        const pending = Array.from(doc.querySelectorAll('template[data-grand-tab-append]'))
            .filter(template => template.getAttribute('data-grand-tab-append') === id)
        for (const template of pending) {
            this.append({ text: template.dataset.tabName, contentNode: template.content.cloneNode(true) })
            template.remove()
        }
    }
}

export function createTabs({ doc = globalThis.document } = {}) {
    const strips = new Map()

    function init(root = doc) {
        const scope = root.querySelectorAll ? root : doc
        const elements = []
        if (scope.matches?.('[data-grand-tabstrip]')) elements.push(scope)
        elements.push(...scope.querySelectorAll('[data-grand-tabstrip]'))
        const created = []
        for (const element of elements) {
            if (element.grandTabStrip) continue
            let config = {}
            try {
                config = JSON.parse(element.getAttribute('data-grand-tabstrip') || '{}')
            } catch (error) {
                console.error(`[admin-ui] invalid tabstrip configuration on #${element.id}`, error)
            }
            const strip = new TabStrip(element, config)
            element.grandTabStrip = strip
            if (window.jQuery) window.jQuery.data(element, 'grandTabStrip', strip)
            if (element.id) strips.set(element.id, strip)
            created.push(strip)
        }
        return created
    }

    return {
        init,
        get: id => strips.get(id),
        all: () => Array.from(strips.values()),
        /** The selected-tab-index input the strip of a tab owns (tabstrip_on_tab_select). */
        indexInputOf: selectedTabIndexInput,
        /** The strip an element belongs to, as jQuery callers expect to look it up. */
        of: element => jq(element)?.[0]?.grandTabStrip ?? element?.grandTabStrip
    }
}
