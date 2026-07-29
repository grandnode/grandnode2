/*
 * BootstrapVue 2 compatible directives: v-b-toggle, v-b-modal, v-b-tooltip.
 * Targets are taken from directive modifiers (v-b-toggle.some-id) or value.
 */
import { toggleTarget, onToggleState, $bvModal, showTooltip, hideTooltip } from './bv-services'

function targetsOf(binding) {
    const targets = Object.keys(binding.modifiers || {})
    if (typeof binding.value === 'string' && binding.value) targets.push(binding.value)
    if (Array.isArray(binding.value)) targets.push(...binding.value)
    return targets
}

export const vBToggle = {
    mounted(el, binding) {
        const targets = targetsOf(binding)
        el.__bvToggleTargets = targets
        el.classList.add('collapsed')
        el.setAttribute('aria-expanded', 'false')
        el.__bvToggleClick = e => {
            e.preventDefault()
            targets.forEach(toggleTarget)
        }
        el.addEventListener('click', el.__bvToggleClick)
        targets.forEach(id => onToggleState(id, shown => {
            el.classList.toggle('collapsed', !shown)
            el.classList.toggle('not-collapsed', shown)
            el.setAttribute('aria-expanded', shown ? 'true' : 'false')
        }))
    },
    unmounted(el) {
        el.removeEventListener('click', el.__bvToggleClick)
    }
}

export const vBModal = {
    mounted(el, binding) {
        const targets = targetsOf(binding)
        el.__bvModalClick = e => {
            e.preventDefault()
            targets.forEach(id => $bvModal.show(id))
        }
        el.addEventListener('click', el.__bvModalClick)
    },
    unmounted(el) {
        el.removeEventListener('click', el.__bvModalClick)
    }
}

const placements = ['top', 'bottom', 'left', 'right']

export const vBTooltip = {
    mounted(el, binding) {
        const mods = binding.modifiers || {}
        const placement = placements.find(p => mods[p]) || 'top'
        const html = !!mods.html
        const getTitle = () => {
            if (typeof binding.value === 'string' && binding.value) return binding.value
            if (binding.value && typeof binding.value === 'object' && binding.value.title) return binding.value.title
            return el.getAttribute('data-bs-original-title') || ''
        }
        const title = el.getAttribute('title')
        if (title) {
            el.setAttribute('data-bs-original-title', title)
            el.removeAttribute('title')
        }
        el.__bvTipShow = () => showTooltip(el, getTitle(), placement, html)
        el.__bvTipHide = hideTooltip
        el.addEventListener('mouseenter', el.__bvTipShow)
        el.addEventListener('mouseleave', el.__bvTipHide)
        el.addEventListener('click', el.__bvTipHide)
    },
    unmounted(el) {
        el.removeEventListener('mouseenter', el.__bvTipShow)
        el.removeEventListener('mouseleave', el.__bvTipHide)
        el.removeEventListener('click', el.__bvTipHide)
    }
}

export function registerBvDirectives(app) {
    app.directive('bToggle', vBToggle)
    app.directive('bModal', vBModal)
    app.directive('bTooltip', vBTooltip)
}
