/*
 * Two-column layout sidebar (was the inline script in Shared/_TwoColumns.cshtml
 * in both themes).
 *
 * Below the large breakpoint the left column is moved into the mobile offcanvas
 * and back again on the way up, and the filters toggle is hidden when the
 * column holds nothing but category navigation.
 */
import { registerView } from '../views/index'
import { onReady } from './dom'

const MOBILE_MAX_WIDTH = 991

registerView('twoColumnsSidebar', ({ elements }) => {
    onReady(() => {
        const sidebar = document.querySelector(elements.sidebarSelector)
        const desktopHost = document.getElementById(elements.desktopHostId)
        if (!sidebar || !desktopHost) return

        const toggle = document.getElementById(elements.toggleId)
        const hasMobileContent = Array.prototype.some.call(sidebar.children,
            child => child.id !== elements.categoryNavigationId)
        if (toggle && !hasMobileContent) {
            toggle.classList.remove('d-block')
            toggle.classList.add('d-none')
        }

        const place = () => {
            const width = window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth
            const host = width <= MOBILE_MAX_WIDTH
                ? document.querySelector(elements.mobileHostSelector)
                : desktopHost
            // the offcanvas is not on every page; leaving the column where it is
            // beats throwing
            if (host && sidebar.parentElement !== host) host.appendChild(sidebar)
        }

        place()
        window.addEventListener('resize', place)
    })
})
