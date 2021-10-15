﻿var vm = new Vue({
    el: '#app',
    data: function() {
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
            PopupProductReviewVueModal: null,
            index: null,
            RelatedProducts: null,
        }
    },
    props: {
        flycart: null,
        flycartitems: null,
        flycartindicator: undefined,
        flywish: null,
        wishlistitems: null,
        wishindicator: undefined,
        UpdatedShoppingCartItemId: null
    },
    mounted: function () {
        if (localStorage.fluid == "true") this.fluid = "fluid";
        if (localStorage.fluid == "fluid") this.fluid = "fluid";
        if (localStorage.fluid == "") this.fluid = "false";
        if (localStorage.darkMode == "true") this.darkMode = true;
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
        updateFly: function () {
            axios({
                baseURL: '/Component/Index?Name=SidebarShoppingCart',
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
        updateWishlist: function () {
            axios({
                baseURL: '/wishlist',
                method: 'get',
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(response => (
                this.flywish = response.data,
                this.wishlistitems = response.data.Items,
                this.wishindicator = response.data.Items.length
            ))
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
        formSubmit(e) {
            if (e && e.submitter.dataset.form !== undefined) {
                eval(e.submitter.dataset.form)
            } else {
                vm.$refs.form.submit();
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
                        vm.PopupQuickViewVueModal.ProductPrice.Price = response.data.price;
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
                    if (response.data.buttonTextOutOfStockSubscription) {
                        PopupQuickViewVueModal.StockAvailability = response.data.stockAvailability;
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
                    if (response.data.notAvailableAttributeMappingids) {
                        document.querySelectorAll('[data-disable]').forEach((element) => element.disabled = false);
                        for (var i = 0; i < response.data.notAvailableAttributeMappingids.length; i++) {
                            if (document.querySelectorAll("[data-disable='" + response.data.notAvailableAttributeMappingids[i] + "']").length > 0) {
                                document.querySelectorAll("[data-disable='" + response.data.notAvailableAttributeMappingids[i] + "']")[0].disabled = true;
                            }
                        }
                    }
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
            var data = { productId: id };
            axios({
                url: '/Component/Index',
                method: 'post',
                params: { "name": "RelatedProducts" },
                data: JSON.stringify(data),
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
            var whId = document.getElementById('WarehouseId').value;
            var data = { warehouseId: whId }
            axios({
                url: url + '?productId=' + id,
                data: JSON.stringify(data),
                params: { product: id },
                method: 'post',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
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
        addProductReview: function (url) {
            axios({
                url: url,
                method: 'get',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(function (response) {
                vm.PopupProductReviewVueModal = response.data;
                vm.$refs['ModalProductReview'].show();
            });
        },
        modalReviewShown: function () {
            if (vm.PopupProductReviewVueModal.AddProductReview.DisplayCaptcha && document.querySelector("#ModalProductReview .captcha-box") == null) {
                var html = document.getElementById("captcha-box");
                document.getElementById("captcha-popup").prepend(html);
            }
            vm.PopupProductReviewVueModal = Object.assign({}, vm.PopupProductReviewVueModal, { ReviewTitle: '', ReviewText: '' })
        },
        modalReviewClose: function () {
            if (vm.PopupProductReviewVueModal.AddProductReview.DisplayCaptcha && document.querySelector("#ModalProductReview .captcha-box") !== null) {
                var html = document.getElementById("captcha-box");
                document.getElementById("captcha-container").prepend(html);
            }
        },
        submitProductReview: function () {
            var form = document.getElementById("addReviewForm");
            var url = form.getAttribute("action");
            var resultTitle = form.getAttribute("data-title");
            var bodyFormData = new FormData(form);
            axios({
                method: "post",
                url: url,
                data: bodyFormData,
                headers: {
                    "Content-Type": "multipart/form-data",
                    'X-Response-View': 'Json'
                },
            }).then(function (response) {
                vm.PopupProductReviewVueModal = response.data;
                productreviews.Model = response.data.Items;

                var result = response.data.AddProductReview.Result;
                var variant = "";

                if (response.data.AddProductReview.SuccessfullyAdded) {

                    variant = "info";
                    vm.$refs['ModalProductReview'].hide();

                } else {
                    variant = "danger";
                }

                new Vue({
                    el: ".modal-place",
                    methods: {
                        toast() {
                            this.$bvToast.toast(result, {
                                title: resultTitle,
                                variant: variant,
                                autoHideDelay: 3000,
                                solid: true
                            })
                        }
                    },
                    mounted: function () {
                        this.toast();
                    }
                });
            });
            return
        }
    },
});
