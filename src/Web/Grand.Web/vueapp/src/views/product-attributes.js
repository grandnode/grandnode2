/*
 * Product attribute selection and repricing
 * (was Views/Product/Partials/ProductAttributes.cshtml and its Theme.Modern
 *  override).
 *
 * Published as `standardProductAttributes_<productId>` - a grouped product
 * renders one of these per variant, and the markup binds each input's @change
 * to its own instance.
 *
 * The two themes differed only in how they swap the picture: the default theme
 * sets the main <img> src, Modern slides its swiper gallery to the matching
 * slide (falling back to the <img> for grouped products). `picture` in the
 * payload picks between them.
 */
import { createViewModel } from '../compat/view-model'
import { onRootReady } from '../runtime/islands'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { axios, notifyRequestError } from './shared'

/** Sets innerText on an element if the response carried a value for it. */
function setText(selector, value) {
    if (!value) return
    const el = document.querySelector(selector)
    if (el) el.innerText = value
}

function toggleRows(ids, display) {
    if (!ids) return
    ids.forEach(id => {
        const label = document.querySelector('#product_attribute_label_' + id)
        const input = document.querySelector('#product_attribute_input_' + id)
        // a mapping the response mentions may not be on the page at all
        if (label) label.style.display = display
        if (input) input.style.display = display
    })
}

function updatePicture(productId, url, mode) {
    if (!url) return

    const setMainImage = () => {
        const img = document.getElementById('main-product-img-' + productId)
        if (img) img.setAttribute('src', url)
    }

    if (mode !== 'swiper' || document.querySelector('.product-grouped')) {
        setMainImage()
        return
    }

    // Modern's gallery: find the slide holding this picture and slide to it
    const swiper = window.vm?.$refs?.swiperTop?.$swiper
    if (!swiper) {
        setMainImage()
        return
    }
    const active = swiper.slides[swiper.activeIndex]?.querySelector('img')?.dataset.srcs
    if (active === url) return
    swiper.slides.forEach((slide, index) => {
        if (slide.querySelector('img')?.dataset.srcs === url) swiper.slideTo(index, 1000, false)
    })
}

registerView('productAttributes', ({ productId, attributes, route, picture, res }) => {
    const vm = createViewModel({
        data: () => ({ ProductAttributes: attributes }),
        methods: {
            attrchange() {
                const form = document.getElementById('product-details-form-' + productId)
                if (!form) return
                return axios.post(route, new FormData(form), { params: { product: productId } })
                    .then(({ data }) => {
                        // the price element carries both classes on one node in some
                        // themes and nests them in others
                        const price = document.querySelector(`.price-value-${productId} .actual-price`)
                            || document.querySelector(`.price-value-${productId}`)
                        if (data.price && price) price.innerText = data.price

                        setText('#sku-' + productId, data.sku)
                        setText('#mpn-' + productId, data.mpn)
                        setText('#gtin-' + productId, data.gtin)
                        setText('#stock-availability-value-' + productId, data.stockAvailability)

                        const subscribe = document.querySelector('#out-of-stock-subscribe-' + productId)
                        if (subscribe && typeof data.outOfStockSubscription === 'boolean') {
                            subscribe.style.display = data.outOfStockSubscription ? 'block' : 'none'
                        }
                        if (subscribe && data.buttonTextOutOfStockSubscription) {
                            subscribe.value = data.buttonTextOutOfStockSubscription
                        }

                        toggleRows(data.enabledattributemappingids, 'table-cell')
                        toggleRows(data.disabledattributemappingids, 'none')

                        // the gallery needs a beat to settle before it is addressed
                        setTimeout(() => updatePicture(productId, data.pictureDefaultSizeUrl, picture), 100)
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            }
        }
    })

    const name = 'standardProductAttributes_' + productId
    window[name] = registerViewModel(name, vm)

    // Prime the price and availability for the preselected attribute values.
    // This has to wait for the root app: the attribute inputs are rendered by
    // Vue from ProductAttributes, so before the mount the form is empty and the
    // server would price the product as if nothing were selected.
    onRootReady(root => root.$nextTick(() => vm.attrchange()))
})
