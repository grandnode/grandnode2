import { h } from 'vue'

/*
 * The handful of Vue components the storefront still needs.
 *
 * Everything Bootstrap 5 provides natively - modals, offcanvas drawers,
 * collapse, tabs, dropdowns, tooltips, carousels and the plain layout
 * wrappers - is now plain markup driven by Bootstrap's own JavaScript. What
 * is left are widgets Bootstrap has no equivalent for.
 */

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
        //the slot is wrapped in a span the theme lays out and puts the ":" separators in
        if (this.finished) {
            return h('span', this.$slots.finish ? this.$slots.finish() : [])
        }
        return h('span', this.$slots.process ? this.$slots.process({ timeObj: this.timeObj }) : [])
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

/* ------------------------------- registration ------------------------------ */

export function registerBvComponents(app) {
    app.component('b-form-rating', BFormRating)
    app.component('countdown', Countdown)
    app.component('vue-gallery-slideshow', VueGallerySlideshow)
}

export { VueGallerySlideshow }
