/*
 * Merchandise return request form (was Views/MerchandiseReturn/MerchandiseReturn.cshtml).
 *
 * One `quantity_<itemId>` field per returnable line, which is why the state has
 * to be built from the payload rather than declared literally.
 *
 * With a return method active in the store, the ways of sending the items back
 * depend on the items and reasons chosen, so they are asked for again whenever
 * one of those changes, and the method chosen is let go: its cost may have
 * changed, so the customer chooses again from the new list. The form of an
 * option (e.g. the pickup point to leave the items at) is server-rendered HTML,
 * loaded once the option is chosen; its fields go under the "returnshipping."
 * prefix and are posted with the form.
 *
 * A point left unchosen in that form is caught before the submit: the page the
 * server sends back after refusing it starts again with every quantity at 0.
 */
import { createViewModel } from '../compat/view-model'
import { getRootVm, registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { axios, notifyRequestError } from './shared'

const QUANTITY_FIELD = /^Items\[\d+\]\.Quantity$/

registerView('merchandiseReturn', ({ pickupDate, showPickupDate, itemIds, items, photoReasonIds, photosRequired,
    photosInvalid, nothingChosen, photoTypes, maxPhotoBytes, maxPhotosPerItem, maxPhotosPerSubmit,
    estimateUrl, returnShipping, offersUrl, choosePoint }) => {
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
            formError: '',
            step: 1,
            itemsError: '',
            estimateError: '',
            estimate: null,
            summaryItems: [],
            submitting: false
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

                this.clearChoice()

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
            },
            // no method chosen, and no form of one shown or still loading
            clearChoice() {
                formRequest++
                this.selectedKey = ''
                this.formHtml = ''
                this.formError = ''
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
            // posted once, from the summary: Enter in a field of an earlier step, or a second click, posts nothing
            submit() {
                if (this.step !== 3 || this.submitting) return
                if (returnShipping && !this.formAnswered()) {
                    this.formError = choosePoint ?? ''
                    return
                }
                this.submitting = true
                getRootVm().$refs.form.submit()
            },
            returned() {
                return itemIds.filter(id => Number(this['quantity_' + id]) > 0)
            },
            // an item returned for a reason that asks for a photo has one chosen; the server checks it again
            photosMissing() {
                return this.returned().some(id => {
                    const input = document.getElementById('photos-' + id)
                    const reason = input?.closest('td')?.querySelector('select.return-reasons')?.value
                    return photoReasonIds.includes(reason) && input.files.length === 0
                })
            },
            // the photos the server would refuse, checked before they are uploaded: type, size and count
            photosRefused() {
                const returned = this.returned()
                let total = 0
                return itemIds.some(id => {
                    const files = [...(document.getElementById('photos-' + id)?.files ?? [])]
                    total += files.length
                    return (files.length > 0 && !returned.includes(id)) || files.length > maxPhotosPerItem ||
                        files.some(file => file.size === 0 || file.size > maxPhotoBytes ||
                            !photoTypes.includes(('.' + file.name.split('.').pop()).toLowerCase()))
                }) || total > maxPhotosPerSubmit
            },
            toMethod() {
                if (this.returned().length === 0) this.itemsError = nothingChosen
                else if (this.photosRefused()) this.itemsError = photosInvalid
                else if (this.photosMissing()) this.itemsError = photosRequired
                else this.itemsError = ''
                if (!this.itemsError) this.step = 2
            },
            // the refund the form comes to, worked out on the server as the refund will be; the photos stay out of it
            toSummary() {
                if (!this.formAnswered()) {
                    this.formError = choosePoint ?? ''
                    return
                }
                this.estimateError = ''
                const data = new FormData(getRootVm().$refs.form)
                for (const name of [...data.keys()]) {
                    if (name.startsWith('photos.')) data.delete(name)
                }
                axios.post(estimateUrl, data)
                    .then(response => {
                        const estimate = response.data ?? {}
                        if (estimate.Errors?.length) {
                            this.estimateError = estimate.Errors.join(' ')
                            return
                        }
                        this.estimate = estimate
                        this.summaryItems = this.returned().map(id => ({
                            id,
                            name: items.find(item => item.id === id)?.name ?? '',
                            quantity: this['quantity_' + id]
                        }))
                        this.step = 3
                    })
                    .catch(notifyRequestError)
            },
            back() {
                this.step = Math.max(1, this.step - 1)
            }
        }
    }))
})
