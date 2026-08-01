/*
 * Catalog listing view-model for Theme.Modern (was
 * Views/Modern/Catalog/Partials/ModelScript.cshtml).
 *
 * Differs from the default theme in three ways, which is why it is a separate
 * module rather than an option on catalog.js:
 *  - pages append instead of replacing, feeding the infinite scroll in
 *    scroll-pagination.js;
 *  - it hands paging over to the AjaxFilter plugin when that is on the page;
 *  - it publishes `Count.p_numbers`, which scroll-pagination.js reads to know
 *    when it has reached the end.
 *
 * `AjaxFilter` is a global from a separate plugin and may legitimately not be
 * there, hence the typeof guards.
 */
import LegacyVue from '../compat/core'
import { registerView } from './index'
import { applyCatalogModel } from './catalog'
import { axios, notify } from './shared'

registerView('catalogModern', ({ model, res }) => {
    window.catalog = new LegacyVue({
        data: () => ({
            Model: [],
            pager: [],
            Count: []
        }),
        created() {
            this.Model = model
            // synchronously, before #app renders against it - a Vue 3 watcher
            // would only flush on a microtask, after the first render
            this.refresh(model)
        },
        watch: {
            // still here for anything that assigns Model from outside
            Model(value) {
                this.refresh(value)
            }
        },
        methods: {
            refresh(value) {
                if (!value) return
                applyCatalogModel(this, value)
                this.setCount(0)
            },
            loadProducts(url) {
                const stateUrl = new URL(url, window.location.origin)
                stateUrl.searchParams.delete('timestamp')
                stateUrl.searchParams.delete('pagenumber')

                if (typeof AjaxFilter !== 'undefined') {
                    stateUrl.searchParams.forEach((value, key) => {
                        const field = document.querySelector('#ajaxfilter-form .' + key)
                        if (field) field.value = value
                        AjaxFilter.setFilter(key)
                    })
                } else {
                    this.getResponse(url)
                }

                window.history.replaceState({ path: stateUrl.href }, '', stateUrl.href)
            },
            getResponse(url) {
                const target = new URL(url, window.location.origin)
                target.searchParams.set('timestamp', new Date().getTime())
                return axios.get(target.href, {
                    headers: { Accept: 'application/json', 'X-Response-View': 'Json' }
                }).then(response => {
                    const incoming = response.data
                    const page = incoming.PagingFilteringContext.PageNumber
                    const isNextPage = page > 1 && page !== this.Model.PagingFilteringContext.PageNumber

                    if (isNextPage) {
                        const previous = this.Model.Products
                        this.Model = incoming
                        this.Model.Products = previous.concat(incoming.Products)
                    } else {
                        this.Model = incoming
                    }
                    this.refresh(this.Model)

                    // the infinite-scroll view-model is only on listing pages that
                    // render ScrollPagination
                    if (window.scrollpagination) window.scrollpagination.stop = false
                }).catch(err => {
                    console.error('[grand] catalog request failed', err)
                    notify(res.loadFailed, res.warning)
                })
            },
            goToPage(page) {
                // used to ignore its argument and reload the current URL, so every
                // pager control did the same thing
                const url = new URL(window.location.href)
                url.searchParams.set('pagenumber', page)
                this.loadProducts(url.href)
            },
            setCount(pageSize) {
                const size = pageSize > 0 ? pageSize : 1
                const pages = Math.ceil(this.Model.Products.length / size)
                this.Count = { ...this.Count, p_numbers: Array.from(Array(pages).keys()) }
            }
        }
    })
})
