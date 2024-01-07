Vue.use(VueAwesomeSwiper)
var hpi = new Vue({
    props: {
        HomePageItems: {
            type: Array,
            default: () => [
                {
                    HomePageCategories: {
                        items: [],
                        swiperOptions: {
                            lazy: true,
                            freeMode: true,
                            autoplay: {
                                delay: 5000,
                            },
                            spaceBetween: 10,
                            breakpoints: {
                                320: {
                                    slidesPerView: 1,
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
                        },
                        loading: true,
                    },
                    HomePageBrands: {
                        items: [],
                        loading: true,
                        swiperOptions: {
                            loop: true,
                            autoplay: {
                                delay: 5000,
                            },
                            breakpoints: {
                                320: {
                                    slidesPerView: 2,
                                },
                                576: {
                                    slidesPerView: 2,
                                },
                                768: {
                                    slidesPerView: 4,
                                },
                                992: {
                                    slidesPerView: 4,
                                },
                                1200: {
                                    slidesPerView: 5,
                                },
                                1400: {
                                    slidesPerView: 6,
                                }
                            },
                        },
                    },
                    HomePageProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    CategoryFeaturedProducts: {
                        items: [],
                        swiperOptions: {
                            loop: true,
                            lazy: true,
                            watchOverflow: true,
                            autoplay: {
                                delay: 5000,
                            },
                            slidesPerView: 1,
                            slidesPerColumn: 3,
                            slidesPerColumnFill: 'row',
                            breakpoints: {
                                320: {
                                    slidesPerView: 2,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                575: {
                                    slidesPerView: 2,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                768: {
                                    slidesPerView: 3,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                992: {
                                    slidesPerView: 4,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                1399: {
                                    slidesPerView: 5,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                1600: {
                                    slidesPerView: 6,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                            },
                            spaceBetween: 0,
                            lazy: {
                                preloaderClass: 'preloader',
                            }
                        },
                        loading: true,
                    },
                    CollectionFeaturedProducts: {
                        items: [],
                        swiperOptions: {
                            loop: true,
                            lazy: true,
                            watchOverflow: true,
                            autoplay: {
                                delay: 5000,
                            },
                            slidesPerView: 1,
                            slidesPerColumn: 3,
                            slidesPerColumnFill: 'row',
                            breakpoints: {
                                320: {
                                    slidesPerView: 2,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                575: {
                                    slidesPerView: 2,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                768: {
                                    slidesPerView: 3,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                992: {
                                    slidesPerView: 4,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                1399: {
                                    slidesPerView: 5,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                                1600: {
                                    slidesPerView: 6,
                                    slidesPerColumn: 2,
                                    slidesPerColumnFill: 'row',
                                },
                            },
                            spaceBetween: 0,
                            lazy: {
                                preloaderClass: 'preloader',
                            },
                            navigation: {
                                nextEl: '#HomePageBestSellers .swiper-button-next',
                                prevEl: '#HomePageBestSellers .swiper-button-prev'
                            }
                        },
                        loading: true,
                    },
                    HomePageBestSellers: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    CustomerRecommendedProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    PersonalizedProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    SuggestedProducts: {
                        items: [],
                        loading: true,
                        ProductsToShow: null,
                        NumberOfProducts: null,
                    },
                    HomePageNews: {
                        items: [],
                        swiperOptions: {
                            lazy: {
                                preloaderClass: 'preloader'
                            },
                            autoplay: {
                                delay: 5000,
                            },
                            fadeEffect: {
                                crossFade: true
                            },
                            slidesPerView: 1,
                            spaceBetween: 15,
                            navigation: {
                                nextEl: '#HomePageNews .swiper-button-next',
                                prevEl: '#HomePageNews .swiper-button-prev'
                            },
                            breakpoints: {
                                320: {
                                    slidesPerView: 1
                                },
                                575: {
                                    slidesPerView: 2,
                                },
                                992: {
                                    slidesPerView: 3,
                                }
                            }
                        },
                        loading: true,
                    },
                    HomePageBlog: {
                        items: [],
                        swiperOptions: {
                            lazy: {
                                preloaderClass: 'preloader'
                            },
                            loop: true,
                            autoplay: {
                                delay: 5000,
                            },
                            slidesPerView: 1,
                            spaceBetween: 15,
                            navigation: {
                                nextEl: '#HomePageBlog .swiper-button-next',
                                prevEl: '#HomePageBlog .swiper-button-prev'
                            },
                            pagination: {
                                el: '#HomePageBlog .swiper-pagination',
                                type: 'bullets',
                                clickable: true,
                            },
                            breakpoints: {
                                320: {
                                    slidesPerView: 1,
                                },
                                575: {
                                    slidesPerView: 2,
                                },
                                768: {
                                    slidesPerView: 3,
                                },
                                992: {
                                    slidesPerView: 2,
                                },
                                1250: {
                                    slidesPerView: 3,
                                },
                                1540: {
                                    slidesPerView: 3,
                                },
                                1680: {
                                    slidesPerView: 3,
                                },
                                1920: {
                                    slidesPerView: 3,
                                },
                            },
                        },
                        loading: true,
                    },
                }
            ]
        }
    },
    methods: {
        loadMore(event, NumberProducts, Products) {
            var SectionName = event.target.closest(".section");
            var LoadedProducts = SectionName.querySelectorAll(".product-box").length;

            if (LoadedProducts >= NumberProducts) {
                event.target.classList.add('disabled');
                event.target.innerText = event.target.getAttribute("data-nomore");
            } else {
                if ((NumberProducts - LoadedProducts) >= 6) {
                    var showed = Products.ProductsToShow;
                    Products.ProductsToShow = showed + 6;
                } else {
                    Products.ProductsToShow = Products.ProductsToShow + (NumberProducts - LoadedProducts);
                    event.target.classList.add('disabled');
                    event.target.innerText = event.target.getAttribute("data-nomore");
                }
            }
        },
        groupBy(xs, key) {
            return xs.reduce(function (rv, x) {
                (rv[x[key]] = rv[x[key]] || []).push(x);
                return rv;
            }, {});
        }
    }
})

function LoadStandard(section) {
    axios({
        baseURL: '/Component/Form?Name=' + section + '',
        method: 'get',
        data: null,
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json',
            'X-Response-View': 'Json'
        }
    }).then(response => {
        if (response.data != "") {
            hpi.HomePageItems[0][section].items = response.data;
        }
    }).then(function () {
        hpi.HomePageItems[0][section].loading = false;
    })
}
function LoadMore(section) {
    axios({
        baseURL: '/Component/Form?Name=' + section + '',
        method: 'get',
        data: null,
        headers: {
            'Accept': 'asplication/json',
            'Content-Type': 'asplication/json',
            'X-Response-View': 'Json'
        }
    }).then(response => {
        if (response.data != "") {
            hpi.HomePageItems[0][section].items = response.data;
            hpi.HomePageItems[0][section].NumberOfProducts = response.data.length;
            if (response.data.length < 6) {
                hpi.HomePageItems[0][section].ProductsToShow = hpi.HomePageItems[0][section].NumberOfProducts;
            } else {
                hpi.HomePageItems[0][section].ProductsToShow = 6;
            }
        }
    }).then(function () {
        hpi.HomePageItems[0][section].loading = false;
    }).then(function () {
        hpi.HomePageItems[0][section].items.forEach(function (element) {
            element.SpecificationAttributeModels = hpi.groupBy(element.SpecificationAttributeModels, 'SpecificationAttributeName')
        })
    });
}

function LazyLoad() {
    var hp_section = document.querySelectorAll('.home-page-section');
    var viewLine = window.scrollY + window.innerHeight;
    for (var i = 0; i < hp_section.length; i++) {
        var section_loaded = hp_section[i].dataset.isloaded;
        if (hp_section[i].offsetTop - viewLine < 0 && section_loaded !== 'true') {
            var hpsectionid = hp_section[i].dataset.id;
            if (hp_section[i].dataset.load === 'standard') {
                LoadStandard(hpsectionid)
            } else {
                LoadMore(hpsectionid)
            }
            hp_section[i].dataset.isloaded = 'true';
        }
    }
}

window.onload = function () {
    LazyLoad();
};
document.addEventListener("DOMContentLoaded", function () {
    window.addEventListener('scroll', LazyLoad);

    document.querySelectorAll('.parallax-banner').forEach(function (element) {
        var scene = element;
        var parallaxInstance = new Parallax(scene, {
            relativeInput: true,
            hoverOnly: true,
        });
    })
});