/*
 * Cart, wishlist and compare actions driven by [data-cart-action] attributes
 * (was wwwroot/theme/script/public.axios.js).
 *
 * The delegated listener at the bottom is bound to the document, so running
 * from <head> as part of the bundle changes nothing for it.
 */
import axios from 'axios'

/*
 * Cart/wishlist/compare actions driven by [data-cart-action] attributes
 * (was wwwroot/theme/script/public.axios.js).
 *
 * The delegated listener at the bottom is bound to the document, so loading
 * from <head> as part of the bundle changes nothing for it.
 */

/*
** axios cart implementation
*/
var AxiosCart = {
    loadWaiting: false,
    sidebarcartselector: '.sidebar-cart',

    quickview_product: function (quickviewurl) {
        axios({
            url: quickviewurl,
            method: 'get',
        }).then(function (response) {
            AxiosCart.success_process(response);
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            AxiosCart.resetLoadWaiting();
        });  
    },

    //add a product to the cart/wishlist from the catalog pages
    addproducttocart_catalog: function (urladd, showqty, productid) {
        if (showqty.toLowerCase() == 'true') {
            var qty = document.querySelector('#addtocart_' + productid + '_EnteredQuantity').value;
            if (urladd.indexOf("forceredirection") != -1) {
                urladd += '&quantity=' + qty;
            }
            else {
                urladd += '?quantity=' + qty;
            }
        }
        if (this.loadWaiting != false) {
            return;
        }
        this.setLoadWaiting(true);

        axios({
            url: urladd,
            method: 'post',
        }).then(function (response) {
            AxiosCart.success_process(response);
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            if (typeof window.vmwishlist !== 'undefined') {
                window.vmwishlist.getModel();
            }
            if (typeof window.vmorder !== 'undefined') {
                window.vmorder.getModel();
            }
            AxiosCart.resetLoadWaiting();
        });  
    },

    //add to the cart/wishlist from the product details
    addproducttocart_details: function (urladd, formselector) {
        if (this.loadWaiting != false) {
            return;
        }
        this.setLoadWaiting(true);
        // The quick-view modal is always present in the DOM now, so its mere
        // existence no longer means it is open - test for the shown state.
        var quickView = document.querySelector('#ModalQuickView.show');
        var form = quickView
            ? quickView.querySelector('#product-details-form')
            : document.querySelector(formselector);
        if (!form) {
            this.setLoadWaiting(false);
            return;
        }
        var data = new FormData(form);
        axios({
            url: urladd,
            data: data,
            method: 'post',
        }).then(function (response) {
            AxiosCart.success_process(response); 
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            if (typeof window.vmwishlist !== 'undefined') {
                window.vmwishlist.getModel();
            }
            if (typeof window.vmorder !== 'undefined') {
                window.vmorder.getModel();
            }
            AxiosCart.resetLoadWaiting();
        });
    },

    //update product on cart/wishlist
    updateitem: function (urlupdate, formselector) {
        var model;
        // same rule as addproducttocart_details: the quick-view modal is always
        // in the DOM, so only take its form when it is actually shown
        var quickView = document.querySelector('#ModalQuickView.show');
        var form = quickView
            ? quickView.querySelector('#product-details-form')
            : document.querySelector(formselector);
        if (!form) return;
        var data = new FormData(form);

        if (typeof window.vmwishlist !== 'undefined') {
            model = window.vmwishlist;
        } else {
            model = window.vmorder;
        }

        axios({
            url: urlupdate,
            data: data,
            method: 'post',
        }).then(function (response) {
            if (response.data.success) {
                window.bootstrap.Modal.getOrCreateInstance(document.getElementById('ModalQuickView')).hide();
            } else {
                model.displayWarning(response.data.message, 'danger');
            }
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            model.getModel();
            AxiosCart.resetLoadWaiting();
        });
    },

    //add bid
    addbid: function (urladd, formselector) {
        if (this.loadWaiting != false) {
            return;
        }
        this.setLoadWaiting(true);
        var form = document.querySelector(formselector);
        var data = new FormData(form);
        axios({
            url: urladd,
            data: data,
            method: 'post',
        })
        .then(function (response) {
            AxiosCart.success_process(response);
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            AxiosCart.resetLoadWaiting();
        });  
    },
    //add a product to compare list
    addproducttocomparelist: function (id, message, url) {
        if (this.loadWaiting != false) {
            return;
        }
        this.setLoadWaiting(true);

        var cookie = this.getCookie('Grand.CompareProduct');
        if (cookie !== '') {
            if (!cookie.includes(id)) {
                cookie = cookie + '|' + id;
            }
        } else {
            cookie = id;
        }
        this.setCookie('Grand.CompareProduct', cookie);
        window.vm.updateCompareProductsQty();

        window.vm.displayBarNotification(message, url, 'success', 3500);

        this.resetLoadWaiting();

        return false;  
    },

    setLoadWaiting: function (display) {
        this.loadWaiting = display;
    },

    success_process: function (response) {
        if (response.data.updatetopwishlistsectionhtml) {
            if (document.querySelector('.wishlist-qty'))
                document.querySelector('.wishlist-qty').innerHTML = response.data.updatetopwishlistsectionhtml;
        }
        if (response.data.sidebarshoppingcartmodel) {
            var newfly = response.data.sidebarshoppingcartmodel;
            window.vm.flycart = newfly;
            window.vm.flycartitems = newfly.Items;
            window.vm.flycartindicator = newfly.TotalProducts;

        }
        if (response.data.updatetopcartsectionhtml) {
            window.vm.flycartindicator = response.data.updatetopcartsectionhtml;
        }
        if (response.data.product) {
            if (response.data.success == true) {

                window.vm.PopupQuickViewVueModal = response.data.model;

                Object.assign(window.vm.PopupQuickViewVueModal, { RelatedProducts: [] });

                window.bootstrap.Modal.getOrCreateInstance(document.getElementById('ModalQuickView')).show();

                if (response.data.model.ProductType == 20) {

                    var fullDate = new Date(response.data.model.StartDate).toLocaleDateString('en-US');
                    var year = new Date(response.data.model.StartDate).getFullYear();
                    var month = new Date(response.data.model.StartDate).getUTCMonth() + 1;

                    Object.assign(window.vm.PopupQuickViewVueModal, { ReservationFullDate: fullDate });
                    Object.assign(window.vm.PopupQuickViewVueModal, { ReservationYear: year });
                    Object.assign(window.vm.PopupQuickViewVueModal, { ReservationMonth: month });

                }

            }
        }
        if (response.data.message) {
            if (response.data.success == true) {
                //success
                window.vm.PopupAddToCartVueModal = response.data.model;
                window.bootstrap.Modal.getOrCreateInstance(document.getElementById('ModalQuickView')).hide();
                window.bootstrap.Modal.getOrCreateInstance(document.getElementById('ModalAddToCart')).show();
                if (response.data.refreshreservation == true) {
                    var dropdown = document.querySelector("#parameterDropdown");
                    var param = dropdown ? dropdown.value : "";
                    window.Reservation.fillAvailableDates(window.Reservation.currentYear, window.Reservation.currentMonth, param, true);
                }

            }
            else {
                //error
                window.vm.displayBarNotification(response.data.message, '', 'error', 3500);
            }
            return false;
        }
        if (response.data.redirect) {
            location.href = response.data.redirect;
            return true;
        }
        return false;
    },

    resetLoadWaiting: function () {
        AxiosCart.setLoadWaiting(false);
    },

    axiosFailure: function () {
        alert('Failed to add the product. Please refresh the page and try one more time.');
    },
    setCookie: function (cname, cvalue, exdays) {
        const d = new Date();
        d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
        let expires = "expires=" + d.toUTCString();
        document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
    },
    getCookie: function (cname) {
        let name = cname + "=";
        let ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) == ' ') {
                c = c.substring(1);
            }
            if (c.indexOf(name) == 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    },
    deleteCookie: function (cname) {
        document.cookie = "" + cname +"=; expires=Thu, 01 Jan 1970 00:00:00 UTC;"
    }
};

/*
 * Cart actions are declared on the markup as data attributes and dispatched
 * from here, instead of every button carrying a hand-written
 * onclick="AxiosCart.someFunction('url', 'id', ...)" string.
 *
 * That old form had to interpolate routes and localized text into JS source at
 * each call site, and the Vue-rendered variants built that source with string
 * concatenation inside a :onclick binding - two levels of escaping for a single
 * click. Delegation also means markup Vue renders later is handled for free.
 *
 *   <button data-cart-action="add"
 *           data-url="/addproducttocart/catalog/123/1"
 *           data-show-qty="true"
 *           data-product-id="123">
 */
document.addEventListener('click', function (event) {
    var el = event.target.closest('[data-cart-action]');
    if (!el) return;

    var d = el.dataset;
    var res = window.grandRes || {};
    var routes = window.grandRoutes || {};

    switch (d.cartAction) {
        case 'add':
            AxiosCart.addproducttocart_catalog(d.url, d.showQty, d.productId);
            break;
        case 'add-details':
            AxiosCart.addproducttocart_details(d.url, d.form);
            break;
        case 'update':
            AxiosCart.updateitem(d.url, d.form);
            break;
        case 'compare':
            AxiosCart.addproducttocomparelist(d.productId, res.compareAddedLink, routes.compareProducts);
            break;
        case 'quickview':
            AxiosCart.quickview_product(d.url);
            break;
        case 'bid':
            AxiosCart.addbid(d.url, d.form);
            break;
        default:
            return;
    }
    event.preventDefault();
});


window.AxiosCart = AxiosCart


window.AxiosCart = AxiosCart
