/*
 * Catalog listing view-model for the default theme (was
 * Views/Catalog/Partials/ModelScript.cshtml). Theme.Modern pages a different
 * way and has its own module - see catalog-modern.js, which reuses the pager
 * and filter-grouping helpers exported here.
 *
 * Drives paging, sorting and the specification filters on category, brand,
 * collection, tag, vendor and search pages. Published as the global `catalog`
 * because those Razor templates iterate `catalog.Model.Products` directly.
 *
 * Category pages render their first page of products on the server (see
 * CategoryLayout.GridOrLines.cshtml). This module does not draw that first
 * render: it only takes the list over once `clientRendered` flips, on the first
 * successful sort/filter/page request.
 */
import { createViewModel } from '../compat/view-model'
import { registerView } from './index'
import { axios, notify } from './shared'

/** Page numbers to show either side of the current one. */
const PAGER_RADIUS = 4

export function pagerRange(paging) {
    const from = Math.max(1, paging.PageNumber - PAGER_RADIUS)
    const to = paging.PageNumber + PAGER_RADIUS < paging.TotalPages
        ? paging.PageNumber + PAGER_RADIUS
        : paging.TotalPages
    const pages = []
    for (let i = from; i <= to; i++) pages.push(i)
    return pages
}

const FILTER_KEY = 'SpecificationAttributeName'

/**
 * Groups the specification filter items by attribute name, which is the shape
 * the filter markup iterates. Idempotent: the transform runs both from the
 * initial assignment and from the Model watcher, and must not re-group what it
 * has already grouped.
 */
export function groupBy(items, key) {
    if (!Array.isArray(items)) return items || {}
    return items.reduce((grouped, item) => {
        (grouped[item[key]] = grouped[item[key]] || []).push(item)
        return grouped
    }, {})
}

/**
 * Brings the pager and the grouped filters in step with a freshly assigned
 * Model.
 *
 * This has to be callable *synchronously*, not only from the watcher: Vue 3
 * watchers flush on a microtask, and the view-models are now built in the same
 * synchronous block that mounts #app. A page whose filters had not been grouped
 * yet rendered `array['Processor'].SpecificationAttributeName` and threw.
 */
export function applyCatalogModel(vm, value) {
    if (!value) return
    const paging = value.PagingFilteringContext
    if (!paging) return

    vm.pager = pagerRange(paging)

    const filter = paging.SpecificationFilter
    if (!filter) return
    filter.NotFilteredItems = groupBy(filter.NotFilteredItems, FILTER_KEY)
    filter.AlreadyFilteredItems = groupBy(filter.AlreadyFilteredItems, FILTER_KEY)
}

registerView('catalog', ({ model, res }) => {
    window.catalog = createViewModel({
        data: () => ({
            Model: [],
            pager: [],
            // The first page of results is rendered by Razor so the products are
            // in the HTML source. The views keep that server markup on screen
            // until this flips, at which point Vue owns the list.
            clientRendered: false
        }),
        created() {
            this.Model = model
            // synchronously, before #app renders against it
            applyCatalogModel(this, model)
        },
        watch: {
            // still here for anything that assigns Model from outside
            Model(value) {
                applyCatalogModel(this, value)
            }
        },
        methods: {
            loadProducts(url) {
                this.getResponse(url)
                // keep the address bar in step with what is on screen, minus the
                // cache-busting timestamp
                const stateUrl = new URL(url, window.location.origin)
                stateUrl.searchParams.delete('timestamp')
                window.history.replaceState({ path: stateUrl.href }, '', stateUrl.href)
            },
            setRating(rating) {
                const url = new URL(window.location.href)
                if (rating === '') {
                    url.searchParams.delete('rating')
                } else {
                    url.searchParams.set('rating', rating)
                }
                url.searchParams.delete('pagenumber')
                this.loadProducts(url.href)
            },
            getResponse(url) {
                const target = new URL(url, window.location.origin)
                target.searchParams.set('timestamp', new Date().getTime())
                return axios.get(target.href, { headers: { Accept: 'application/json' } })
                    .then(response => {
                        this.Model = response.data
                        applyCatalogModel(this, response.data)
                        // Hand the list over to Vue. Only on success: a failed
                        // request must leave the server-rendered page on screen
                        // rather than blank it.
                        this.clientRendered = true
                    })
                    // Silent failure here reads as a dead page: sorting, filtering and
                    // paging simply stop responding with nothing on screen to say why.
                    .catch(err => {
                        console.error('[grand] catalog request failed', err)
                        notify(res.loadFailed, res.warning)
                    })
            },
            goToPage(page) {
                const url = new URL(window.location.href)
                url.searchParams.set('pagenumber', page)
                this.loadProducts(url.href)
                this.scrollToSection()
            },
            scrollToSection() {
                const container = document.getElementById('catalog-products')
                if (!container) return
                window.scrollTo({ top: container.offsetTop, left: 0, behavior: 'smooth' })
            }
        }
    })
})
