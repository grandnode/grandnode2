// @vitest-environment jsdom
import { describe, it, expect, beforeEach } from 'vitest'
import { getModal, openModal, closeModal } from './modal.js'

describe('modal', () => {
    beforeEach(() => {
        document.body.innerHTML = '<div id="host"><div id="confirm" style="display:none"><form><input name="x"></form></div></div>'
    })

    it('moves the element into a dialog appended to the body, as the Kendo Window did', () => {
        const element = document.getElementById('confirm')
        openModal('#confirm', { title: 'Are you sure?' })
        expect(element.closest('.grand-modal-body')).not.toBeNull()
        expect(element.closest('.grand-modal').parentElement).toBe(document.body)
        expect(element.style.display).toBe('')
        expect(document.querySelector('.grand-modal-title').textContent).toBe('Are you sure?')
    })

    it('opens and closes by id without the leading hash', () => {
        openModal('confirm')
        expect(document.querySelector('.grand-modal').hidden).toBe(false)
        closeModal('confirm')
        expect(document.querySelector('.grand-modal').hidden).toBe(true)
    })

    it('keeps one dialog per element across repeated opens', () => {
        openModal('#confirm', { title: 'first' })
        closeModal('#confirm')
        openModal('#confirm', { title: 'second' })
        expect(document.querySelectorAll('.grand-modal').length).toBe(1)
        expect(document.querySelector('.grand-modal-title').textContent).toBe('second')
    })

    it('renders the title as text, never as markup', () => {
        openModal('#confirm', { title: '<img src=x onerror=alert(1)>' })
        const title = document.querySelector('.grand-modal-title')
        expect(title.querySelector('img')).toBeNull()
        expect(title.textContent).toBe('<img src=x onerror=alert(1)>')
    })

    it('closes on Escape and on the close button', () => {
        openModal('#confirm')
        document.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape' }))
        expect(getModal('#confirm').isOpen).toBe(false)
        openModal('#confirm')
        document.querySelector('.grand-modal-close').click()
        expect(getModal('#confirm').isOpen).toBe(false)
    })

    //Bootstrap's .modal-dialog is `pointer-events: none`; the class on a window this widget
    //adopts made everything inside it - the file field of an import window - unclickable.
    it('drops the Bootstrap modal classes from the element it adopts', () => {
        document.body.innerHTML = '<div id="win" class="modal-dialog fade extra" style="display:none"><input type="file"></div>'
        openModal('#win')
        const element = document.getElementById('win')
        expect(element.classList.contains('modal-dialog')).toBe(false)
        expect(element.classList.contains('fade')).toBe(false)
        expect(element.classList.contains('extra')).toBe(true)
    })

    it('answers null for an element that is not on the page', () => {
        expect(openModal('#missing')).toBeNull()
        expect(closeModal('#missing')).toBeNull()
    })
})
