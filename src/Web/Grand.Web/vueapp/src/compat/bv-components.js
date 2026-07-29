/*
 * BootstrapVue 2 compatible components rendered with Bootstrap 5 markup.
 * Only the components (and props) actually used by the storefront views are
 * implemented. All of them are registered globally with their kebab-case
 * names so the server-rendered in-DOM templates keep working unchanged.
 */
import { h, nextTick } from 'vue'
import {
    registerModal, unregisterModal,
    registerToggle, unregisterToggle, notifyToggleState,
    showTooltip, hideTooltip
} from './bv-services'

/* ------------------------------- simple ones ------------------------------ */

const BIcon = {
    name: 'BIcon',
    props: {
        icon: { type: String, default: '' },
        variant: { type: String, default: null },
        fontScale: { type: [String, Number], default: null }
    },
    render() {
        return h('i', {
            class: ['b-icon', 'bi', 'bi-' + this.icon, this.variant ? 'text-' + this.variant : ''],
            style: this.fontScale ? { fontSize: this.fontScale + 'em' } : null,
            'aria-hidden': 'true'
        })
    }
}

const BLink = {
    name: 'BLink',
    props: { href: { type: String, default: null }, disabled: Boolean },
    render() {
        return h('a', {
            href: this.href || '#',
            onClick: e => { if (!this.href) e.preventDefault() }
        }, this.$slots.default ? this.$slots.default() : [])
    }
}

const BButton = {
    name: 'BButton',
    props: {
        variant: { type: String, default: 'secondary' },
        size: { type: String, default: null },
        type: { type: String, default: 'button' },
        block: Boolean,
        pill: Boolean,
        disabled: Boolean,
        href: { type: String, default: null }
    },
    render() {
        const classes = ['btn', 'btn-' + this.variant,
            this.size ? 'btn-' + this.size : '',
            this.block ? 'w-100' : '',
            this.pill ? 'rounded-pill' : '']
        const slot = this.$slots.default ? this.$slots.default() : []
        if (this.href) {
            return h('a', { class: classes, href: this.href, role: 'button' }, slot)
        }
        return h('button', { class: classes, type: this.type, disabled: this.disabled }, slot)
    }
}

const simple = (name, tag, classes, extraAttrs) => ({
    name,
    render() {
        return h(tag, { class: classes, ...(extraAttrs || {}) },
            this.$slots.default ? this.$slots.default() : [])
    }
})

const BContainer = {
    name: 'BContainer',
    props: { fluid: { type: [Boolean, String], default: false } },
    render() {
        const cls = this.fluid === true || this.fluid === '' && this.$attrs.fluid !== undefined
            ? 'container-fluid'
            : (typeof this.fluid === 'string' && this.fluid ? 'container-' + this.fluid : (this.fluid ? 'container-fluid' : 'container'))
        return h('div', { class: cls }, this.$slots.default ? this.$slots.default() : [])
    }
}

const BCol = {
    name: 'BCol',
    props: {
        cols: { type: [String, Number], default: null },
        sm: { type: [String, Number], default: null },
        md: { type: [String, Number], default: null },
        lg: { type: [String, Number], default: null },
        xl: { type: [String, Number], default: null }
    },
    render() {
        const cls = []
        if (this.cols) cls.push('col-' + this.cols)
        for (const bp of ['sm', 'md', 'lg', 'xl']) {
            if (this[bp]) cls.push('col-' + bp + '-' + this[bp])
        }
        if (!cls.length) cls.push('col')
        return h('div', { class: cls }, this.$slots.default ? this.$slots.default() : [])
    }
}

const BImg = {
    name: 'BImg',
    props: {
        src: { type: String, default: null },
        alt: { type: String, default: null },
        fluid: Boolean,
        thumbnail: Boolean,
        rounded: Boolean,
        lazy: Boolean
    },
    render() {
        return h('img', {
            src: this.src,
            alt: this.alt,
            loading: this.lazy ? 'lazy' : null,
            class: [this.fluid ? 'img-fluid' : '', this.thumbnail ? 'img-thumbnail' : '',
                this.rounded ? 'rounded' : '']
        })
    }
}

const BImgLazy = {
    ...BImg,
    name: 'BImgLazy',
    render() {
        return h('img', {
            src: this.src,
            alt: this.alt,
            loading: 'lazy',
            class: [this.fluid ? 'img-fluid' : '', this.thumbnail ? 'img-thumbnail' : '',
                this.rounded ? 'rounded' : '']
        })
    }
}

const BCardImgLazy = {
    name: 'BCardImgLazy',
    props: {
        src: { type: String, default: null },
        alt: { type: String, default: null },
        top: Boolean,
        bottom: Boolean
    },
    render() {
        return h('img', {
            src: this.src,
            alt: this.alt,
            loading: 'lazy',
            class: this.top ? 'card-img-top' : (this.bottom ? 'card-img-bottom' : 'card-img')
        })
    }
}

const BCard = {
    name: 'BCard',
    props: { noBody: Boolean, title: { type: String, default: null } },
    render() {
        const content = this.$slots.default ? this.$slots.default() : []
        const body = this.noBody
            ? content
            : [h('div', { class: 'card-body' },
                (this.title ? [h('h5', { class: 'card-title' }, this.title)] : []).concat(content))]
        return h('div', { class: 'card' }, body)
    }
}

const BAlert = {
    name: 'BAlert',
    props: {
        show: { type: [Boolean, Number], default: false },
        variant: { type: String, default: 'info' },
        fade: Boolean,
        dismissible: Boolean
    },
    render() {
        if (!this.show) return null
        return h('div', { class: ['alert', 'alert-' + this.variant], role: 'alert' },
            this.$slots.default ? this.$slots.default() : [])
    }
}

const BSpinner = {
    name: 'BSpinner',
    props: {
        variant: { type: String, default: null },
        label: { type: String, default: null },
        small: Boolean,
        type: { type: String, default: 'border' }
    },
    render() {
        return h('span', {
            class: ['spinner-' + this.type,
                this.small ? 'spinner-' + this.type + '-sm' : '',
                this.variant ? 'text-' + this.variant : ''],
            role: 'status'
        }, this.label ? [h('span', { class: 'visually-hidden' }, this.label)] : [])
    }
}

const BOverlay = {
    name: 'BOverlay',
    props: { show: { type: [Boolean, String], default: false } },
    template: `
        <div class="position-relative">
            <slot></slot>
            <div v-if="show" class="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center" style="background:rgba(255,255,255,.65);z-index:10;min-height:4rem;">
                <span class="spinner-border" role="status"></span>
            </div>
        </div>`
}

/* --------------------------------- modal ---------------------------------- */

const BModal = {
    name: 'BModal',
    inheritAttrs: false,
    props: {
        id: { type: String, default: null },
        title: { type: String, default: null },
        size: { type: String, default: null },
        centered: Boolean,
        scrollable: Boolean,
        hideFooter: Boolean,
        hideHeader: Boolean,
        bodyClass: { type: [String, Array, Object], default: null },
        headerClass: { type: [String, Array, Object], default: null },
        okTitle: { type: String, default: 'OK' }
    },
    emits: ['show', 'shown', 'hide', 'hidden'],
    data() {
        return { visible: false }
    },
    mounted() {
        registerModal(this.id, this)
        this._escHandler = e => { if (e.key === 'Escape' && this.visible) this.hide() }
        document.addEventListener('keydown', this._escHandler)
    },
    beforeUnmount() {
        document.removeEventListener('keydown', this._escHandler)
        unregisterModal(this.id, this)
        if (this.visible) document.body.classList.remove('modal-open')
    },
    methods: {
        show() {
            if (this.visible) return
            this.$emit('show')
            this.visible = true
            document.body.classList.add('modal-open')
            nextTick(() => this.$emit('shown'))
        },
        hide() {
            if (!this.visible) return
            this.$emit('hide')
            this.visible = false
            document.body.classList.remove('modal-open')
            nextTick(() => this.$emit('hidden'))
        },
        toggle() {
            this.visible ? this.hide() : this.show()
        }
    },
    template: `
        <teleport to="body">
            <template v-if="visible">
                <div class="modal fade show d-block" tabindex="-1" :id="id" v-bind="$attrs" @mousedown.self="hide()">
                    <div :class="['modal-dialog', size ? 'modal-' + size : '', centered ? 'modal-dialog-centered' : '', scrollable ? 'modal-dialog-scrollable' : '']">
                        <div class="modal-content">
                            <div v-if="!hideHeader" :class="['modal-header', headerClass]">
                                <slot name="modal-header">
                                    <h5 class="modal-title"><slot name="modal-title">{{ title }}</slot></h5>
                                    <button type="button" class="btn-close" aria-label="Close" @click="hide()"></button>
                                </slot>
                            </div>
                            <div :class="['modal-body', bodyClass]">
                                <slot></slot>
                            </div>
                            <div v-if="!hideFooter" class="modal-footer">
                                <slot name="modal-footer">
                                    <button type="button" class="btn btn-secondary" @click="hide()">{{ okTitle }}</button>
                                </slot>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-backdrop fade show"></div>
            </template>
        </teleport>`
}

/* -------------------------------- collapse -------------------------------- */

const accordionGroups = new Map()

const BCollapse = {
    name: 'BCollapse',
    props: {
        id: { type: String, default: null },
        visible: Boolean,
        accordion: { type: String, default: null },
        appear: Boolean
    },
    emits: ['show', 'shown', 'hide', 'hidden', 'input'],
    data() {
        return { shown: this.visible }
    },
    created() {
        if (this.accordion) {
            if (!accordionGroups.has(this.accordion)) accordionGroups.set(this.accordion, new Set())
            accordionGroups.get(this.accordion).add(this)
        }
    },
    mounted() {
        registerToggle(this.id, this)
    },
    beforeUnmount() {
        unregisterToggle(this.id, this)
        if (this.accordion) accordionGroups.get(this.accordion).delete(this)
    },
    watch: {
        visible(v) {
            this.shown = v
        },
        shown(v) {
            notifyToggleState(this.id, v)
            this.$emit('input', v)
            this.$emit(v ? 'show' : 'hide')
            nextTick(() => this.$emit(v ? 'shown' : 'hidden'))
        }
    },
    methods: {
        isShown() {
            return this.shown
        },
        show() {
            if (this.accordion) {
                accordionGroups.get(this.accordion).forEach(c => { if (c !== this) c.shown = false })
            }
            this.shown = true
        },
        hide() {
            this.shown = false
        },
        toggle() {
            this.shown ? this.hide() : this.show()
        }
    },
    template: `<div :id="id" :class="['collapse', shown ? 'show' : '']"><slot></slot></div>`
}

/* -------------------------------- sidebar --------------------------------- */

const BSidebar = {
    name: 'BSidebar',
    props: {
        id: { type: String, default: null },
        title: { type: String, default: null },
        backdrop: Boolean,
        right: Boolean,
        shadow: Boolean,
        bodyClass: { type: [String, Array, Object], default: null }
    },
    emits: ['shown', 'hidden'],
    data() {
        return { shown: false }
    },
    mounted() {
        registerToggle(this.id, this)
    },
    beforeUnmount() {
        unregisterToggle(this.id, this)
    },
    watch: {
        shown(v) {
            notifyToggleState(this.id, v)
            nextTick(() => this.$emit(v ? 'shown' : 'hidden'))
        }
    },
    methods: {
        isShown() {
            return this.shown
        },
        show() {
            this.shown = true
        },
        hide() {
            this.shown = false
        },
        toggle() {
            this.shown = !this.shown
        }
    },
    template: `
        <teleport to="body">
            <aside :id="id" :class="['b-sidebar', right ? 'b-sidebar-right' : '', shadow ? 'shadow' : '']"
                   :style="{ display: shown ? 'flex' : 'none' }" tabindex="-1">
                <header class="b-sidebar-header">
                    <button type="button" class="btn-close" aria-label="Close" @click="hide()"></button>
                    <strong class="ms-2"><slot name="title">{{ title }}</slot></strong>
                </header>
                <div :class="['b-sidebar-body', bodyClass]">
                    <slot></slot>
                </div>
                <footer v-if="$slots.footer" class="b-sidebar-footer"><slot name="footer"></slot></footer>
            </aside>
            <div v-if="backdrop && shown" class="b-sidebar-backdrop modal-backdrop fade show" @click="hide()"></div>
        </teleport>`
}

/* ---------------------------------- tabs ----------------------------------- */

const BTabs = {
    name: 'BTabs',
    props: {
        contentClass: { type: [String, Array, Object], default: null },
        align: { type: String, default: null }
    },
    data() {
        return { tabs: [] }
    },
    provide() {
        return { bTabs: this }
    },
    methods: {
        registerTab(tab) {
            this.tabs.push(tab)
            if (tab.localActive && this.tabs.filter(t => t.localActive).length > 1) {
                this.tabs.forEach(t => { if (t !== tab) t.localActive = false })
            }
            if (!this.tabs.some(t => t.localActive)) tab.localActive = true
        },
        unregisterTab(tab) {
            const i = this.tabs.indexOf(tab)
            if (i > -1) this.tabs.splice(i, 1)
        },
        selectTab(tab) {
            this.tabs.forEach(t => { t.localActive = (t === tab) })
            tab.$emit('click')
        }
    },
    template: `
        <div class="tabs">
            <ul :class="['nav', 'nav-tabs', align === 'center' ? 'justify-content-center' : '', align === 'right' || align === 'end' ? 'justify-content-end' : '']" role="tablist">
                <li v-for="tab in tabs" :key="tab.uid" class="nav-item" role="presentation">
                    <button type="button" :class="['nav-link', tab.localActive ? 'active' : '']" role="tab"
                            @click="selectTab(tab)">{{ tab.title }}</button>
                </li>
            </ul>
            <div :class="['tab-content', contentClass]">
                <slot></slot>
            </div>
        </div>`
}

let tabUid = 0

const BTab = {
    name: 'BTab',
    inject: ['bTabs'],
    props: {
        title: { type: String, default: '' },
        active: Boolean,
        id: { type: String, default: null }
    },
    emits: ['click'],
    data() {
        return { localActive: this.active, uid: ++tabUid }
    },
    created() {
        this.bTabs.registerTab(this)
    },
    beforeUnmount() {
        this.bTabs.unregisterTab(this)
    },
    watch: {
        active(v) {
            if (v) this.bTabs.selectTab(this)
        }
    },
    template: `
        <div :id="id" :class="['tab-pane', 'fade', localActive ? 'show active' : '']" role="tabpanel">
            <slot></slot>
        </div>`
}

/* -------------------------------- dropdown --------------------------------- */

const BDropdown = {
    name: 'BDropdown',
    props: {
        text: { type: String, default: '' },
        variant: { type: String, default: 'secondary' },
        size: { type: String, default: null },
        right: Boolean
    },
    data() {
        return { open: false }
    },
    mounted() {
        this._outside = e => {
            if (this.open && !this.$el.contains(e.target)) this.open = false
        }
        document.addEventListener('click', this._outside)
    },
    beforeUnmount() {
        document.removeEventListener('click', this._outside)
    },
    template: `
        <div class="b-dropdown dropdown btn-group">
            <button type="button" :class="['btn', 'btn-' + variant, size ? 'btn-' + size : '', 'dropdown-toggle']"
                    :aria-expanded="open ? 'true' : 'false'" @click="open = !open">
                <slot name="button-content">{{ text }}</slot>
            </button>
            <ul :class="['dropdown-menu', right ? 'dropdown-menu-end' : '', open ? 'show' : '']">
                <slot></slot>
            </ul>
        </div>`
}

const BDropdownItem = {
    name: 'BDropdownItem',
    props: { href: { type: String, default: '#' }, active: Boolean, disabled: Boolean },
    template: `
        <li>
            <a :class="['dropdown-item', active ? 'active' : '', disabled ? 'disabled' : '']" :href="href">
                <slot></slot>
            </a>
        </li>`
}

/* ------------------------------- form inputs ------------------------------- */

let checkUid = 0

const BFormCheckbox = {
    name: 'BFormCheckbox',
    props: {
        modelValue: { default: undefined },
        value: { default: true },
        uncheckedValue: { default: false },
        switch: Boolean,
        inline: Boolean,
        size: { type: String, default: null },
        id: { type: String, default: null },
        name: { type: String, default: null },
        disabled: Boolean
    },
    emits: ['update:modelValue', 'change', 'input'],
    data() {
        return { uid: 'bfc_' + (++checkUid) }
    },
    computed: {
        inputId() {
            return this.id || this.uid
        },
        isChecked() {
            if (Array.isArray(this.modelValue)) return this.modelValue.includes(this.value)
            return this.modelValue === this.value || this.modelValue === true
        }
    },
    methods: {
        onChange(e) {
            const checked = e.target.checked
            let next
            if (Array.isArray(this.modelValue)) {
                next = checked
                    ? this.modelValue.concat([this.value])
                    : this.modelValue.filter(v => v !== this.value)
            } else {
                next = checked ? this.value : this.uncheckedValue
            }
            this.$emit('update:modelValue', next)
            this.$emit('input', next)
            this.$emit('change', next)
        }
    },
    template: `
        <div :class="['form-check', $props.switch ? 'form-switch' : '', inline ? 'form-check-inline' : '', size ? 'form-check-' + size : '']">
            <input class="form-check-input" type="checkbox" :id="inputId" :name="name"
                   :checked="isChecked" :disabled="disabled" @change="onChange">
            <label v-if="$slots.default" class="form-check-label" :for="inputId"><slot></slot></label>
        </div>`
}

const BFormFile = {
    name: 'BFormFile',
    render() {
        return h('input', { type: 'file', class: 'form-control' })
    }
}

const BFormRating = {
    name: 'BFormRating',
    props: {
        modelValue: { type: [Number, String], default: null },
        value: { type: [Number, String], default: null },
        variant: { type: String, default: null },
        noBorder: Boolean,
        size: { type: String, default: null },
        showValue: Boolean,
        precision: { type: [Number, String], default: 0 },
        readonly: Boolean,
        inline: Boolean,
        stars: { type: [Number, String], default: 5 },
        id: { type: String, default: null }
    },
    emits: ['update:modelValue', 'change', 'input'],
    computed: {
        current() {
            const v = this.modelValue !== null && this.modelValue !== undefined && this.modelValue !== ''
                ? this.modelValue
                : this.value
            const n = parseFloat(v)
            return isNaN(n) ? 0 : n
        },
        starList() {
            const list = []
            const count = parseInt(this.stars) || 5
            for (let i = 1; i <= count; i++) {
                let icon = 'bi-star'
                if (this.current >= i - 0.25) icon = 'bi-star-fill'
                else if (this.current >= i - 0.75) icon = 'bi-star-half'
                list.push({ index: i, icon })
            }
            return list
        },
        displayValue() {
            const p = parseInt(this.precision) || 0
            return this.current.toFixed(p)
        }
    },
    methods: {
        setValue(i) {
            if (this.readonly) return
            this.$emit('update:modelValue', i)
            this.$emit('input', i)
            this.$emit('change', i)
        }
    },
    template: `
        <span :id="id" :class="['b-rating', inline ? 'd-inline-flex' : 'd-flex', 'align-items-center', noBorder ? '' : 'border rounded', size ? 'b-rating-' + size : '']"
              :style="readonly ? '' : 'cursor:pointer'">
            <i v-for="star in starList" :key="star.index"
               :class="['bi', star.icon, variant ? 'text-' + variant : '', 'mx-1']"
               @click="setValue(star.index)"></i>
            <span v-if="showValue" class="b-rating-value ms-1">{{ displayValue }}</span>
        </span>`
}

/* -------------------------------- tooltip ---------------------------------- */

const BTooltip = {
    name: 'BTooltip',
    props: {
        target: { type: String, required: true },
        placement: { type: String, default: 'top' },
        triggers: { type: String, default: 'hover' }
    },
    mounted() {
        this._bind = () => {
            const el = document.getElementById(this.target)
            if (!el || el.__bTooltipBound) return
            el.__bTooltipBound = true
            el.addEventListener('mouseenter', () => showTooltip(el, this.$el.innerHTML, this.placement, true))
            el.addEventListener('mouseleave', hideTooltip)
        }
        this._bind()
        this._retry = setTimeout(this._bind, 500)
    },
    beforeUnmount() {
        clearTimeout(this._retry)
        hideTooltip()
    },
    template: `<div style="display:none"><slot></slot></div>`
}

/* -------------------------------- countdown -------------------------------- */

const Countdown = {
    name: 'Countdown',
    props: { endTime: { type: [Number, String], default: 0 } },
    data() {
        return { now: Date.now() }
    },
    computed: {
        remaining() {
            return Math.max(0, parseFloat(this.endTime) - this.now)
        },
        finished() {
            return this.remaining <= 0
        },
        timeObj() {
            const total = Math.floor(this.remaining / 1000)
            const pad = n => (n < 10 ? '0' + n : String(n))
            return {
                d: String(Math.floor(total / 86400)),
                h: pad(Math.floor((total % 86400) / 3600)),
                m: pad(Math.floor((total % 3600) / 60)),
                s: pad(total % 60)
            }
        }
    },
    mounted() {
        this._t = setInterval(() => { this.now = Date.now() }, 1000)
    },
    beforeUnmount() {
        clearInterval(this._t)
    },
    render() {
        if (this.finished) {
            return this.$slots.finish ? this.$slots.finish() : null
        }
        return this.$slots.process ? this.$slots.process({ timeObj: this.timeObj }) : null
    }
}

/* ---------------------------- gallery slideshow ----------------------------- */

const VueGallerySlideshow = {
    name: 'VueGallerySlideshow',
    props: {
        images: { type: Array, default: () => [] },
        index: { type: Number, default: null }
    },
    emits: ['close'],
    computed: {
        visible() {
            return this.index !== null && this.index !== undefined && this.images.length > 0
        },
        current() {
            const img = this.images[this.index] || {}
            return typeof img === 'string' ? img : (img.fullimg || img.url)
        },
        currentAlt() {
            const img = this.images[this.index] || {}
            return typeof img === 'string' ? '' : (img.alt || '')
        }
    },
    data() {
        return { localIndex: null }
    },
    watch: {
        index(v) {
            this.localIndex = v
        }
    },
    methods: {
        thumb(img) {
            return typeof img === 'string' ? img : img.url
        },
        prev() {
            if (this.index > 0) this.$parent.index = this.index - 1
        },
        next() {
            if (this.index < this.images.length - 1) this.$parent.index = this.index + 1
        },
        goTo(i) {
            this.$parent.index = i
        }
    },
    template: `
        <div v-if="visible" class="vgs" style="position:fixed;inset:0;background:rgba(0,0,0,.85);z-index:1100;display:flex;flex-direction:column;align-items:center;justify-content:center;"
             @click.self="$emit('close')">
            <button type="button" class="btn-close btn-close-white" style="position:absolute;top:1rem;right:1rem;" aria-label="Close" @click="$emit('close')"></button>
            <button v-if="index > 0" type="button" class="btn btn-dark" style="position:absolute;left:1rem;top:50%;transform:translateY(-50%);" @click="goTo(index - 1)">&#10094;</button>
            <img :src="current" :alt="currentAlt" style="max-width:90vw;max-height:75vh;object-fit:contain;">
            <button v-if="index < images.length - 1" type="button" class="btn btn-dark" style="position:absolute;right:1rem;top:50%;transform:translateY(-50%);" @click="goTo(index + 1)">&#10095;</button>
            <div style="display:flex;gap:.5rem;margin-top:1rem;max-width:90vw;overflow-x:auto;">
                <img v-for="(img, i) in images" :key="i" :src="thumb(img)"
                     :style="{ height: '3.5rem', cursor: 'pointer', opacity: i === index ? 1 : 0.5 }"
                     @click="goTo(i)">
            </div>
        </div>`
}

/* -------------------------------- carousel --------------------------------- */

const BCarousel = {
    name: 'BCarousel',
    props: {
        interval: { type: [Number, String], default: 5000 },
        fade: Boolean,
        controls: Boolean,
        indicators: Boolean,
        imgWidth: { type: [Number, String], default: null },
        imgHeight: { type: [Number, String], default: null }
    },
    data() {
        return { index: 0, slides: [] }
    },
    provide() {
        return { bCarousel: this }
    },
    methods: {
        registerSlide(slide) {
            this.slides.push(slide)
        },
        unregisterSlide(slide) {
            const i = this.slides.indexOf(slide)
            if (i > -1) this.slides.splice(i, 1)
            if (this.index >= this.slides.length) this.index = 0
        },
        slideIndex(slide) {
            return this.slides.indexOf(slide)
        },
        goTo(i) {
            if (!this.slides.length) return
            this.index = (i + this.slides.length) % this.slides.length
        },
        next() {
            this.goTo(this.index + 1)
        },
        prev() {
            this.goTo(this.index - 1)
        },
        start() {
            this.stop()
            const ms = parseInt(this.interval)
            if (ms > 0 && this.slides.length > 1) {
                this._timer = setInterval(this.next, ms)
            }
        },
        stop() {
            if (this._timer) clearInterval(this._timer)
            this._timer = null
        }
    },
    mounted() {
        this.start()
    },
    beforeUnmount() {
        this.stop()
    },
    template: `
        <div :class="['carousel', 'slide', fade ? 'carousel-fade' : '']" @mouseenter="stop" @mouseleave="start">
            <ol v-if="indicators" class="carousel-indicators">
                <li v-for="(s, i) in slides" :key="i" role="button" data-bs-target=""
                    :class="i === index ? 'active' : ''" :aria-label="'Slide ' + (i + 1)" @click="goTo(i)"></li>
            </ol>
            <div class="carousel-inner">
                <slot></slot>
            </div>
            <button v-if="controls" class="carousel-control-prev" type="button" @click="prev">
                <span class="carousel-control-prev-icon" aria-hidden="true"></span>
                <span class="visually-hidden">Previous</span>
            </button>
            <button v-if="controls" class="carousel-control-next" type="button" @click="next">
                <span class="carousel-control-next-icon" aria-hidden="true"></span>
                <span class="visually-hidden">Next</span>
            </button>
        </div>`
}

const BCarouselSlide = {
    name: 'BCarouselSlide',
    inject: ['bCarousel'],
    props: {
        imgSrc: { type: String, default: null },
        imgAlt: { type: String, default: null },
        imgBlank: Boolean,
        height: { type: [Number, String], default: null },
        caption: { type: String, default: null },
        captionHtml: { type: String, default: null },
        text: { type: String, default: null },
        textHtml: { type: String, default: null }
    },
    computed: {
        isActive() {
            return this.bCarousel.slideIndex(this) === this.bCarousel.index
        },
        hasCaption() {
            return !!(this.caption || this.captionHtml || this.text || this.textHtml)
        }
    },
    created() {
        this.bCarousel.registerSlide(this)
    },
    beforeUnmount() {
        this.bCarousel.unregisterSlide(this)
    },
    template: `
        <div :class="['carousel-item', isActive ? 'active' : '']">
            <slot name="img">
                <img v-if="!imgBlank && imgSrc" class="d-block w-100" :src="imgSrc" :alt="imgAlt">
                <div v-else-if="imgBlank" :style="{ height: (height || 400) + 'px' }"></div>
            </slot>
            <div v-if="hasCaption" class="carousel-caption d-md-block">
                <h3 v-if="caption">{{ caption }}</h3>
                <div v-if="captionHtml" v-html="captionHtml"></div>
                <p v-if="text">{{ text }}</p>
                <div v-if="textHtml" v-html="textHtml"></div>
            </div>
            <slot></slot>
        </div>`
}

/* ------------------------------- registration ------------------------------ */

export function registerBvComponents(app) {
    app.component('b-icon', BIcon)
    app.component('b-link', BLink)
    app.component('b-button', BButton)
    app.component('b-button-group', simple('BButtonGroup', 'div', 'btn-group', { role: 'group' }))
    app.component('b-btn-group', simple('BBtnGroup', 'div', 'btn-group', { role: 'group' }))
    app.component('b-container', BContainer)
    app.component('b-row', simple('BRow', 'div', 'row'))
    app.component('b-col', BCol)
    app.component('b-card', BCard)
    app.component('b-card-header', simple('BCardHeader', 'div', 'card-header'))
    app.component('b-card-body', simple('BCardBody', 'div', 'card-body'))
    app.component('b-card-img-lazy', BCardImgLazy)
    app.component('b-img', BImg)
    app.component('b-img-lazy', BImgLazy)
    app.component('b-alert', BAlert)
    app.component('b-spinner', BSpinner)
    app.component('b-overlay', BOverlay)
    app.component('b-modal', BModal)
    app.component('b-collapse', BCollapse)
    app.component('b-sidebar', BSidebar)
    app.component('b-tabs', BTabs)
    app.component('b-tab', BTab)
    app.component('b-dropdown', BDropdown)
    app.component('b-dropdown-item', BDropdownItem)
    app.component('b-form-checkbox', BFormCheckbox)
    app.component('b-form-file', BFormFile)
    app.component('b-form-rating', BFormRating)
    app.component('b-tooltip', BTooltip)
    app.component('b-label', simple('BLabel', 'span', null))
    app.component('b-carousel', BCarousel)
    app.component('b-carousel-slide', BCarouselSlide)
    app.component('countdown', Countdown)
    app.component('vue-gallery-slideshow', VueGallerySlideshow)
}

export { VueGallerySlideshow }
