/*
 * Shopping cart page view-model (was Views/ShoppingCart/Partials/ModelScript.cshtml).
 *
 * Still published as the global `vmorder` because the Razor markup of the cart
 * page - and the root #app template - address it by that name.
 */
import * as bootstrap from 'bootstrap'
import LegacyVue from '../compat/core'
import { registerView } from './index'
import { axios, formData, notify, notifyRequestError } from './shared'

registerView('shoppingCart', ({ model, routes, res }) => {
    const warn = message => notify(message, res.warning)

    window.vmorder = new LegacyVue({
        data: () => ({
            cart: model,
            totals: null,
            checkoutAsGuest: false,
            // confirm order
            MinOrderTotalWarning: null,
            TermsOfServiceOnOrderConfirmPage: null,
            ConfirmWarnings: null,
            // terms of service
            terms: false,
            acceptTerms: false
        }),
        created() {
            this.updateTotals()
            document.body.classList.add('cart-view')
        },
        watch: {
            terms() {
                if (this.terms === true) this.acceptTerms = false
            }
        },
        methods: {
            updateCart() {
                return axios.get(routes.summary)
                    .then(response => { this.cart = response.data })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            updateTotals() {
                return axios.get(routes.total)
                    .then(response => { this.totals = response.data })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            termsCheck(guest) {
                if (this.cart.MinOrderSubtotalWarning != null) return
                if (this.terms) {
                    this.checkout(guest)
                    this.acceptTerms = false
                } else {
                    this.acceptTerms = true
                }
            },
            /*
             * "Checkout as guest" navigates straight to checkout rather than posting the
             * cart form: the selected checkout attributes are already persisted, because
             * every control in Partials/CheckoutAttributes posts to CheckoutAttributeChange
             * as it is changed. The required-attribute check that this path used to miss
             * now lives in CheckoutController.Start, which covers every way into checkout,
             * not just this button.
             */
            checkout(guest) {
                if (guest) {
                    window.location = routes.checkout
                    return
                }
                if (!this.cart.ShowCheckoutAsGuestButton && this.cart.IsGuest) {
                    window.location = routes.loginReturningToCart
                    return
                }
                const form = document.getElementById('shopping-cart-form')
                form.setAttribute('action', routes.startCheckout)
                form.submit()
            },
            ApplyGiftVoucher(href) {
                return axios.post(href, formData({
                    giftvouchercouponcode: document.getElementById('giftvouchercouponcode').value
                })).then(response => {
                    this.cart = response.data.model
                    return this.updateTotals()
                }).catch(error => notifyRequestError(error, res.warning))
            },
            removeGiftVoucher(href) {
                return axios.get(href)
                    .then(response => {
                        this.cart = response.data.model
                        return this.updateTotals()
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            getModel() {
                return axios.get(routes.shoppingCart, { params: { timestamp: new Date().getTime() } })
                    .then(response => {
                        this.cart = response.data
                        window.vm.flycartindicator = this.cart.Items
                            .reduce((total, item) => total + item.Quantity, 0)
                        return this.updateTotals()
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            updateQuantity(element, id) {
                return axios.post(routes.updateQuantity, formData({
                    shoppingcartId: id,
                    ShoppingCartType: 1,
                    quantity: document.getElementById(element + id).value
                })).then(response => {
                    if (response.data.success) {
                        this.cart = response.data.model
                        window.vm.flycartindicator = response.data.totalproducts
                    } else {
                        warn(response.data.warnings)
                    }
                    return this.updateTotals()
                }).catch(error => notifyRequestError(error, res.warning))
            },
            updateCartType(id, iscart) {
                return axios.post(routes.changeTypeCartItem, formData({ id: id, status: iscart }))
                    .then(response => {
                        this.cart = response.data.model
                        window.vm.updateSidebarShoppingCart(routes.sidebarShoppingCart)
                        return this.updateTotals()
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            updateOnCart(id) {
                return axios.get(`${routes.getItemCart}/${id}`)
                    .then(response => {
                        if (!response.data.success) {
                            warn(response.data.message)
                            return
                        }
                        window.vm.PopupQuickViewVueModal = response.data.model
                        window.vm.UpdatedShoppingCartItemId = id
                        bootstrap.Modal.getOrCreateInstance(document.getElementById('ModalQuickView')).show()
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            cartClick(event) {
                const btn = event.target.closest('.deleteshoppingcartitem')
                if (btn && btn.dataset.deleteUrl) this.deleteitem(btn.dataset.deleteUrl)
            },
            deleteitem(href) {
                return axios.post(href).then(response => {
                    const vm = window.vm
                    vm.flycartindicator = response.data.totalproducts
                    if (document.querySelector('.sidebar-cart')) {
                        const newfly = response.data.sidebarshoppingcartmodel
                        vm.flycart = newfly
                        vm.flycartitems = newfly.Items
                        vm.flycartindicator = newfly.TotalProducts
                    }
                    this.cart = response.data.model
                    return this.updateTotals()
                }).catch(error => notifyRequestError(error, res.warning))
            },
            ApplyDiscountCode(href) {
                return axios.post(href, formData({
                    discountcouponcode: document.getElementById('discountcouponcode').value
                })).then(response => {
                    this.cart = response.data.model
                    return this.updateTotals()
                }).catch(error => notifyRequestError(error, res.warning))
            },
            RemoveDiscountId(href) {
                return axios.get(href)
                    .then(response => {
                        this.cart = response.data.model
                        return this.updateTotals()
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            displayWarning(message) {
                warn(message)
            }
        }
    })
})
