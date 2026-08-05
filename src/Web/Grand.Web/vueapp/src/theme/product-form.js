/*
 * Which product form an action should post.
 *
 * The quick-view modal is always in the DOM - the old BootstrapVue b-modal only
 * rendered its content while open, so code written then could treat "the modal
 * exists" as "the modal is open". It cannot any more: test the shown state.
 *
 * When quick view is open it owns the interaction, and its form carries the bare
 * id `product-details-form`. Otherwise the page's own form does, suffixed with the
 * product id. Those are different ids, so a lookup written for one finds nothing
 * in the other - and `new FormData(null)` throws rather than failing quietly.
 * That is exactly how picking a reservation date inside quick view broke.
 */
export function activeProductForm(fallbackSelector) {
    const quickView = document.querySelector('#ModalQuickView.show')
    if (quickView) return quickView.querySelector('#product-details-form')
    return fallbackSelector ? document.querySelector(fallbackSelector) : null
}
