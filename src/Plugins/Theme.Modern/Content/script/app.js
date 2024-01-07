"use strict";

Vue.use(VueAwesomeSwiper)
var vm = new Vue({
    el: '#app',
    data: function () {
        return {
            show: false,
            fluid: false,
            hover: false,
            darkMode: false,
            active: false,
            NextDropdownVisible: false,
            value: 5,
            searchitems: null,
            searchcategories: null,
            searchbrands: null,
            searchblog: null,
            searchproducts: null,
            flycartfirstload: true,
            PopupAddToCartVueModal: null,
            PopupQuickViewVueModal: null,
            menuToggled: false,
            index: null,
            RelatedProducts: null,
            compareproducts: null,
            compareProductsQty: 0,
            loader: false,
            flyMenuContainer: false,
            topHeaderInfo: {
                swiperOptions: {
                    slidesPerView: 1,
                    autoplay: {
                        delay: '3000'
                    },
                    effect: 'flip',
                    flipEffect: {
                        slideShadows: false,
                    },
                    loop: true,
                    spaceBetween: 10,
                }
            },
            QuickViewSlider: {
                swiperOptions: {
                    slidesPerView: 1,
                    spaceBetween: 10,
                    navigation: {
                        nextEl: '#ModalQuickView .swiper-button-next',
                        prevEl: '#ModalQuickView .swiper-button-prev'
                    }
                },
            },
            QuickViewSliderThumbs: {
                swiperOptions: {
                    spaceBetween: 10,
                    slidesPerView: 5,
                    watchSlidesVisibility: true,
                    watchSlidesProgress: true,
                    slideToClickedSlide: true,
                },
            },
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
        if (localStorage.darkMode == "true") this.darkMode = true;
        this.TopScroll();
        //this.wishindicator = parseInt(this.$refs.wishlistQty.innerText);
        this.updateCompareProductsQty();
        this.backToTop();
        if (991 < window.innerWidth) {
            this.linksContainer();
        }
        window.addEventListener('DOMContentLoaded', () => {
            vm.$forceUpdate();
        });
    },
    created: function () {
        if (location.pathname !== "/") {
            this.menuToggled = false;
        }
    },
    watch: {
        fluid: function (newName) {
            localStorage.fluid = newName;
        },
        darkMode: function (newValue) {
            localStorage.darkMode = newValue;
        },
        PopupQuickViewVueModal: function () {
            vm.getLinkedProductsQV(vm.PopupQuickViewVueModal.Id);
        }
    },
    methods: {
        openMenu(el, mainMenu) {
            var menu = document.getElementById(mainMenu);
            if (menu.classList.contains('show')) {
                menu.classList.remove('show');
                el.classList.remove('show');
            } else {
                menu.classList.add('show');
                el.classList.add('show');
            }
        },
        linksContainer() {
            var ol = document.querySelector('#mainMenu .other-links');
            var lc = document.getElementById('links-container');
            if (ol !== null) {
                lc.prepend(ol)
            }
        },
        menuButton: function () {
            this.menuToggled = !this.menuToggled;
        },
        productImageSlide: function (event) {
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
            var selector = ".product-box-slide";
            var parent = findParentBySelector(yourElm, selector);
            var Image = parent.querySelectorAll(".main-product-img")[0];
            Image.setAttribute('src', Imagesrc);
        },
        slideTo: function (e) {
            vm.$refs.QuickViewSlider.$swiper.slideTo(e.dataset.index);
        },
        TopScroll: function () {
            var body = document.body;
            var scrollUp = "scroll-up";
            var scrollDown = "scroll-down";
            var onTop = "onTop";
            let lastScroll = 0;

            var currentScrollWindow = window.pageYOffset;
            if (currentScrollWindow == 0) {
                body.classList.add(onTop);
            }

            window.addEventListener("scroll", function () {
                var currentScroll = window.pageYOffset;
                if (window.pageYOffset <= 10) {
                    body.classList.add(onTop);
                } else {
                    body.classList.remove(onTop);
                }
                if (currentScroll > lastScroll && !body.classList.contains(scrollDown)) {
                    // down
                    body.classList.remove(scrollUp);
                    if (lastScroll != 0) {
                        body.classList.add(scrollDown);
                        return;
                    } else {
                        body.classList.remove(scrollDown);
                    }
                } else if (currentScroll < lastScroll && body.classList.contains(scrollDown)) {
                    // up
                    body.classList.remove(scrollDown);
                    body.classList.add(scrollUp);
                }
                lastScroll = currentScroll;
            });
        },
        toogleMenu: function () {
            if (this.flyMenuContainer) {
                this.flyMenuContainer = false;
            } else {
                this.flyMenuContainer = true;
            }
        },
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
            new Vue({
                el: '#' + el,
                data: {
                    template: null,
                },
                render: function (createElement) {
                    if (!this.template) {
                        return createElement('b-overlay', {
                            attrs: {
                                show: 'true'
                            }
                        });
                    } else {
                        return this.template();
                    }
                },
                methods: {
                    showModal: function () {
                        this.$refs[el].show()
                    }
                },
                mounted: function () {
                    var self = this;
                    self.template = Vue.compile(html).render;
                    this.darkMode = vm.darkMode;
                },
                updated: function () {
                    this.showModal();
                }
            });
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
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
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
                    'X-Response-View': 'Json'
                }
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
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
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
        showModalOutOfStock: function () {
            this.$refs['out-of-stock'].show()
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
                        if (vm.PopupQuickViewVueModal.ProductPrice.PriceWithDiscount != null)
                            vm.PopupQuickViewVueModal.ProductPrice.PriceWithDiscount = response.data.price;
                        else
                            vm.PopupQuickViewVueModal.ProductPrice.Price = response.data.price;
                    }
                } else {
                    if (response.data.price) {
                        if (vm.PopupQuickViewVueModal.ProductType == 0) {
                            if (vm.PopupQuickViewVueModal.ProductPrice.PriceWithDiscount != null)
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
                    if (response.data.enabledattributemappingids) {
                        for (var i = 0; i < response.data.enabledattributemappingids.length; i++) {
                            document.querySelector('#product_attribute_label_' + response.data.enabledattributemappingids[i]).style.display = "table-cell";
                            document.querySelector('#product_attribute_input_' + response.data.enabledattributemappingids[i]).style.display = "table-cell";
                        }
                    }
                    if (response.data.disabledattributemappingids) {
                        for (var i = 0; i < response.data.disabledattributemappingids.length; i++) {
                            document.querySelector('#product_attribute_label_' + response.data.disabledattributemappingids[i]).style.display = "none";
                            document.querySelector('#product_attribute_input_' + response.data.disabledattributemappingids[i]).style.display = "none";
                        }
                    }
                    /*if (response.data.notAvailableAttributeMappingids) {
                        document.querySelectorAll('[data-disable]').forEach((element) => element.disabled = false);
                        for (var i = 0; i < response.data.notAvailableAttributeMappingids.length; i++) {
                            if (document.querySelectorAll("[data-disable='" + response.data.notAvailableAttributeMappingids[i] + "']").length > 0) {
                                document.querySelectorAll("[data-disable='" + response.data.notAvailableAttributeMappingids[i] + "']")[0].disabled = true;
                            }
                        }
                    }*/
                    if (response.data.pictureDefaultSizeUrl !== null) {
                        if (vm.PopupQuickViewVueModal.PictureModels.length > 1) {
                            const active_img_src = vm.$refs.QuickViewSlider.$swiper.slides[vm.$refs.QuickViewSlider.$swiper.activeIndex].querySelector("img").dataset.srcs;
                            if (!(active_img_src == response.data.pictureDefaultSizeUrl)) {
                                vm.$refs.QuickViewSlider.$swiper.slides.forEach(function (element, index) {
                                    const img_src = element.querySelector('img').dataset.srcs;
                                    if (img_src == response.data.pictureDefaultSizeUrl) {
                                        vm.$refs.QuickViewSlider.$swiper.slideTo(index, 1000, false)
                                    }
                                })
                            }
                        } else {
                            vm.PopupQuickViewVueModal.DefaultPictureModel.ImageUrl = response.data.pictureDefaultSizeUrl;
                        }
                    }
                }
            });
        },
        uploadFile: function (e) {
            var formData = new FormData();
            var imagefile = e;
            var url = imagefile.getAttribute('data-url');
            formData.append("image", qqfile.files[0]);
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
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(function (response) {
                vm.RelatedProducts = response.data;
            });
        },
        warehouse_change_handler(id, url) {
            var data = new FormData();
            data.append('warehouseId', document.getElementById('WarehouseId').value);
            data.append('productId', id);
            axios({
                url: url,
                data: data,
                method: 'post'
            }).then(function (response) {
                if (response.data.stockAvailability) {
                    vm.PopupQuickViewVueModal.StockAvailability = response.data.stockAvailability;
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
