var vmorder = new Vue({
    data: function () {
        return {
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
            // paymentinfobusy
            paymentBusy: false,
            // shippingbusy
            shippingBusy: false,
            // selectedshipping
            selectedShippingMethod: 0,
            shippingAddressErrors: null,
            billingAddressErrors: null,
            PickUpInStore: false,
            shippingContainer: true,
            validPayment: true,
            previousStep: [],
        }
    },
    methods: {
        saveStep(step, submitter, form) {
            if (this.PickUpInStore && step === 'vShipping') {
                vmorder[step].save();
            } else {
                if (form) {
                    if (step === 'vBilling') {
                        vmorder.BillingAddress = true;
                    } else {
                        vmorder.BillingAddress = false;
                    }
                    vm.$refs[submitter].click();
                } else {
                    vmorder[step].save();
                }
            }
        },
        backStep(step) {
            const last = step[step.length - 1];
            vmorder.Checkout.back();
            vm.$refs[last].click();
            vmorder.previousStep.pop();
        },
        formCheckoutSubmit() {
            if (vmorder.BillingAddress) {
                vmorder.vBilling.save();
                vmorder.shippingAddressErrors = null;
                vmorder.billingAddressErrors = null;
            } else {
                vmorder.vShipping.save();
                vmorder.shippingAddressErrors = null;
                vmorder.billingAddressErrors = null;
            }
        },
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
                            vmorder.paymentBusy = true;
                            vmorder.validPayment = true;
                            document.querySelector(".payment-info-next-step-button").classList.add("disabled");
                            axios({
                                baseURL: '/Component/Index?Name=' + model.PaymentViewComponentName,
                                method: 'get',
                                data: null,
                                headers: {
                                    'Accept': 'application/json',
                                    'Content-Type': 'application/json',
                                }
                            }).then(response => {
                                vmorder.paymentBusy = false;
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

                    vmorder.scrollToSection();

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
                                vmorder.previousStep.push('buttonShipping');
                                vmorder.vShipping.nextStep(response);
                            }
                        }
                        if (response.data.wrong_shipping_address) {
                            vmorder.shippingAddressErrors = response.data.model_state;
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
                            vmorder.previousStep.push('buttonBilling');
                        }
                        if (response.data.wrong_billing_address) {
                            vmorder.billingAddressErrors = response.data.model_state;
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
                                vmorder.previousStep.push('buttonShippingMethod');
                                vmorder.ShippingMethodError = undefined;
                            }
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
                                vmorder.previousStep.push('buttonPaymentMethod');
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
                    if (vmorder.validPayment) {
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
                                vmorder.previousStep.push('buttonPaymentInfo');
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
            vmorder.shippingBusy = true;
            document.querySelector(".shipping-method-next-step-button").classList.add("disabled");
            var url = window.location.origin + '/checkout/GetShippingFormPartialView?shippingOption=' + arg_value;
            axios({
                url: url,
                method: 'post',
            }).then(function (response) {
                vmorder.shippingBusy = false;
                document.getElementById('shipping_form').innerHTML = response.data;
                document.querySelector(".shipping-method-next-step-button").classList.remove("disabled");
            }).then(function () {
                if (document.querySelector('.script-tag')) {
                    runScripts(document.querySelector('.script-tag'))
                }
            });
        },
        scrollToSection() {
            var container = document.getElementById("checkout-steps");
            window.scrollTo({
                top: container.offsetTop,
                left: 0,
                behavior: 'smooth'
            });
        }
    },
    created() {
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
        PickUpInStore() {
            this.shippingContainer = !this.shippingContainer;
        },
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
                if (document.getElementById("shipping-address-select")) {
                    vmorder.vShipping.newAddress(!document.getElementById('shipping-address-select').value);
                }
            }
        },
        vBilling: function () {
            if (this.vBilling !== null) {
                if (document.querySelector("#billing-address-select")) {
                    vmorder.vBilling.newAddress(!document.querySelector('#billing-address-select').value);
                }
            }
        }
    }
});