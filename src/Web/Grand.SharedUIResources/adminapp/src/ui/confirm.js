//The confirmation of a single button or link, declared on the element instead of written as
//`onclick="return confirm('Are you sure?')"`. The views asked the browser's own confirm() in
//dozens of places - cancel an order, save a changed order status, mark a shipment delivered -
//and that dialog can neither be styled nor stops blocking the page. Here the element carries
//
//  <input type="submit" name="cancelorder" data-grand-confirm="Are you sure?"
//         data-confirm-title="Cancel order" data-confirm-ok="Yes">
//
//and a click asks the panel's modal first. On "yes" the same element is clicked again, so a
//submit button still posts its own name and formaction and a link still follows its href.

const CONFIRMED = 'grandConfirmed'

/**
 * Listens on the document, in the capture phase so the question comes before any handler the
 * element itself carries (a jQuery click, an inline onclick). Returns a function that removes
 * the listener again.
 */
export function initConfirmActions(doc = globalThis.document, ask, texts = () => ({})) {
    const onClick = event => {
        const element = event.target?.closest?.('[data-grand-confirm]')
        if (!element || element.disabled) return
        //the second, confirmed click goes through untouched
        if (element.dataset[CONFIRMED]) {
            delete element.dataset[CONFIRMED]
            return
        }
        event.preventDefault()
        event.stopImmediatePropagation()
        ask(element.getAttribute('data-grand-confirm'), {
            title: element.getAttribute('data-confirm-title') || '',
            confirmText: element.getAttribute('data-confirm-ok') || undefined,
            cancelText: element.getAttribute('data-confirm-cancel') || texts().cancel || undefined
        }).then(confirmed => {
            if (!confirmed) return
            element.dataset[CONFIRMED] = 'true'
            element.click()
        })
    }
    doc.addEventListener('click', onClick, true)
    return () => doc.removeEventListener('click', onClick, true)
}
