/*
 * Product and vendor review lists
 * (were Views/Product/Components/ProductReviews/Default.cshtml and
 *  Views/Catalog/Components/VendorReviews/Default.cshtml).
 *
 * The two files were the same code with "Product" swapped for "Vendor", so the
 * differing names come in on the payload. Method names are kept as they were
 * because the review markup and its modal partial address them directly.
 *
 * Every toast used to be a freshly mounted Vue application on `.modal-place`,
 * one per helpfulness vote - notify() replaces all of them.
 */
import * as bootstrap from 'bootstrap'
import { createViewModel } from '../compat/view-model'
import { registerViewModel } from '../runtime/islands'
import { registerView } from './index'
import { axios, notify, notifyRequestError } from './shared'

registerView('reviews', payload => {
    const {
        name, modalId, methods, addReviewKey, overviewKey, overviewVm,
        helpfulness, res
    } = payload

    const vm = createViewModel({
        data: () => ({
            Model: payload.model,
            rating: null,
            captcha: null
        }),
        methods: {
            [methods.setHelpfulness](url, wasHelpful, reviewId, ownerId, toastTitle) {
                return axios.post(url, null, {
                    params: {
                        [helpfulness.reviewIdParam]: reviewId,
                        [helpfulness.ownerIdParam]: ownerId,
                        washelpful: wasHelpful
                    }
                }).then(response => {
                    document.getElementById('helpfulness-vote-yes-' + reviewId).innerHTML = response.data.TotalYes
                    document.getElementById('helpfulness-vote-no-' + reviewId).innerHTML = response.data.TotalNo
                    notify(response.data.Result, toastTitle, 'info')
                }).catch(error => notifyRequestError(error, res.warning))
            },
            [methods.addReview]() {
                const modal = document.getElementById(modalId)
                /*
                 * The review modal is rendered inside the "Reviews" tab pane, and
                 * an inactive pane is display:none - its descendants measure 0x0.
                 * Opening it from there gives a backdrop over a modal nobody can
                 * see: the page just greys out. The product page hides that
                 * because its trigger lives in the same pane; the vendor page's
                 * "Be the first to review" sits in the rating summary, outside the
                 * tabs, and hit it every time.
                 *
                 * Show the pane first, if there is one to show.
                 */
                const pane = modal?.closest('.tab-pane')
                if (pane && !pane.classList.contains('active')) {
                    const tab = document.querySelector(`[data-bs-target="#${pane.id}"], #${pane.id}-tab`)
                    if (tab) bootstrap.Tab.getOrCreateInstance(tab).show()
                }
                bootstrap.Modal.getOrCreateInstance(modal).show()
            },
            modalReviewShown() {
                const modal = document.getElementById(modalId)
                const add = this.Model[addReviewKey]
                if (add.DisplayCaptcha && !modal.querySelector('.captcha-box') && !add.SuccessfullyAdded) {
                    document.getElementById('captcha-popup').prepend(document.getElementById('captcha-box'))
                }
                this.Model = { ...this.Model, ReviewTitle: '', ReviewText: '' }
            },
            modalReviewClose() {
                const modal = document.getElementById(modalId)
                if (this.Model[addReviewKey].DisplayCaptcha && modal.querySelector('.captcha-box')) {
                    document.getElementById('captcha-container').prepend(document.getElementById('captcha-box'))
                }
            },
            [methods.submitReview]() {
                const form = document.getElementById('addReviewForm')
                const resultTitle = form.getAttribute('data-title')
                return axios.post(form.getAttribute('action'), new FormData(form))
                    .then(response => {
                        this.Model = response.data

                        const added = response.data[addReviewKey].SuccessfullyAdded
                        if (added) {
                            bootstrap.Modal.getOrCreateInstance(document.getElementById(modalId)).hide()
                        }
                        notify(response.data[addReviewKey].Result, resultTitle, added ? 'info' : 'danger')

                        // keep the star summary above the list in step
                        const overview = response.data[overviewKey]
                        if (overview && window[overviewVm]) window[overviewVm].Model = overview
                    })
                    .catch(error => notifyRequestError(error, res.warning))
            }
        }
    })

    window[name] = registerViewModel(name, vm)

    // The modal partial used to carry its own inline script to hook these up.
    // Delegated from the document because Bootstrap's modal events bubble and
    // the modal itself is re-rendered by the root app.
    document.addEventListener('shown.bs.modal', event => {
        if (event.target.id === modalId) vm.modalReviewShown()
    })
    document.addEventListener('hide.bs.modal', event => {
        if (event.target.id === modalId) vm.modalReviewClose()
    })
})
