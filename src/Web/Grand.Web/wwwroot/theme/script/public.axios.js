
/*
** axios cart implementation
*/
var AxiosCart = {
    loadWaiting: false,
    sidebarcartselector: '.sidebar-cart',

    quickview_product: function (quickviewurl) {
        axios({
            url: quickviewurl,
            method: 'post',
        }).then(function (response) {
            this.AxiosCart.success_process(response);
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function (response) {
            this.AxiosCart.resetLoadWaiting();
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
            this.AxiosCart.success_process(response);
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            if (typeof vmwishlist !== 'undefined') {
                vmwishlist.getModel();
            }
            if (typeof vmorder !== 'undefined') {
                vmorder.getModel();
            }
            this.AxiosCart.resetLoadWaiting();
        });  
    },

    //add to the cart/wishlist from the product details
    addproducttocart_details: function (urladd, formselector) {
        if (this.loadWaiting != false) {
            return;
        }
        this.setLoadWaiting(true);
        if (document.querySelector("#ModalQuickView")) {
            var form = document.querySelector('#ModalQuickView #product-details-form');
        } else {
            var form = document.querySelector('.product-standard #product-details-form');
        }
        var data = new FormData(form);
        axios({
            url: urladd,
            data: data,
            method: 'post',
        }).then(function (response) {
            this.AxiosCart.success_process(response); 
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            if (typeof vmwishlist !== 'undefined') {
                vmwishlist.getModel();
            }
            if (typeof vmorder !== 'undefined') {
                vmorder.getModel();
            }
            this.AxiosCart.resetLoadWaiting();
        });
    },

    //update product on cart/wishlist
    updateitem: function (urlupdate) {
        var model;
        var form = document.querySelector('#ModalQuickView #product-details-form');
        var data = new FormData(form);

        if (typeof vmwishlist !== 'undefined') {
            model = vmwishlist;
        } else {
            model = vmorder;
        }

        axios({
            url: urlupdate,
            data: data,
            method: 'post',
        }).then(function (response) {
            if (response.data.success) {
                vm.$refs['ModalQuickView'].hide();
            } else {
                model.displayWarning(response.data.message, 'danger');
            }
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            model.getModel();
            this.AxiosCart.resetLoadWaiting();
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
            this.AxiosCart.success_process(response);
        }).catch(function (error) {
            error.axiosFailure;
        }).then(function () {
            this.AxiosCart.resetLoadWaiting();
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
        vm.updateCompareProductsQty();

        vm.displayBarNotification(message, url, 'success', 3500);

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
            this.flycart = newfly;
            this.flycartitems = newfly.Items;
            this.flycartindicator = newfly.TotalProducts;
            vm.flycart = newfly;
            vm.flycartitems = newfly.Items;
            vm.flycartindicator = newfly.TotalProducts;

        }
        if (response.data.updatetopcartsectionhtml !== 'undefiend') {
            vm.flycartindicator = response.data.updatetopcartsectionhtml;
        }
        if (response.data.product) {
            if (response.data.success == true) {

                vm.PopupQuickViewVueModal = response.data.model;

                Object.assign(vm.PopupQuickViewVueModal, { RelatedProducts: [] });

                vm.$refs['ModalQuickView'].show();

                if (response.data.model.ProductType == 20) {

                    var fullDate = new Date(response.data.model.StartDate).toLocaleDateString('en-US');
                    var year = new Date(response.data.model.StartDate).getFullYear();
                    var month = new Date(response.data.model.StartDate).getUTCMonth() + 1;

                    Object.assign(vm.PopupQuickViewVueModal, { ReservationFullDate: fullDate });
                    Object.assign(vm.PopupQuickViewVueModal, { ReservationYear: year });
                    Object.assign(vm.PopupQuickViewVueModal, { ReservationMonth: month });

                }

            }
        }
        if (response.data.message) {
            if (response.data.success == true) {
                //success
                vm.PopupAddToCartVueModal = response.data.model;
                vm.$refs['ModalQuickView'].hide();
                vm.$refs['ModalAddToCart'].show();
                if (response.data.refreshreservation == true) {
                    var param = "";
                    if ($("#parameterDropdown").val() != null) {
                        param = $("#parameterDropdown").val();
                    }
                    Reservation.fillAvailableDates(Reservation.currentYear, Reservation.currentMonth, param, true);
                }

            }
            else {
                //error
                vm.displayBarNotification(response.data.message, '', 'error', 3500);
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
