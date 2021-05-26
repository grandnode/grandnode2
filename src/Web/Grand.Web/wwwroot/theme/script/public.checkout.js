var Order = Vue.extend({
    props: {
        cart: null,
        totals: null,
        checkoutAsGuest: false,
        BillingAddress: false,
        ShippingMethod: false,
        PaymentMethod: false,
        PaymentInfo: false,
        Confirm: false,
        // billing address
        BillingExistingAddresses: null,
        BillingNewAddress: null,
        BillingNewAddressPreselected: null,
        BillingWarnings: null,
        // shipping address
        ShippingAllowPickUpInStore: null,
        ShippingExistingAddresses: null,
        ShippingNewAddress: null,
        ShippingNewAddressPreselected: null,
        ShippingPickUpInStore: null,
        ShippingPickUpInStoreOnly: null,
        ShippingPickupPoints: null,
        ShippingWarnings: null,
        ShippingMethodError: null,
        // shipping method
        ShippingMethods: null,
        ShippingMethodWarnings: null,
        // payment methods
        DisplayLoyaltyPoints: null,
        PaymentMethods: null,
        LoyaltyPointsAmount: null,
        LoyaltyPointsBalance: null,
        LoyaltyPointsEnoughToPayForOrder: null,
        UseLoyaltyPoints: null,
        // payment info
        PaymentViewComponentName: null,
        // confirm order
        MinOrderTotalWarning: null,
        TermsOfServiceOnOrderConfirmPage: null,
        ConfirmWarnings: null,
        // terms of service
        terms: false,
        acceptTerms: false,
        // checkout steps methods
        Checkout: null,
        vShipping: null,
        vBilling: null,
        vShippingMethod: null,
        vPaymentMethod: null,
        vPaymentInfo: null,
        vConfirmOrder: null,
        // paymentinfobussy
        paymentBussy: false,
        // shippingbussy
        shippingBussy: false,
        // selectedshipping
        selectedShippingMethod: 0
    },
    methods: {
        setDisabled(e) {
            var button = e.target;
            button.classList.add('disabled');
            setTimeout(function () {
                button.classList.remove('disabled');
            }, 600);
        },
        vmCheckout() {
            this.Checkout = {
                loadWaiting: false,
                failureUrl: false,

                init: function (failureUrl) {
                    this.loadWaiting = false;
                    this.failureUrl = failureUrl;
                },

                axiosFailure: function () {
                    location = vmorder.Checkout.failureUrl;
                },

                _disableEnableAll: function (element, isDisabled) {
                    var descendants = element.querySelectorAll('*');
                    descendants.forEach(function (d) {
                        if (isDisabled) {
                            d.setAttribute('disabled', 'disabled');
                        } else {
                            d.removeAttribute('disabled');
                        }
                    });

                    if (isDisabled) {
                        element.setAttribute('disabled', 'disabled');
                    } else {
                        element.removeAttribute('disabled');
                    }
                },

                setLoadWaiting: function (step, keepDisabled) {
                    if (step) {
                        if (this.loadWaiting) {
                            this.setLoadWaiting(false);
                        }
                        var container = document.querySelector('#' + step + '-buttons-container');
                        container.classList.add('disabled');
                        container.style.opacity = '0.5';
                        this._disableEnableAll(container, true);
                        document.querySelector('#' + step + '-please-wait').style.display = 'block';
                    } else {
                        if (this.loadWaiting) {
                            var container = document.querySelector('#' + this.loadWaiting + '-buttons-container');
                            var isDisabled = (keepDisabled ? true : false);
                            if (!isDisabled) {
                                container.classList.remove('disabled');
                                container.style.opacity = '1';
                            }
                            this._disableEnableAll(container, isDisabled);
                            document.querySelector('#' + this.loadWaiting + '-please-wait').style.display = 'none';
                        }
                    }
                    this.loadWaiting = step;
                },

                gotoSection: function (section) {
                    section = document.querySelector('#button-' + section);
                    if (section)
                        section.classList.add("allow");
                },

                back: function () {
                    if (this.loadWaiting) return;
                },

                setStepResponse: function (response) {

                    if (response.data.update_section.name) {
                        if (response.data.goto_section == "shipping") {
                            var model = response.data.update_section.model;
                            vmorder.ShippingAllowPickUpInStore = model.AllowPickUpInStore;
                            vmorder.ShippingPickUpInStore = model.PickUpInStore;
                            vmorder.ShippingPickUpInStoreOnly = model.PickUpInStoreOnly;
                            vmorder.ShippingPickupPoints = model.PickupPoints;
                            vmorder.ShippingExistingAddresses = model.ExistingAddresses;
                            vmorder.ShippingNewAddress = model.NewAddress;
                            vmorder.ShippingNewAddressPreselected = model.NewAddressPreselected;
                            vmorder.ShippingWarnings = model.Warnings;
                            vmorder.ShippingAddress = true;
                        }
                        if (response.data.goto_section == "billing") {
                            var model = response.data.update_section.model;
                            vmorder.BillingExistingAddresses = model.ExistingAddresses;
                            vmorder.BillingNewAddress = model.NewAddress;
                            vmorder.BillingNewAddressPreselected = model.NewAddressPreselected;
                            vmorder.BillingWarnings = model.Warnings;
                            vmorder.BillingAddress = true;
                        }
                        if (response.data.goto_section == "shipping_method") {
                            var model = response.data.update_section.model;
                            vmorder.ShippingMethods = model.ShippingMethods;
                            vmorder.ShippingMethodWarnings = model.Warnings;
                            vmorder.ShippingMethod = true;
                            if (model.ShippingMethods.length > 0) {
                                var index;
                                if (vmorder.selectedShippingMethod !== undefined) {
                                    index = vmorder.selectedShippingMethod;
                                } else {
                                    index = 0;
                                }
                                var elem = model.ShippingMethods[index].Name + '___' + model.ShippingMethods[index].ShippingRateProviderSystemName;
                                vmorder.loadPartialView(elem);
                            }
                            vmorder.updateTotals();
                        }
                        if (response.data.goto_section == "payment_method") {
                            var model = response.data.update_section.model;
                            vmorder.DisplayLoyaltyPoints = model.DisplayLoyaltyPoints;
                            vmorder.PaymentMethods = model.PaymentMethods;
                            vmorder.LoyaltyPointsAmount = model.LoyaltyPointsAmount;
                            vmorder.LoyaltyPointsBalance = model.LoyaltyPointsBalance;
                            vmorder.LoyaltyPointsEnoughToPayForOrder = model.LoyaltyPointsEnoughToPayForOrder;
                            vmorder.UseLoyaltyPoints = model.UseLoyaltyPoints;
                            vmorder.PaymentMethod = true;

                            vmorder.updateTotals();

                        }
                        if (response.data.goto_section == "payment_info") {
                            var model = response.data.update_section.model;
                            vmorder.PaymentViewComponentName = model.PaymentViewComponentName;
                            vmorder.PaymentInfo = true;
                            vmorder.paymentBussy = true;
                            document.querySelector(".payment-info-next-step-button").classList.add("disabled");
                            document.querySelector(".payment-info-next-step-button").setAttribute("onclick", "vmorder.vPaymentInfo.save()");
                            axios({
                                baseURL: '/Component/Index?Name=' + model.PaymentViewComponentName,
                                method: 'get',
                                data: null,
                                headers: {
                                    'Accept': 'application/json',
                                    'Content-Type': 'application/json',
                                }
                            }).then(response => {
                                vmorder.paymentBussy = false;
                                var html = response.data;
                                document.querySelector('.payment-info .info').innerHTML = html;
                            }).then(function () {
                                if (document.querySelector('.script-tag-info')) {
                                    runScripts(document.querySelector('.script-tag-info'))
                                }
                            }).then(function () {
                                document.querySelector(".payment-info-next-step-button").classList.remove("disabled");
                            });

                            this.updateOrderSummary(false);
                            vmorder.updateTotals();

                        }
                        if (response.data.goto_section == "confirm_order") {
                            var model = response.data.update_section.model;
                            vmorder.MinOrderTotalWarning = model.MinOrderTotalWarning;
                            vmorder.ConfirmWarnings = model.Warnings;

                            vmorder.Confirm = true;

                            setTimeout(function () {
                                var c_back = document.getElementById('back-confirm_order').getAttribute('onclick');
                                document.getElementById('new-back-confirm_order').setAttribute('onclick', c_back);
                            }, 300);

                            this.updateOrderSummary(true);
                            vmorder.updateTotals();
                        }

                        if (!response.data.wrong_billing_address) {
                            if (!(document.querySelector("#opc-confirm-order").classList.contains('show'))) {
                                vm.$root.$emit('bv::toggle::collapse', 'opc-' + response.data.update_section.name)
                                vmorder.vmresetSteps(document.querySelector('#opc-' + response.data.update_section.name));
                            }
                        }
                    }
                    if (response.data.allow_sections) {
                        response.data.allow_sections.forEach(function (e) {
                            document.querySelector('#button-' + e).classList.add('allow');
                        });
                    }

                    if (document.querySelector("#shipping-address-select")) {
                        vmorder.vShipping.newAddress(!document.querySelector('#shipping-address-select').value);
                    }
                    if (document.querySelector("#billing-address-select")) {
                        vmorder.vBilling.newAddress(!document.querySelector('#billing-address-select').value);
                    }
                    if (response.data.update_section) {
                        vmorder.Checkout.gotoSection(response.data.update_section.name);
                        return true;
                    }
                    if (response.data.redirect) {
                        location.href = response.data.redirect;
                        return true;
                    }
                    return false;
                },
                updateOrderSummary: function (displayOrderReviewData) {
                    axios({
                        baseURL: '/Component/Index?Name=OrderSummary',
                        method: 'post',
                        data: {
                            prepareAndDisplayOrderReviewData: displayOrderReviewData,
                        },
                        headers: {
                            'Accept': 'application/json',
                            'Content-Type': 'application/json',
                            'X-Response-View': 'Json'
                        }
                    }).then(response => {
                        vmorder.cart.OrderReviewData = response.data.OrderReviewData
                    });
                },
            };
        },
        vmShipping() {
            this.vShipping = {
                form: false,
                saveUrl: false,

                init: function (form, saveUrl) {
                    this.form = form;
                    this.saveUrl = saveUrl;
                },

                newAddress: function (isNew) {
                    if (isNew) {
                        this.resetSelectedAddress();
                        document.querySelector('#shipping-new-address-form').style.display = 'block';
                    } else {
                        document.querySelector('#shipping-new-address-form').style.display = 'none';
                    }
                },

                togglePickUpInStore: function (pickupInStoreInput) {
                    if (pickupInStoreInput.checked) {

                        if (document.querySelector('.select-shipping-address'))
                            document.querySelector('.select-shipping-address').style.display = 'none';

                        document.querySelector('#pickup-points-form').style.display = 'block';
                        document.getElementById("BillToTheSameAddress").disabled = true;

                        if (!document.querySelector("#shipping-address-select")) {
                            document.querySelector('#shipping-new-address-form').style.display = 'none';
                        }
                    }
                    else {
                        if (document.querySelector('.select-shipping-address'))
                            document.querySelector('.select-shipping-address').style.display = 'block';

                        document.querySelector('#pickup-points-form').style.display = 'none';
                        document.getElementById("BillToTheSameAddress").disabled = false;

                        if (!document.querySelector("#shipping-address-select")) {
                            document.querySelector('#shipping-new-address-form').style.display = 'block';
                        }

                    }
                },

                resetSelectedAddress: function () {
                    var selectElement = document.querySelector('#shipping-address-select');
                    if (selectElement) {
                        selectElement.value = '';
                    }
                },

                save: function () {
                    if (vmorder.Checkout.loadWaiting != false) return;
                    vmorder.Checkout.setLoadWaiting('shipping');

                    var form = document.querySelector(this.form);
                    var data = new FormData(form);
                    axios({
                        url: this.saveUrl,
                        method: 'post',
                        data: data,
                    }).then(function (response) {
                        if (response.data.goto_section !== undefined) {
                            if (!(response.data.update_section.name == "shipping")) {
                                document.querySelector('#back-' + response.data.goto_section).setAttribute('onclick', 'document.querySelector("#button-shipping").click(); vmorder.Billing = false;');
                                vmorder.vShipping.nextStep(response);
                            }
                        }
                    }).catch(function (error) {
                        error.axiosFailure;
                    }).then(function () {
                        vmorder.vShipping.resetLoadWaiting();
                    });
                },

                resetLoadWaiting: function () {
                    vmorder.Checkout.setLoadWaiting(false);
                },

                nextStep: function (response) {
                    if (response.data.error) {
                        if ((typeof response.data.message) == 'string') {
                            alert(response.data.message);
                        } else {
                            alert(response.data.message.join("\n"));
                        }

                        return false;
                    }
                    vmorder.Checkout.setStepResponse(response);
                }
            };
        },
        vmBilling() {
            this.vBilling = {
                form: false,
                saveUrl: false,

                init: function (form, saveUrl) {
                    this.form = form;
                    this.saveUrl = saveUrl;
                },

                newAddress: function (isNew) {
                    if (isNew) {
                        this.resetSelectedAddress();
                        vmorder.BillingNewAddressPreselected = true;
                        if (document.querySelector('#billing-new-address-form'))
                            document.querySelector('#billing-new-address-form').style.display = 'block';

                    } else {
                        vmorder.BillingNewAddressPreselected = false;
                        if (document.querySelector('#billing-new-address-form'))
                            document.querySelector('#billing-new-address-form').style.display = 'none';
                    }

                },

                resetSelectedAddress: function () {
                    var selectElement = document.querySelector('#billing-address-select');
                    if (selectElement) {
                        selectElement.value = '';
                    }
                },

                save: function () {
                    if (vmorder.Checkout.loadWaiting != false) return;

                    vmorder.Checkout.setLoadWaiting('billing');

                    var form = document.querySelector(this.form);
                    var data = new FormData(form);
                    axios({
                        url: this.saveUrl,
                        method: 'post',
                        data: data,
                    }).then(function (response) {
                        if (document.querySelector('#back-' + response.data.goto_section)) {
                            document.querySelector('#back-' + response.data.goto_section).setAttribute('onclick', 'document.querySelector("#button-billing").click(); vmorder.ShippingMethod = false;');
                        }
                        vmorder.vBilling.nextStep(response);

                    }).catch(function (error) {
                        alert(error);
                    }).then(function () {
                        vmorder.vBilling.resetLoadWaiting();
                    });
                },

                resetLoadWaiting: function () {
                    vmorder.Checkout.setLoadWaiting(false);
                },

                nextStep: function (response) {
                    //ensure that response.wrong_billing_address is set
                    //if not set, "true" is the default value
                    if (typeof response.data.wrong_billing_address == 'undefined') {
                        response.data.wrong_billing_address = false;
                    }

                    if (response.data.error) {
                        if ((typeof response.data.message) == 'string') {
                            alert(response.data.message);
                        } else {
                            alert(response.data.message.join("\n"));
                        }

                        return false;
                    }
                    vmorder.Checkout.setStepResponse(response);
                }
            };
        },
        vmShippingMethod() {
            this.vShippingMethod = {
                form: false,
                saveUrl: false,

                init: function (form, saveUrl) {
                    this.form = form;
                    this.saveUrl = saveUrl;
                },

                validate: function () {
                    var methods = document.getElementsByName('shippingoption');
                    if (methods.length == 0) {
                        alert('Your order cannot be completed at this time as there is no shipping methods available for it. Please make necessary changes in your shipping address.');
                        return false;
                    }

                    for (var i = 0; i < methods.length; i++) {
                        if (methods[i].checked) {
                            return true;
                        }
                    }
                    alert('Please specify shipping method.');
                    return false;
                },

                save: function () {
                    if (vmorder.Checkout.loadWaiting != false) return;
                    if (this.validate()) {
                        vmorder.Checkout.setLoadWaiting('shipping-method');

                        var form = document.querySelector(this.form);
                        var data = new FormData(form);
                        axios({
                            url: this.saveUrl,
                            method: 'post',
                            data: data,
                        }).then(function (response) {
                            if (response.data.error !== undefined) {
                                vmorder.ShippingMethodError = response.data.message;
                            } else {
                                vmorder.ShippingMethodError = undefined;
                            }
                            document.querySelector('#back-' + response.data.goto_section).setAttribute('onclick', 'document.querySelector("#button-shipping-method").click(); vmorder.PaymentMethod = false;');
                            vmorder.vShippingMethod.nextStep(response);
                        }).catch(function (error) {
                            error.axiosFailure;
                        }).then(function () {
                            vmorder.vShippingMethod.resetLoadWaiting();
                        });
                    }
                },

                resetLoadWaiting: function () {
                    vmorder.Checkout.setLoadWaiting(false);
                },

                nextStep: function (response) {
                    if (response.data.error) {
                        if ((typeof response.data.message) == 'string') {
                            alert(response.data.message);
                        } else {
                            alert(response.data.message.join("\n"));
                        }

                        return false;
                    }

                    vmorder.Checkout.setStepResponse(response);
                }
            };
        },
        vmPaymentMethod() {
            this.vPaymentMethod = {
                form: false,
                saveUrl: false,

                init: function (form, saveUrl) {
                    this.form = form;
                    this.saveUrl = saveUrl;
                },

                toggleUseLoyaltyPoints: function (useLoyaltyPointsInput) {
                    if (useLoyaltyPointsInput.checked) {
                        document.querySelector('#payment-method-block').style.display = 'none';
                    }
                    else {
                        document.querySelector('#payment-method-block').style.display = 'block';
                    }
                },

                validate: function () {
                    var methods = document.getElementsByName('paymentmethod');
                    if (methods.length == 0) {
                        alert('Your order cannot be completed at this time as there is no payment methods available for it.');
                        return false;
                    }

                    for (var i = 0; i < methods.length; i++) {
                        if (methods[i].checked) {
                            return true;
                        }
                    }
                    alert('Please specify payment method.');
                    return false;
                },

                save: function () {
                    if (vmorder.Checkout.loadWaiting != false) return;

                    if (this.validate()) {
                        vmorder.Checkout.setLoadWaiting('payment-method');
                        var form = document.querySelector(this.form);
                        var data = new FormData(form);
                        axios({
                            url: this.saveUrl,
                            method: 'post',
                            data: data,
                        }).then(function (response) {
                            if (response.data.goto_section !== undefined) {
                                vmorder.vPaymentMethod.nextStep(response);
                                document.querySelector('#back-' + response.data.goto_section).setAttribute('onclick', 'document.querySelector("#button-payment-method").click(); vmorder.PaymentInfo = false;');
                            }
                            if (response.data.error) {
                                alert(response.data.message);
                            }
                        }).catch(function (error) {
                            error.axiosFailure;
                        }).then(function () {
                            vmorder.vPaymentMethod.resetLoadWaiting();
                        });
                    }
                },

                resetLoadWaiting: function () {
                    vmorder.Checkout.setLoadWaiting(false);
                },

                nextStep: function (response) {
                    if (response.data.error) {
                        if ((typeof response.data.message) == 'string') {
                            alert(response.data.message);
                        } else {
                            alert(response.data.message.join("\n"));
                        }

                        return false;
                    }

                    vmorder.Checkout.setStepResponse(response);
                }
            };
        },
        vmPaymentInfo() {
            this.vPaymentInfo = {
                form: false,
                saveUrl: false,

                init: function (form, saveUrl) {
                    this.form = form;
                    this.saveUrl = saveUrl;
                },

                save: function () {
                    if (vmorder.Checkout.loadWaiting != false) return;

                    vmorder.Checkout.setLoadWaiting('payment-info');
                    var form = document.querySelector(this.form);
                    var data = new FormData(form);

                    axios({
                        url: this.saveUrl,
                        method: 'post',
                        data: data,
                    }).then(function (response) {
                        if (response.data.goto_section !== undefined) {
                            document.querySelector('#back-' + response.data.goto_section).setAttribute('onclick', 'document.querySelector("#button-payment-info").click();vmorder.Confirm = false;');
                            vmorder.vPaymentInfo.nextStep(response);
                        }
                        if (response.data.update_section !== undefined && response.data.update_section.name == 'payment-info') {
                            var model = response.data.update_section.model;
                            vm.PaymentViewComponentName = model.PaymentViewComponentName,
                                vm.PaymentInfo = true;

                            axios({
                                baseURL: '/Component/Form?Name=' + model.PaymentViewComponentName,
                                method: 'post',
                                data: data,
                            }).then(response => {
                                var html = response.data;
                                document.querySelector('.payment-info .info').innerHTML = html;
                            })

                        }

                    }).catch(function (error) {
                        error.axiosFailure;
                    }).then(function () {
                        vmorder.vPaymentInfo.resetLoadWaiting()
                    });
                },

                resetLoadWaiting: function () {
                    vmorder.Checkout.setLoadWaiting(false);
                },

                nextStep: function (response) {
                    if (response.data.error) {
                        if ((typeof response.data.message) == 'string') {
                            alert(response.data.message);
                        } else {
                            alert(response.data.message.join("\n"));
                        }

                        return false;
                    }

                    vmorder.Checkout.setStepResponse(response);
                }
            };
        },
        vmConfirmOrder() {
            this.vConfirmOrder = {
                form: false,
                saveUrl: false,
                isSuccess: false,

                init: function (saveUrl, successUrl) {
                    this.saveUrl = saveUrl;
                    this.successUrl = successUrl;
                },

                save: function () {
                    if (vmorder.Checkout.loadWaiting != false) return;

                    // terms of service
                    var termOfServiceOk = true;
                    if (termOfServiceOk) {
                        vmorder.Checkout.setLoadWaiting('confirm-order');
                        axios({
                            url: this.saveUrl,
                            method: 'post',
                        }).then(function (response) {
                            vmorder.vConfirmOrder.nextStep(response);
                        }).catch(function (error) {
                            error.axiosFailure;
                        }).then(function () {
                            vmorder.vConfirmOrder.resetLoadWaiting()
                        });
                    } else {
                        return false;
                    }
                },

                resetLoadWaiting: function (transport) {
                    vmorder.Checkout.setLoadWaiting(false, vmorder.vConfirmOrder.isSuccess);
                },

                nextStep: function (response) {
                    if (response.data.error) {
                        if ((typeof response.data.message) == 'string') {
                            alert(response.data.message);
                        } else {
                            alert(response.data.message.join("\n"));
                        }

                        return false;
                    }

                    if (response.data.redirect) {
                        vmorder.vConfirmOrder.isSuccess = true;
                        location.href = response.data.redirect;
                        return;
                    }
                    if (response.data.success) {
                        vmorder.vConfirmOrder.isSuccess = true;
                        window.location = vmorder.vConfirmOrder.successUrl;
                    }
                    vmorder.Checkout.setStepResponse(response);
                }
            };
        },
        cartView() {
            document.addEventListener("DOMContentLoaded", function () {
                var body = document.body;
                body.classList.add("cart-view");
            });
        },
        updateCart() {
            axios({
                baseURL: '/Component/Index?Name=OrderSummary',
                method: 'get',
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(response => {
                this.cart = response.data;
            })
        },
        updateTotals() {
            axios({
                baseURL: '/Component/Index?Name=OrderTotals',
                method: 'get',
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(response => {
                this.totals = response.data;
            });
        },
        termsCheck() {
            if (vmorder.cart.MinOrderSubtotalWarning == null) {
                if (this.terms) {
                    vmorder.vConfirmOrder.save();
                    vmorder.acceptTerms = false;
                }
                else {
                    vmorder.acceptTerms = true;
                }
            }
        },
        otherScripts() {
            document.addEventListener("DOMContentLoaded", function () {
                if (document.querySelector("#shipping-address-select")) {
                    vmorder.vShipping.newAddress(!document.querySelector('#shipping-address-select').value);
                }
                if (document.querySelector("#billing-address-select")) {
                    vmorder.vBilling.newAddress(!document.querySelector('#billing-address-select').value);
                }
                if (document.querySelector("#PickUpInStore")) {
                    vmorder.vShipping.togglePickUpInStore(document.querySelector("#PickUpInStore"));
                }

            });
        },
        vmresetSteps(e) {
            var getClosest = function (elem, selector) {
                for (; elem && elem !== document; elem = elem.parentNode) {
                    if (elem.matches(selector)) return elem;
                }
                return null;
            };
            var card = getClosest(e, '.card');
            document.querySelectorAll('.opc > .card').forEach(function (e) { e.classList.remove('active'); });
            card.classList.add('active');
        },
        loadPartialView(arg_value) {
            vmorder.shippingBussy = true;
            document.querySelector(".shipping-method-next-step-button").classList.add("disabled");
            var url = window.location.origin + '/checkout/GetShippingFormPartialView?shippingOption=' + arg_value;
            axios({
                url: url,
                method: 'post',
            }).then(function (response) {
                vmorder.shippingBussy = false;
                document.getElementById('shipping_form').innerHTML = response.data;
                document.querySelector(".shipping-method-next-step-button").classList.remove("disabled");
            }).then(function () {
                if (document.querySelector('.script-tag')) {
                    runScripts(document.querySelector('.script-tag'))
                }
            });
        }
    },
    mounted() {
        this.vmCheckout();
        this.vmShipping();
        this.vmBilling();
        this.vmShippingMethod();
        this.vmPaymentMethod();
        this.vmPaymentInfo();
        this.vmConfirmOrder();
        this.updateCart();
        this.updateTotals();
        this.cartView();
        this.otherScripts();
    },
    watch: {
        terms: function () {
            if (this.terms == true) {
                this.acceptTerms = false;
            }
        },
        Checkout: function () {
            if (this.Checkout !== null) {
                vmorder.Checkout.init('/cart/');
            }
        },
        vShipping: function () {
            if (this.vShipping !== null) {
                vmorder.vShipping.init('#co-shipping-form', '/checkout/SaveShipping/');
                if (document.querySelector("#shipping-address-select")) {
                    vmorder.vShipping.newAddress(!document.querySelector('#shipping-address-select').value);
                }
            }
        },
        vBilling: function () {
            if (this.vBilling !== null) {
                vmorder.vBilling.init('#co-billing-form', '/checkout/SaveBilling/');
                if (document.querySelector("#billing-address-select")) {
                    vmorder.vBilling.newAddress(!document.querySelector('#billing-address-select').value);
                }
            }
        },
        vShippingMethod: function () {
            if (this.vShippingMethod !== null) {
                vmorder.vShippingMethod.init('#co-shipping-method-form', '/checkout/SaveShippingMethod/');
            }
        },
        vPaymentMethod: function () {
            if (this.vPaymentMethod !== null) {
                vmorder.vPaymentMethod.init('#co-payment-method-form', '/checkout/SavePaymentMethod/');
            }
        },
        vPaymentInfo: function () {
            if (this.vPaymentInfo !== null) {
                vmorder.vPaymentInfo.init('#co-payment-info-form', '/checkout/SavePaymentInfo/');
            }
        },
        vConfirmOrder: function () {
            if (this.vConfirmOrder !== null) {
                vmorder.vConfirmOrder.init('/checkout/ConfirmOrder/', '/checkout/completed/');
            }
        },
    }
});
var vmorder = new Order().$mount('#ordersummarypagecart')