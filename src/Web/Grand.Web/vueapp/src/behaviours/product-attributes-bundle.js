/*
 * Attribute controls for a product inside a bundle
 * (was Views/Product/Partials/ProductAttributesBundle.cshtml).
 *
 * That view built the change wiring as *generated JavaScript*: a C# StringBuilder
 * emitted one addEventListener call per attribute control, which the component's
 * mounted() hook then evaluated. One delegated listener on the bundle container
 * replaces the whole generator.
 */
import LegacyVue from '../compat/core'
import { registerView } from '../views/index'
import { axios } from '../views/shared'
import { delegate } from './dom'

function reprice(container) {
    const productId = container.dataset.bundleProduct
    const form = document.getElementById('product-details-form')
    if (!form) return

    return axios.post(container.dataset.attributeRoute, new FormData(form), {
        params: { product: productId }
    }).then(response => {
        const price = document.querySelector('.price-value-' + productId)
        if (price && response.data.price) price.innerText = response.data.price
    }).catch(err => console.error('[grand] bundle attribute change failed', err))
}

// one listener for every bundle on the page, whichever control was touched
delegate('change', '[data-bundle-product] [id^="product_attribute_"]',
    control => reprice(control.closest('[data-bundle-product]')))
delegate('click', '[data-bundle-product] [id^="product_attribute_"]',
    control => reprice(control.closest('[data-bundle-product]')))

registerView('productAttributesBundle', ({ componentName, templateId, containerId, attributes }) => {
    LegacyVue.component(componentName, {
        template: templateId,
        data: () => ({ productAttributes: attributes }),
        mounted() {
            // prime the price for the preselected values
            const container = document.getElementById(containerId)
            if (container) reprice(container)
        }
    })
})
