(function () {
    function $(selector, context) {
        context = context || document;
        return context["querySelectorAll"](selector);
    }

    function forEach(collection, iterator) {
        for (var key in Object.keys(collection)) {
            iterator(collection[key]);
        }
    }

    function showMenu(menu) {

        if (menu.target.classList.contains('hasGallery')) {
            checkPosition(menu.target);
        }

        if (this.tagName == "a") {
            var menuli = this;
        } else {
            var menuli = menu.target;
        }
        var ul = $("ul", menuli)[0];

        if (!ul || ul.classList.contains("-visible")) return;

        ul.classList.add("-visible");
        if (menu.target.previousElementSibling !== null) {
            menu.target.previousElementSibling.classList.add("-visible")
        }
        document.getElementById("backdrop-menu").classList.remove("hide");
        document.getElementById("backdrop-menu").classList.add("show");
    }

    function hideMenu(menu) {
        var menu = this;
        var ul = $("ul", menu)[0];

        if (!ul || !ul.classList.contains("-visible")) return;

        menu.classList.remove("-active");
        ul.classList.add("-animating");
        setTimeout(function () {
            ul.classList.remove("-visible");
            ul.classList.remove("-animating");
        }, 200);
        document.getElementById("backdrop-menu").classList.remove("show");
        document.getElementById("backdrop-menu").classList.add("hide");
    }
    function showMenuMobile(menu) {
        document.querySelectorAll(".-visible").forEach(function (element) {
            element.classList.remove("-visible");
        });
        if (this.tagName == "a") {
            var menuli = this;
        } else {
            var menuli = this.parentElement;
        }
        var ul = $("ul", menuli)[0];

        if (!ul || ul.classList.contains("-visible")) return;

        ul.classList.add("-visible");
        menu.target.previousElementSibling.classList.add("-visible")
    }
    function hideMenuMobile(menu) {
        menu.target.classList.remove("-visible");
        var menu = this.parentElement;
        var ul = $("ul", menu)[0];

        menu.classList.remove("-active");
        ul.classList.add("-animating");
        setTimeout(function () {
            ul.classList.remove("-visible");
            ul.classList.remove("-animating");
        }, 200);
    }

    function checkPosition(li) {
        var positions = li.getBoundingClientRect();
        var containerH = document.querySelector(".left-side-container").clientHeight;
        var menuH = document.querySelector(".Menu.-vertical").clientHeight;
        var Top = positions.top;

        if (document.body.classList.contains('.onTop')) {
            li.querySelectorAll("li > ul")[0].style.top = containerH - Top + 15 + "px";
        } else {
            li.querySelectorAll("li > ul")[0].style.top = containerH - Top + "px";
        }
        li.querySelectorAll("li > ul")[0].style.minHeight = menuH + 2 + "px";
    }

    function topMenuScroll() {

        var body = document.body;
        var scrollUp = "scroll-up";
        var scrollDown = "scroll-down";
        var onTop = "onTop";
        let lastScroll = 0;

        var currentScrollWindow = window.pageYOffset;
        if (currentScrollWindow == 0) {
            body.classList.add(onTop);
        }

        window.addEventListener("scroll", function () {
            var currentScroll = window.pageYOffset;
            if (window.pageYOffset <= 120) {
                body.classList.add(onTop);
            } else {
                body.classList.remove(onTop);
            }
            if (currentScroll > lastScroll && !body.classList.contains(scrollDown)) {
                // down
                body.classList.remove(scrollUp);
                if (lastScroll != 0) {
                    body.classList.add(scrollDown);
                    return;
                } else {
                    body.classList.remove(scrollDown);
                }
            } else if (currentScroll < lastScroll && body.classList.contains(scrollDown)) {
                // up
                body.classList.remove(scrollDown);
                body.classList.add(scrollUp);
            }
            lastScroll = currentScroll;
        });
    }

    document.addEventListener("DOMContentLoaded", function () {     
        if (991 < window.innerWidth) {
            topMenuScroll();
            forEach($(".Menu > li.-hasSubmenu.hasGallery"), function (e) {
                checkPosition(e);
            });
            forEach($(".Menu > li.-hasSubmenu"), function (e) {
                e.addEventListener("mouseenter", showMenu);
            });
            forEach($(".Menu > li"), function (e) {
                e.addEventListener("mouseleave", hideMenu);
            });
        } else {
            forEach($(".Menu > li.-hasSubmenu > .go-forward"), function (e) {
                e.addEventListener("click", showMenuMobile);
            });
            forEach($(".Menu > li.-hasSubmenu > .go-back"), function (e) {
                e.addEventListener("click", hideMenuMobile);
            });
        }

        //forEach($(".Menu li .back"), function(e){
        //    e.addEventListener("click", hideMenu);
        //});

    });
})();
