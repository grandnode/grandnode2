Vue.use(VueAwesomeSwiper)
var hpnslider = new Vue({
    data() {
        return {
            Model: null,
            swiperOptions: {
                effect: 'fade',
                lazy: {
                    preloaderClass: 'preloader'
                },
                autoplay: {
                    delay: 5000,
                },
                fadeEffect: {
                    crossFade: true
                },
                pagination: {
                    el: '#hpnslider .swiper-pagination',
                    clickable: true
                },
                navigation: {
                    nextEl: '#hpnslider .swiper-button-next',
                    prevEl: '#hpnslider .swiper-button-prev'
                },
                scrollbar: {
                    el: '#hpnslider .swiper-scrollbar',
                    draggable: true,
                    hide: false
                },
                slidesPerView: 1,
                on: {
                    init: function () {
                        hpnslider.progressBarIn(this);
                        hpnslider.animateIn(this);
                    },
                    slideChangeTransitionStart: function () {
                        hpnslider.progressBarIn(this);
                        hpnslider.animateIn(this);
                    },
                    activeIndexChange: function () {
                        hpnslider.progressBarOut(this);
                        hpnslider.animateOut(this);
                    },
                }
            }
        }
    },
    methods: {
        animateIn(slider) {
            if (typeof (hpnslider.Model) == 'object') {
                if (slider.slides.length > 0) {
                    var active = slider.slides[slider.activeIndex];
                    active.querySelectorAll('.animate__animated').forEach(function (element) {
                        var delay = element.dataset.delay;
                        var animation = element.dataset.animation;
                        element.classList.add(delay);
                        element.classList.add(animation);
                    });
                }
            }
        },
        animateOut(slider) {
            if (typeof (hpnslider.Model) == 'object') {
                var prev = slider.slides[slider.previousIndex];
                prev.querySelectorAll('.animate__animated').forEach(function (element) {
                    var delay = element.dataset.delay;
                    var animation = element.dataset.animation;
                    element.classList.remove(delay);
                    element.classList.remove(animation);
                });
            }
        },
        progressBarIn(slider) {
            slider.scrollbar.init();
            void slider.el.querySelector('#hpnslider .swiper-scrollbar').offsetWidth;
            slider.el.querySelector('#hpnslider .swiper-scrollbar').classList.add('progressAnimation');
            slider.scrollbar.dragEl.style.animationDuration = "5s";
        },
        progressBarOut(slider) {
            slider.scrollbar.init();
            void slider.el.querySelector('#hpnslider .swiper-scrollbar').offsetWidth;
            slider.el.querySelector('#hpnslider .swiper-scrollbar').classList.remove('progressAnimation');
            slider.scrollbar.dragEl.style.animationDuration = "0s";
        },
        groupBy(xs, key) {
            return xs.reduce(function (rv, x) {
                (rv[x[key]] = rv[x[key]] || []).push(x);
                return rv;
            }, {});
        }
    },
    created() {
        axios({
            baseURL: '/Component/Index?Name=HomePageNewProducts',
            method: 'get',
            data: null,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'X-Response-View': 'Json'
            }
        }).then(response => {
            this.Model = response.data;
        }).then(function () {
            if (hpnslider.Model.length > 0) {
                hpnslider.Model.forEach(function (element) {
                    element.SpecificationAttributeModels = hpnslider.groupBy(element.SpecificationAttributeModels, 'SpecificationAttributeName')
                })
            }
        });
    }
});