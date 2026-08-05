"use strict";

/* Vue 2 -> Vue 3: replaces vue-awesome-swiper (which registered <swiper> and
 * <swiper-slide> via Vue.use, an API the storefront's window.Vue does not have).
 * <swiper>/<swiper-slide> are registered here as thin wrappers around the
 * (framework-agnostic) Swiper core library loaded just before this script. */
Vue.component('swiper-slide', {
    template: '<div class="swiper-slide"><slot></slot></div>'
})
Vue.component('swiper', {
    props: {
        options: {
            type: Object,
            default: () => ({})
        }
    },
    data: function () {
        return { _swiper: null }
    },
    computed: {
        $swiper: function () {
            return this._swiper;
        }
    },
    mounted: function () {
        this.reparentControls();
        this.$nextTick(() => {
            /*
             * `observer` is not an optimisation - without it the gallery is dead.
             *
             * The slides come from the parent's <slot> (a v-for over a model that
             * arrives by ajax, e.g. the quick view), and Swiper reads the slide list
             * once, at construction. It was constructing against an empty wrapper, so
             * `swiper.slides` stayed empty for the life of the instance: slideTo() had
             * nothing to move, and Theme.Modern's attrchange threw on slides[0] every
             * time a product attribute changed. Observing the DOM lets Swiper pick the
             * slides up whenever they land; the update() below covers the slides that
             * were already there by the time this ran.
             */
            var options = Object.assign({ observer: true, observeParents: true }, this.options);
            this._swiper = new Swiper(this.$refs.container, options);
            this.$nextTick(() => { if (this._swiper) this._swiper.update(); });
        });
    },
    updated: function () {
        this.reparentControls();
        if (this._swiper) this._swiper.update();
    },
    beforeUnmount: function () {
        if (this._swiper) this._swiper.destroy(true, true);
    },
    methods: {
        reparentControls: function () {
            var container = this.$refs.container;
            if (!container) return;
            var wrapper = container.querySelector('.swiper-wrapper');
            if (!wrapper) return;
            wrapper.querySelectorAll(':scope > .swiper-button-next, :scope > .swiper-button-prev, :scope > .swiper-pagination, :scope > .swiper-scrollbar').forEach(function (el) {
                container.appendChild(el);
            });
        }
    },
    template: '<div class="swiper-container" ref="container"><div class="swiper-wrapper"><slot></slot></div></div>'
})
