// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest'
import { renameBootstrap4Attributes, watchBootstrap4Attributes } from './bs4-attributes.js'

describe('bs4 data attributes', () => {
    beforeEach(() => {
        document.body.innerHTML = ''
    })

    it('renames a Bootstrap 4 modal trigger', () => {
        document.body.innerHTML = '<a data-toggle="modal" data-target="#dialog">open</a>'
        renameBootstrap4Attributes(document)
        const link = document.querySelector('a')
        expect(link.getAttribute('data-bs-toggle')).toBe('modal')
        expect(link.getAttribute('data-bs-target')).toBe('#dialog')
        //the Bootstrap 4 spelling stays: a view may read it back, and Bootstrap 5 ignores it
        expect(link.getAttribute('data-toggle')).toBe('modal')
    })

    it('renames dismiss and the options that go with it', () => {
        document.body.innerHTML = '<button class="close" data-dismiss="modal"></button>'
        renameBootstrap4Attributes(document)
        expect(document.querySelector('button').getAttribute('data-bs-dismiss')).toBe('modal')
    })

    it('renames tooltip and popover options', () => {
        document.body.innerHTML = '<i data-toggle="tooltip" data-placement="top" data-html="true" title="x"></i>'
        renameBootstrap4Attributes(document)
        const icon = document.querySelector('i')
        expect(icon.getAttribute('data-bs-placement')).toBe('top')
        expect(icon.getAttribute('data-bs-html')).toBe('true')
    })

    it('leaves a data-toggle that is not a Bootstrap widget alone', () => {
        document.body.innerHTML = '<div data-toggle="grid" data-target="#products-grid"></div>'
        renameBootstrap4Attributes(document)
        const element = document.querySelector('div')
        expect(element.hasAttribute('data-bs-toggle')).toBe(false)
        expect(element.hasAttribute('data-bs-target')).toBe(false)
    })

    it('leaves a data-target that belongs to something else alone', () => {
        document.body.innerHTML = '<input data-target="#search-results">'
        renameBootstrap4Attributes(document)
        expect(document.querySelector('input').hasAttribute('data-bs-target')).toBe(false)
    })

    it('does not overwrite a Bootstrap 5 attribute that is already there', () => {
        document.body.innerHTML = '<a data-toggle="tab" data-bs-toggle="pill">x</a>'
        renameBootstrap4Attributes(document)
        expect(document.querySelector('a').getAttribute('data-bs-toggle')).toBe('pill')
    })

    it('renames markup that arrives after the page has loaded', async () => {
        watchBootstrap4Attributes()
        const holder = document.createElement('div')
        holder.innerHTML = '<a data-toggle="collapse" data-target="#panel">x</a>'
        document.body.appendChild(holder)
        await new Promise(resolve => setTimeout(resolve, 0))
        expect(holder.querySelector('a').getAttribute('data-bs-target')).toBe('#panel')
    })
})
