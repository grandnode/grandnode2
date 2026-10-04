/*
 * Advanced search block toggle (was the inline script in Views/Catalog/Search.cshtml
 * and its Theme.Modern override).
 */
import { registerView } from '../views/index'
import { delegate, onReady } from './dom'

registerView('advancedSearch', ({ elements }) => {
    const apply = () => {
        const checkbox = document.getElementById(elements.checkboxId)
        const block = document.getElementById(elements.blockId)
        if (!checkbox || !block) return
        block.style.display = checkbox.checked ? 'flex' : 'none'
    }

    delegate('click', '#' + elements.checkboxId, apply)
    onReady(apply)
})
