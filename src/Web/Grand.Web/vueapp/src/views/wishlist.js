/*
 * Wishlist page view-model (was Views/Wishlist/Partials/ModelScript.cshtml).
 *
 * Published as the global `vmwishlist` - the wishlist markup binds to it.
 */
import * as bootstrap from 'bootstrap'
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { axios, formData, notify, notifyRequestError } from './shared'

registerView('wishlist', ({ model, routes, res }) => {
    window.vmwishlist = registerViewModel('vmwishlist', createViewModel({
        data: () => ({
            Model: model,
            PopupUpdateVueModal: null
        }),
        methods: {
            deleteFromWishlist(id) {
                return axios.get(`${routes.deleteItem}/${id}`)
                    .then(response => {
                        if (response.data.success) return this.getModel()
                        this.displayWarning(response.data.message, 'danger')
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            addToCartFromWishlist(id) {
                return axios.post(routes.addToCart + id)
                    .then(response => {
                        if (!response.data.success) {
                            this.displayWarning(response.data.message, 'danger')
                            return
                        }
                        window.vm.updateSidebarShoppingCart(routes.sidebarShoppingCart)
                        this.displayWarning(res.addedToCart, 'info', res.success)
                        return this.getModel()
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            getItemCart(id) {
                return axios.get(`${routes.getItemCart}/${id}`)
                    .then(response => {
                        if (!response.data.success) {
                            this.displayWarning(response.data.message, 'danger')
                            return
                        }
                        window.vm.PopupQuickViewVueModal = response.data.model
                        window.vm.UpdatedShoppingCartItemId = id
                        bootstrap.Modal.getOrCreateInstance(document.getElementById('ModalQuickView')).show()
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            },
            updateQuantity(element, id) {
                return axios.post(routes.updateQuantity, formData({
                    shoppingcartId: id,
                    ShoppingCartType: 2,
                    quantity: document.getElementById(element + id).value
                })).then(response => {
                    if (response.data.success) return this.getModel()
                    this.displayWarning(response.data.warnings, 'danger')
                }).catch(error => notifyRequestError(error, res.warning))
            },
            getModel() {
                return axios.get(routes.wishlist, {
                    params: { timestamp: new Date().getTime() },
                    headers: { Accept: 'application/json', 'X-Response-View': 'Json' }
                }).then(response => {
                    this.Model = response.data
                    const qty = document.querySelector('.wishlist-qty')
                    if (qty) {
                        qty.innerHTML = this.Model.Items.reduce((total, item) => total + item.Quantity, 0)
                    }
                }).catch(error => notifyRequestError(error, res.warning))
            },
            displayWarning(message, variant, title) {
                notify(message, title ?? res.warning, variant)
            }
        }
    }))
})
