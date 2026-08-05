/*
 * Theme.Modern infinite scroll (was
 * Views/Modern/Catalog/Partials/ScrollPagination.cshtml).
 *
 * Works together with catalog-modern.js: that module appends each new page and
 * clears `stop` once the response is in, which re-arms the scroll handler.
 */
import { createViewModel } from '../compat/view-model'
import { registerView } from './index'

registerView('scrollPagination', () => {
    const vm = createViewModel({
        data: () => ({ stop: false }),
        methods: {
            next() {
                const catalog = window.catalog
                const currentPage = catalog.Model.PagingFilteringContext.PageNumber
                const pages = catalog.Count?.p_numbers
                if (!pages || !pages.length) return

                const lastPage = pages[pages.length - 1]
                if (lastPage <= currentPage) return

                if (typeof AjaxFilter === 'undefined') {
                    const url = new URL(window.location.href)
                    url.searchParams.set('pagenumber', currentPage + 1)
                    catalog.loadProducts(url.href)
                } else {
                    document.getElementById('PageNumber').value = currentPage
                    AjaxFilter.setFilter('pagenumber')
                }
            },
            pageScroll() {
                const container = document.getElementById('catalog-products')
                if (!container) return
                const trigger = container.scrollHeight * 0.3
                const { PageNumber, TotalPages } = window.catalog.Model.PagingFilteringContext

                if (window.scrollY >= trigger && PageNumber < TotalPages && !this.stop) {
                    this.stop = true
                    this.next()
                }
            }
        }
    })

    window.scrollpagination = vm
    window.addEventListener('scroll', () => vm.pageScroll())
})
