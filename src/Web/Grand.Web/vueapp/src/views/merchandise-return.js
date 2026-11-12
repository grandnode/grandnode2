/*
 * Merchandise return request form (was Views/MerchandiseReturn/MerchandiseReturn.cshtml).
 *
 * One `quantity_<itemId>` field per returnable line, which is why the state has
 * to be built from the payload rather than declared literally.
 *
 * With a return method active in the store, the ways of sending the items back
 * depend on the items, reasons and actions chosen, so they are asked for again
 * whenever one of those changes. The form of an option (e.g. the pickup point to
 * leave the items at) is server-rendered HTML, loaded once the option is chosen;
 * its fields go under the "returnshipping." prefix and are posted with the form.
 *
 * A point left unchosen in that form is caught before the submit: the page the
 * server sends back after refusing it starts again with every quantity at 0.
 */
import { createViewModel } from '../compat/view-model'
import { getRootVm, registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { axios, notifyRequestError } from './shared'

const QUANTITY_FIELD = /^Items\[\d+\]\.Quantity$/

registerView('merchandiseReturn', ({ pickupDate, showPickupDate, itemIds, returnShipping, offersUrl, choosePoint }) => {
    const quantities = {}
    itemIds.forEach(id => { quantities['quantity_' + id] = '0' })

    // only the answer to the latest request is shown: an earlier one may come back after it
    let offersRequest = 0
    let formRequest = 0

    window.merchandisereturns = registerViewModel('merchandisereturns', createViewModel({
        data: () => ({
            ...(showPickupDate ? { PickupDate: pickupDate ?? '' } : {}),
            newAddress: false,
            ...quantities,
            checkboxes: [],
            offers: [],
            selectedKey: '',
            formHtml: '',
            formError: ''
        }),
        computed: {
            selectedOffer() {
                return this.offers.find(offer => this.offerKey(offer) === this.selectedKey) ?? null
            }
        },
        created() {
            this.isNewAddress()
        },
        methods: {
            allowed(checkbox, element) {
                const target = window.vm.$refs[element]
                if (!target) return
                if (checkbox.checked) target.removeAttribute('disabled')
                else target.setAttribute('disabled', 'disabled')
            },
            isNewAddress() {
                const select = document.getElementById('pickup-address-select')
                this.newAddress = !select || select.value === ''
            },
            offerKey(offer) {
                return offer.ProviderSystemName + '-' + offer.OptionCode
            },
            loadOffers(event) {
                if (!returnShipping || !offersUrl) return
                const form = event?.target?.form
                if (!form) return

                const data = new FormData(form)
                const request = ++offersRequest
                const anyQuantity = [...data.entries()]
                    .some(([name, value]) => QUANTITY_FIELD.test(name) && Number(value) > 0)
                if (!anyQuantity) {
                    this.setOffers([])
                    return
                }

                axios.post(offersUrl, data)
                    .then(response => {
                        if (request !== offersRequest) return
                        this.setOffers(Array.isArray(response.data) ? response.data : [])
                    })
                    .catch(error => {
                        if (request !== offersRequest) return
                        this.setOffers([])
                        notifyRequestError(error)
                    })
            },
            setOffers(offers) {
                this.offers = offers
                // an option still offered stays chosen, with what was filled in its form
                if (!offers.some(offer => this.offerKey(offer) === this.selectedKey)) {
                    this.selectedKey = ''
                    this.formHtml = ''
                    this.formError = ''
                }
            },
            selectOffer(offer) {
                const request = ++formRequest
                this.formHtml = ''
                this.formError = ''
                if (!offer.FormUrl) return

                axios.get(offer.FormUrl)
                    .then(response => {
                        if (request !== formRequest) return
                        this.formHtml = typeof response.data === 'string' ? response.data : ''
                    })
                    .catch(error => {
                        if (request === formRequest) notifyRequestError(error)
                    })
            },
            // the chosen option's form, when it asks to pick one of its radios, has one picked
            formAnswered() {
                if (!this.selectedOffer?.FormUrl) return true
                const radios = [...document.querySelectorAll('.return-shipping-form input[type=radio]')]
                return radios.length === 0 || radios.some(radio => radio.checked)
            },
            submit() {
                if (!this.formAnswered()) {
                    this.formError = choosePoint ?? ''
                    return
                }
                getRootVm().$refs.form.submit()
            }
        }
    }))
})
