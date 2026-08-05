"use strict";

/*
 * The shell: the state and methods the page chrome shares. See the note in
 * Grand.Web's theme/script/app.js - this used to be one Vue instance compiling
 * the whole <body>, and is now a reactive object shared by the `vue-island`
 * elements Vue actually mounts on.
 */
var vm = Vue.shell({
    data: function () {
        return {
            show: false,
            fluid: false,
            hover: false,
            // resolved in <head> by Partials/ColorScheme (explicit choice, else the OS
            // preference). Set here and not in mounted() so the watcher does not fire on
            // the initial value - that would persist a choice the visitor never made.
            darkMode: typeof window.grandColorSchemeIsDark === 'function' && window.grandColorSchemeIsDark(),
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
            // Product image gallery (see Pictures.cshtml, which sets
            // window.productGallery before this vm is created). loop mode
            // needs at least loopedSlides real slides to work; with fewer,
            // Swiper's loop math breaks and navigation silently does nothing.
            swiperOptionTop: {
                loop: !!window.productGallery && window.productGallery.pictureCount > 4,
                loopedSlides: 4,
                spaceBetween: 10,
                navigation: {
                    nextEl: '#ppslider .swiper-button-next',
                    prevEl: '#ppslider .swiper-button-prev'
                },
            },
            swiperOptionThumbs: {
                direction: 'vertical',
                loop: !!window.productGallery && window.productGallery.pictureCount > 4,
                loopedSlides: 4,
                spaceBetween: 10,
                centeredSlides: true,
                slidesPerView: 'auto',
                touchRatio: 0.2,
                slideToClickedSlide: true,
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
        this.TopScroll();
        this.wishindicator = this.readWishlistQty();
        this.updateCompareProductsQty();
        this.backToTop();
        if (991 < window.innerWidth) {
            this.linksContainer();
        }
        window.addEventListener('DOMContentLoaded', () => {
            vm.$forceUpdate();
        });
        if (window.productGallery) {
            setTimeout(function () {
                if (!vm.$refs.swiperTop || !vm.$refs.swiperThumbs) return;
                const swiperTop = vm.$refs.swiperTop.$swiper
                const swiperThumbs = vm.$refs.swiperThumbs.$swiper
                swiperTop.controller.control = swiperThumbs
                swiperThumbs.controller.control = swiperTop
            }, 1000)
        }
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
            // fires only when the visitor uses the switch, which is exactly when the
            // choice should become explicit and stop following the OS
            window.grandSetColorScheme(newValue);
        },
        PopupQuickViewVueModal: function () {
            vm.getLinkedProductsQV(vm.PopupQuickViewVueModal.Id);
        },
        // .left-side-container is the element an island mounts on, and Vue never
        // sees the attributes of its own mount point - so the class it used to
        // carry as a v-bind is applied from here. The initial state is rendered
        // by the layout, which keeps the sidebar from flickering open on load.
        menuToggled: function (value) {
            document.querySelectorAll('.left-side-container, #home-page').forEach(function (el) {
                el.classList.toggle('toggled', !value);
            });
        }
    },
    methods: {
        slideToThumb(index) {
            this.$refs.swiperTop.$swiper.slideTo(index)
        },
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
        /*
         * Mounts server-rendered modal markup (privacy preferences, newsletter
         * categories) as an app of its own.
         *
         * It used to render through `Vue.compile`, which the Vue 3 build does not
         * expose - the markup silently never appeared. Passing the HTML as the
         * `template` option compiles it the same way an island does.
         */
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
                    /*
                     * This used to be `this.$refs[el].show()` - a BootstrapVue
                     * `<b-modal ref>` reaching for the component instance. The
                     * partials it renders carry no `ref` at all and Bootstrap 5
                     * modals are plain elements, so the lookup was undefined and
                     * both popups mounted invisibly. Query the modal out of the
                     * container instead: its own id duplicates the container's,
                     * so getElementById would hand back the wrapper.
                     */
                    this.$nextTick(function () {
                        var modal = container.querySelector('.modal');
                        if (modal) bootstrap.Modal.getOrCreateInstance(modal).show();
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
         * customer, so reading the ref unguarded threw and aborted the rest of
         * mounted() - which is why the call had been commented out. The number comes
         * from data-qty rather than the element text, which is a localized template
         * a store is free to decorate ("(0)", "0 items") and parseInt cannot read.
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
                        /*
                         * Slide the gallery to the picture the chosen attribute maps to.
                         *
                         * The swiper is read defensively: inside the quick view it never
                         * finishes initialising (no .swiper-initialized, slides is empty),
                         * so indexing it threw "Cannot read properties of undefined
                         * (reading 'querySelector')" on *every* attribute change - which
                         * aborted this handler and left the picture on the old value. The
                         * single-picture path is the fallback: it swaps the image source
                         * directly, which is what the visitor actually needs to see.
                         */
                        var swiper = vm.$refs.QuickViewSlider && vm.$refs.QuickViewSlider.$swiper;
                        var slides = (swiper && swiper.slides) || [];
                        if (vm.PopupQuickViewVueModal.PictureModels.length > 1 && slides.length) {
                            var active = slides[swiper.activeIndex];
                            var activeImg = active && active.querySelector("img");
                            if (!activeImg || activeImg.dataset.srcs != response.data.pictureDefaultSizeUrl) {
                                Array.prototype.forEach.call(slides, function (element, index) {
                                    var img = element.querySelector('img');
                                    if (img && img.dataset.srcs == response.data.pictureDefaultSizeUrl) {
                                        swiper.slideTo(index, 1000, false)
                                    }
                                })
                            }
                        } else if (vm.PopupQuickViewVueModal.DefaultPictureModel) {
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
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
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
