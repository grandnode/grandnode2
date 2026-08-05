axios.defaults.showLoader = true;

/*
 * The shell: the state and methods the page chrome shares.
 *
 * This used to be `new Vue({ el: '#app' })`, one instance compiling the whole
 * <body> as its template. It is now a plain reactive object; Vue only takes over
 * the elements marked `vue-island` (the header bar, the drawers, the modals,
 * <main>), each of which uses this object as its data.
 */
var vm = Vue.shell({
    data: function () {
        return {
            show: false,
            hover: false,
            // resolved in <head> by Partials/ColorScheme (explicit choice, else the OS
            // preference). Set here and not in mounted() so the watcher does not fire on
            // the initial value - that would persist a choice the visitor never made and
            // stop the page following the system.
            darkMode: typeof window.grandColorSchemeIsDark === 'function' && window.grandColorSchemeIsDark(),
            active: false,
            NextDropdownVisible: false,
            value: 5,
            flycartfirstload: true,
            PopupAddToCartVueModal: null,
            PopupQuickViewVueModal: null,
            index: null,
            RelatedProducts: null,
            compareproducts: null,
            compareProductsQty: 0,
            loader: false,
        }
    },
    props: {
        flycart: null,
        flycartitems: null,
        flycartindicator: undefined,
        flywish: null,
        wishlistitems: null,
        wishindicator: undefined,
        UpdatedShoppingCartItemId: null,      
    },
    mounted: function () {
        if (localStorage.fluid == "true") this.fluid = "fluid";
        if (localStorage.fluid == "fluid") this.fluid = "fluid";
        if (localStorage.fluid == "") this.fluid = "false";
        this.wishindicator = this.readWishlistQty();
        this.updateCompareProductsQty();
        this.backToTop();
    },
    watch: {
        fluid: function (newName) {
            localStorage.fluid = newName;
        },
        darkMode: function (newValue) {
            // fires only when the visitor uses the switch, which is exactly when the
            // choice should become explicit and stop following the OS
            window.grandSetColorScheme(newValue);
        },
        PopupQuickViewVueModal: function () {
            vm.getLinkedProductsQV(vm.PopupQuickViewVueModal.Id);
        }
    },
    created: function () {
        /*
         * The request overlay is driven purely by the .axios-request class now.
         *
         * It used to also set v-cloak on #app, because the loader CSS was keyed off
         * that attribute - which meant the very same rules hid the whole page on
         * first paint. The class was added on every request and never taken off
         * again; only removing v-cloak happened to make the overlay disappear.
         *
         * Concurrent requests are counted, otherwise the first response to come back
         * clears an overlay the others still need.
         */
        let pending = 0;
        const loader = shown => {
            pending = Math.max(0, pending + (shown ? 1 : -1));
            const element = document.querySelector(".page-loader-container");
            if (element) element.classList.toggle("axios-request", pending > 0);
        };

        axios.interceptors.request.use(
            config => {
                if (config.showLoader) loader(true);
                return config;
            },
            error => {
                if (error.config && error.config.showLoader) loader(false);
                return Promise.reject(error);
            }
        );
        axios.interceptors.response.use(
            response => {
                if (response.config.showLoader) loader(false);

                return response;
            },
            error => {
                // a network failure or a cancelled request has no response at all,
                // and reading .config off it threw, leaving the overlay stuck up
                const config = (error.response && error.response.config) || error.config;

                if (config && config.showLoader) loader(false);

                return Promise.reject(error);
            }
        )
    },
    methods: {
        backToTop() {
            if (!document.querySelector('.up-btn')) {
                const upBtn = document.createElement('div');
                const upBtnContent = document.createElement('div');

                upBtn.classList.add('up-btn', 'up-btn__hide');

                function showBtn(num) {
                    if (document.documentElement.scrollTop >= num) {
                        upBtn.classList.remove('up-btn__hide');
                    } else {
                        upBtn.classList.add('up-btn__hide');
                    }
                }

                document.body.append(upBtn);
                upBtn.append(upBtnContent)
                window.addEventListener('scroll', () => {
                    showBtn(400);
                });

                upBtn.addEventListener('click', () => {
                    window.scrollTo({
                        top: 0,
                        behavior: "smooth"
                    });
                });
            }
        },
        newsletterBox(AllowToUnsubscribe, url) {
            let subscribe;
            if (AllowToUnsubscribe) {
                subscribe = this.$refs.newsletterSubscribe.checked
            } else {
                subscribe = true
            }
            var postData = {
                subscribe: subscribe,
                email: document.getElementById("newsletter-email").value
            };
            axios({
                url: url,
                params: postData,
                method: 'post',
            }).then(function (response) {
                let result = response.data.Result;
                let resultCategory = response.data.ResultCategory;
                let showCategories = response.data.Showcategories;
                let success = response.data.Success;
                let variant;

                if (success) {
                    variant = "info";
                } else {
                    variant = "danger";
                }

                vm.$bvToast.toast(result, {
                    variant: variant,
                    autoHideDelay: 3500,
                    solid: true,
                });

                if (showCategories) {
                    vm.displayPopup(resultCategory, 'ModalNewsletterCategory');
                }

            });
        },
        newsletterSubscribeCategory(url) {
            let form = document.getElementById('newsletter-category-method-form');
            let data = new FormData(form);
            axios({
                url: url,
                method: 'post',
                data: data,
            }).then(function (response) {
                if (!response.data.Success) {
                    alert(response.data.Message);
                }
            }).catch(function (error) {
                alert(error);
            })
        },
        getPrivacyPreference(href) {
            axios({
                url: href,
                method: 'get',
            }).then(function (response) {
                vm.displayPopup(response.data.html, 'ModalPrivacyPreference')
            }).catch(function (error) {
                alert(error);
            });
        },
        savePrivacyPreference(href) {
            let form = document.getElementById('frmPrivacyPreference');
            let data = new FormData(form);
            axios({
                url: href,
                method: 'post',
                data: data
            }).catch(function (error) {
                alert(error);
            });
        },
        displayPopup(html, el) {
            var container = document.getElementById(el);
            if (!container) {
                container = document.createElement('div');
                container.id = el;
                document.body.appendChild(container);
            }
            if (!vm._popupApps) vm._popupApps = {};
            if (vm._popupApps[el]) {
                vm._popupApps[el].unmount();
                delete vm._popupApps[el];
            }
            var popup = Vue.createApp({
                template: html,
                data: function () {
                    return {
                        darkMode: vm.darkMode
                    }
                },
                mounted: function () {
                    var self = this;
                    this.$nextTick(function () {
                        var modal = self.$refs[el];
                        if (modal && modal.show) modal.show();
                    });
                }
            });
            popup.mount(container);
            vm._popupApps[el] = popup;
        },
        displayBarNotification(message, url, messagetype, timeout) {
            var variant;

            if (messagetype == 'error') {
                variant = "danger";
            } else {
                variant = "info";
            }

            this.$bvToast.toast(message, {
                title: messagetype,
                variant: variant,
                href: url,
                autoHideDelay: timeout,
                solid: true
            })
        },
        deletecartitem: function (href) {
            axios({
                method: "post",
                baseURL: href
            }).then(function (response) {
                const newfly = response.data.sidebarshoppingcartmodel;
                vm.flycart = newfly;
                vm.flycartitems = newfly.Items;
                vm.flycartindicator = newfly.TotalProducts;
            }).catch(function (error) {
                alert(error);
            });
            return false;
        },
        /*
         * Seeds the wishlist counter from the server-rendered header.
         *
         * The element only exists when the wishlist is enabled for the current
         * customer, so reading the ref unguarded threw a TypeError and aborted the
         * rest of mounted() - updateCompareProductsQty() and backToTop() never ran.
         *
         * The number comes from data-qty rather than the element text, which is a
         * localized template - Wishlist.HeaderQuantity is "{0}" by default, but a
         * store that decorates it ("(0)", "0 items") would feed parseInt something
         * it cannot read.
         */
        readWishlistQty: function () {
            const el = this.$refs.wishlistQty;
            if (!el) return undefined;
            const qty = parseInt(el.dataset.qty, 10);
            return isNaN(qty) ? undefined : qty;
        },
        updateCompareProductsQty: function () {
            const cookie = AxiosCart.getCookie('Grand.CompareProduct');
            if (cookie !== '') {
                const qty = cookie.split('|').filter(Boolean).length;
                this.compareProductsQty = qty;
            } else {
                this.compareProductsQty = 0;
            }
        },
        updateSidebarShoppingCart: function (url) {
            axios({
                baseURL: url,
                method: 'get',
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                showLoader: false
            }).then(response => (
                this.flycart = response.data,
                this.flycartitems = response.data.Items,
                this.flycartindicator = response.data.TotalProducts,
                this.flycartfirstload = false
            ))
        },
        updateWishlist: function (url) {
            axios({
                baseURL: url,
                method: 'get',
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',                    
                },
                showLoader: false
            }).then(response => (
                this.loader = false,
                this.flywish = response.data,
                this.wishlistitems = response.data.Items,
                this.wishindicator = response.data.Items.length
            ))
        },
        getCompareList: function (url) {
            this.loader = true;
            axios({
                baseURL: url,
                method: 'get',
                params: {
                    t: new Date().getTime()
                },
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                showLoader: false
            }).then(response => {
                this.loader = false;
                this.compareproducts = response.data
            })
        },
        removeFromCompareList: function (product, index) {
            if (product !== undefined) {
                const compareList = AxiosCart.getCookie('Grand.CompareProduct');
                const newCompareList = compareList.replace(product, '');
                AxiosCart.setCookie('Grand.CompareProduct', newCompareList);
                vm.compareproducts.Products.splice(index, 1);
            } else {
                AxiosCart.setCookie('Grand.CompareProduct', '');
                vm.compareproducts.Products.splice(0);
            }
            this.updateCompareProductsQty();
        },
        productImage: function (event) {
            var Imagesrc = event.target.parentElement.getAttribute('data-href');
            function collectionHas(a, b) {
                for (var i = 0, len = a.length; i < len; i++) {
                    if (a[i] == b) return true;
                }
                return false;
            }
            function findParentBySelector(elm, selector) {
                var all = document.querySelectorAll(selector);
                var cur = elm.parentNode;
                while (cur && !collectionHas(all, cur)) {
                    cur = cur.parentNode;
                }
                return cur;
            }

            var yourElm = event.target
            var selector = ".product-box";
            var parent = findParentBySelector(yourElm, selector);
            var Image = parent.querySelectorAll(".main-product-img")[0];
            Image.setAttribute('src', Imagesrc);
        },
        formSubmit() {
            vm.$refs.form.submit();
        },
        formSubmitParam(e, observer) {
            if (e && observer) {
                observer.validate().then(success => {
                    if (!success) {
                        return
                    } else {
                        var submitter = e.target.querySelector('[type="submit"]');
                        eval(submitter.dataset.form)
                    }
                });
            }
        },
        isMobile: function () {
            return (typeof window.orientation !== "undefined") || (navigator.userAgent.indexOf('IEMobile') !== -1);
        },
        attrchange: function (productId, loadPicture) {
            var form = document.getElementById('product-details-form');
            var data = new FormData(form);
            var pId;

            if (vm.PopupQuickViewVueModal.ProductBundleModels.length > 0) {
                pId = vm.PopupQuickViewVueModal.Id;
            } else {
                pId = productId;
            }

            axios({
                url: '/product/productdetails_attributechange?productId=' + pId + '&loadPicture=' + loadPicture,
                data: data,
                method: 'post',
                params: { product: pId },
            }).then(function (response) {
                if (vm.PopupQuickViewVueModal.ProductBundleModels.length > 0) {
                    if (response.data.price) {
                        vm.PopupQuickViewVueModal.ProductPrice.Price = response.data.price;
                    }
                } else {
                    if (response.data.price) {
                        if (vm.PopupQuickViewVueModal.ProductType == 0) {
                            if(vm.PopupQuickViewVueModal.ProductPrice.PriceWithDiscount!=null)
                                vm.PopupQuickViewVueModal.ProductPrice.PriceWithDiscount = response.data.price;
                            else
                                vm.PopupQuickViewVueModal.ProductPrice.Price = response.data.price;
                        } else {
                            vm.PopupQuickViewVueModal.AssociatedProducts.find(x => x.Id === pId).ProductPrice.Price = response.data.price;
                        }
                    }
                    if (response.data.sku) {
                        vm.PopupQuickViewVueModal.Sku = response.data.sku;
                    }
                    if (response.data.mpn) {
                        vm.PopupQuickViewVueModal.Mpn = response.data.mpn;
                    }
                    if (response.data.gtin) {
                        vm.PopupQuickViewVueModal.Gtin = response.data.gtin;
                    }
                    if (response.data.stockAvailability) {
                        vm.PopupQuickViewVueModal.StockAvailability = response.data.stockAvailability;
                    }
                    /*
                     * Conditional attributes: show the rows this choice unlocks, hide the
                     * ones it rules out.
                     *
                     * Scoped to the modal, and null-guarded. Both matter: the product page
                     * behind the quick view renders the *same* element ids, so a bare
                     * document.querySelector returned the page's row and left the modal's
                     * untouched; and when neither exists the unguarded `.style` threw,
                     * which aborted the rest of this handler.
                     */
                    var modal = document.getElementById('ModalQuickView');
                    var setRows = function (ids, display) {
                        for (var i = 0; i < (ids || []).length; i++) {
                            var label = modal && modal.querySelector('#product_attribute_label_' + ids[i]);
                            var input = modal && modal.querySelector('#product_attribute_input_' + ids[i]);
                            if (label) label.style.display = display;
                            if (input) input.style.display = display;
                        }
                    };
                    setRows(response.data.enabledattributemappingids, "table-cell");
                    setRows(response.data.disabledattributemappingids, "none");
                    /*if (response.data.notAvailableAttributeMappingids) {
                        document.querySelectorAll('[data-disable]').forEach((element) => element.disabled = false);
                        for (var i = 0; i < response.data.notAvailableAttributeMappingids.length; i++) {
                            if (document.querySelectorAll("[data-disable='" + response.data.notAvailableAttributeMappingids[i] + "']").length > 0) {
                                document.querySelectorAll("[data-disable='" + response.data.notAvailableAttributeMappingids[i] + "']")[0].disabled = true;
                            }
                        }
                    }*/
                    if (response.data.pictureDefaultSizeUrl !== null) {
                        vm.PopupQuickViewVueModal.DefaultPictureModel.ImageUrl = response.data.pictureDefaultSizeUrl;
                    }
                }
            });
        },
        uploadFile: function (e) {
            var formData = new FormData();
            var imagefile = e;
            var url = imagefile.getAttribute('data-url');
            formData.append("file", e.files[0]);
            axios.post(url, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data'
                }
            }).then(function (response) {
                if (response.data.success) {
                    var message = response.data.message;
                    var downloadGuid = response.data.downloadGuid;
                    var downloadUrl = response.data.downloadUrl;
                    var downloadBtn = document.querySelector('.download-file');
                    var messageContainer = document.getElementById('download-message');

                    e.setAttribute('qq-button-id', downloadGuid);
                    document.querySelector('.hidden-upload-input').value = downloadGuid;

                    messageContainer.style.display = "block";
                    messageContainer.classList.remove('alert-danger');
                    messageContainer.classList.add('alert-info');
                    messageContainer.innerText = message;

                    downloadBtn.style.display = "block";
                    downloadBtn.children[0].setAttribute('href', downloadUrl);

                } else {
                    var message = response.data.message;
                    var messageContainer = document.getElementById('download-message');
                    messageContainer.style.display = "block";
                    messageContainer.classList.remove('alert-info');
                    messageContainer.classList.add('alert-danger');
                    messageContainer.innerText = message;
                }
            })
        },
        initReservationQV: function () {
            if (vm.PopupQuickViewVueModal !== null && vm.PopupQuickViewVueModal.ProductType == 20) {
                var productId = vm.PopupQuickViewVueModal.Id;
                var fullDate = vm.PopupQuickViewVueModal.ReservationFullDate;
                var year = vm.PopupQuickViewVueModal.ReservationYear;
                var month = vm.PopupQuickViewVueModal.ReservationMonth;
                Reservation.init(fullDate, year, month, "No available reservations", "/Product/GetDatesForMonth", productId, "/product/productdetails_attributechange?productId=" + productId);
            }
        },
        getLinkedProductsQV: function (id) {
            axios({
                url: '/Product/RelatedProducts',
                method: 'get',
                params: { "productId": id },
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                }
            }).then(function (response) {
                vm.RelatedProducts = response.data;
            });
        },
        warehouse_change_handler(id, url) {
            //scoped to the modal: the product page behind it uses the same
            //element id, and getElementById would return that one instead
            var select = document.querySelector('#ModalQuickView #WarehouseId');
            if (!select) return;
            var data = new FormData();
            data.append('warehouseId', select.value);
            data.append('productId', id);
            axios({
                url: url,
                data: data,
                method: 'post'
            }).then(function (response) {
                if (response.data.stockAvailability) {
                    vm.PopupQuickViewVueModal.StockAvailability = response.data.stockAvailability;
                }
                //an attribute product prices per warehouse too, so let the
                //attribute handler refresh price and availability together
                if (vm.PopupQuickViewVueModal.ProductAttributes
                    && vm.PopupQuickViewVueModal.ProductAttributes.length > 0) {
                    vm.attrchange(id, true);
                }
            })
        },
        formatDate(date) {
            var d = new Date(date),
                month = '' + (d.getMonth() + 1),
                day = '' + d.getDate(),
                year = d.getFullYear();

            if (month.length < 2)
                month = '0' + month;
            if (day.length < 2)
                day = '0' + day;

            return [month, day, year].join('/');
        },
        QuickViewShown: function () {
            if (vm.PopupQuickViewVueModal.ProductAttributes.length > 0) {
                vm.attrchange(vm.PopupQuickViewVueModal.Id, true)
            } else {
                var bundleProducts = vm.PopupQuickViewVueModal.ProductBundleModels;
                if (bundleProducts.length > 0) {
                    vm.attrchange(vm.PopupQuickViewVueModal.Id, true)
                }
            }
            if (vm.PopupQuickViewVueModal.ProductType == 20) {
                var StartDate;
                var EndDate;
                if (vm.PopupQuickViewVueModal.IntervalUnit == 10) {
                    if (vm.PopupQuickViewVueModal.RentalStartDateUtc !== null) {
                        StartDate = this.formatDate(vm.PopupQuickViewVueModal.RentalStartDateUtc);
                        vm.PopupQuickViewVueModal.RentalStartDateUtc = StartDate;
                    }
                    if (vm.PopupQuickViewVueModal.RentalEndDateUtc !== null) {
                        EndDate = this.formatDate(vm.PopupQuickViewVueModal.RentalEndDateUtc);
                        vm.PopupQuickViewVueModal.RentalEndDateUtc = EndDate;
                    }
                } else {
                    if (vm.PopupQuickViewVueModal.RentalStartDateUtc !== null) {
                        vm.PopupQuickViewVueModal.RentalStartDateUtc = this.formatDate(vm.PopupQuickViewVueModal.RentalStartDateUtc);
                    } else {
                        vm.PopupQuickViewVueModal.RentalStartDateUtc = null;
                    }
                }
            }
        },

    },
});
