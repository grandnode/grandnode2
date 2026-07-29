"use strict";

/* Vue 2 -> Vue 3: replaces vue-awesome-swiper (which registered <swiper> and
 * <swiper-slide> via Vue.use, an API the LegacyVue compat shim does not have).
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
            this._swiper = new Swiper(this.$refs.container, this.options);
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
