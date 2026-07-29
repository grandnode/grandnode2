var subcatslider = new Vue({
    data() {
        return {
            swiperOptions: {
                effect: 'slide',
                lazy: {
                    preloaderClass: 'preloader'
                },
                autoplay: {
                    delay: 5000,
                },
                slidesPerView: 2,
                spaceBetween: 15,
                breakpoints: {
                    320: {
                        slidesPerView: 2,
                    },
                    576: {
                        slidesPerView: 2,
                    },
                    768: {
                        slidesPerView: 3,
                    },
                    992: {
                        slidesPerView: 3,
                    },
                    1200: {
                        slidesPerView: 3,
                    }
                },
                
            }
        }
    },
});
function sideToggle() {
    var leftSide = document.querySelector(".generalLeftSide");
    if (leftSide.classList.contains('show')) {
        leftSide.classList.remove('show');
        localStorage.setItem('leftSideOpen', 'false');
    } else {
        leftSide.classList.add('show');
        localStorage.setItem('leftSideOpen', 'true');
    }
    setTimeout(function () {
        if (vm.$refs.SubCategories) {
            vm.$refs.SubCategories.$swiper.update();
        }
    }, 400);
}
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".viewmode-icon").forEach(function (element) {
        element.addEventListener("click", function (e) {
            document.querySelectorAll('.viewmode-icon').forEach(function (el) {
                el.classList.remove('selected');
            });
            element.classList.add('selected');
        });
    });
});

function closeLeftSide() {
    var leftSide = document.querySelector(".generalLeftSide");
    if (leftSide.classList.contains('show')) {
        leftSide.classList.remove('show');
        localStorage.setItem('leftSideOpen', 'false');
    }
    setTimeout(function () {
        if (vm.$refs.SubCategories) {
            vm.$refs.SubCategories.$swiper.update();
        }
    }, 400);
}

function updateFiltersToggleVisibility() {
    var toggle = document.getElementById('mobile-filters-toggle');
    var leftSide = document.querySelector('.generalLeftSide');
    if (!toggle || !leftSide) return;
    // block-category-navigation is d-lg-block/d-none, so it only counts as
    // real content above the lg breakpoint - modal-close is always present
    // and isn't filter content, so it's excluded from the check.
    var hasContent = Array.prototype.some.call(leftSide.children, function (el) {
        if (el.classList.contains('modal-close')) return false;
        return getComputedStyle(el).display !== 'none';
    });
    toggle.style.display = hasContent ? '' : 'none';
}
document.addEventListener("DOMContentLoaded", updateFiltersToggleVisibility);
window.addEventListener("resize", updateFiltersToggleVisibility);

