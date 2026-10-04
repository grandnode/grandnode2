// @vitest-environment jsdom
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { initConfirmActions } from './confirm.js'

describe('confirm actions', () => {
    let dispose

    beforeEach(() => {
        document.body.innerHTML = `
            <form id="f" action="/order/edit" method="post">
                <input type="submit" id="cancel" name="cancelorder" value="Cancel"
                       data-grand-confirm="Are you sure?" data-confirm-title="Cancel order" data-confirm-ok="Yes">
                <input type="submit" id="plain" name="save" value="Save">
            </form>`
    })

    afterEach(() => dispose?.())

    const flush = () => new Promise(resolve => setTimeout(resolve, 0))

    it('asks before the click reaches the element, with the texts it declares', async () => {
        const ask = vi.fn(() => Promise.resolve(false))
        dispose = initConfirmActions(document, ask, () => ({ cancel: 'No' }))
        const handler = vi.fn()
        document.getElementById('cancel').addEventListener('click', handler)

        document.getElementById('cancel').click()
        await flush()

        expect(ask).toHaveBeenCalledWith('Are you sure?', { title: 'Cancel order', confirmText: 'Yes', cancelText: 'No' })
        expect(handler).not.toHaveBeenCalled()
    })

    it('does not submit when the answer is no', async () => {
        dispose = initConfirmActions(document, () => Promise.resolve(false))
        const submitted = vi.fn(e => e.preventDefault())
        document.getElementById('f').addEventListener('submit', submitted)

        document.getElementById('cancel').click()
        await flush()

        expect(submitted).not.toHaveBeenCalled()
    })

    it('clicks the same element again on yes, so the form posts that button', async () => {
        dispose = initConfirmActions(document, () => Promise.resolve(true))
        const submitted = vi.fn(e => {
            e.preventDefault()
            return e.submitter?.name
        })
        document.getElementById('f').addEventListener('submit', submitted)

        document.getElementById('cancel').click()
        await flush()

        expect(submitted).toHaveBeenCalledTimes(1)
        expect(submitted.mock.calls[0][0].submitter.name).toBe('cancelorder')
        //the flag of the confirmed click is spent: the next click asks again
        expect(document.getElementById('cancel').dataset.grandConfirmed).toBeUndefined()
    })

    it('leaves elements without the attribute alone', async () => {
        const ask = vi.fn(() => Promise.resolve(true))
        dispose = initConfirmActions(document, ask)
        const submitted = vi.fn(e => e.preventDefault())
        document.getElementById('f').addEventListener('submit', submitted)

        document.getElementById('plain').click()
        await flush()

        expect(ask).not.toHaveBeenCalled()
        expect(submitted).toHaveBeenCalledTimes(1)
    })
})
