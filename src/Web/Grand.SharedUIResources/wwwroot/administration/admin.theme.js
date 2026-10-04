//The colour scheme of the Admin, Store and Vendor panels.
//
//Loaded with a plain, synchronous <script> in <head>, before the body is parsed, so a page
//never paints light and then flips: the attributes are on <html> before the first paint.
//
//Two attributes, one choice:
//- data-bs-theme is Bootstrap 5.3's own colour mode. Every Bootstrap component - form
//  controls, buttons, tables, dropdowns, modals, tabs - and everything written in terms of
//  its --bs-* variables (the <admin-grid>, the date picker, the numeric field, Tom Select)
//  follows it without a rule of its own.
//- data-theme is the panel's older switch; the rules for the chrome Bootstrap does not
//  draw (the side menu, the top bar, x_panel, the dashboard tiles) still key off it.
//
//localStorage "theme" holds the choice made with the switch in the header ("dark" or
//"light"); nothing stored means light, as before.
(function () {
    var KEY = 'theme';

    function read() {
        try { return localStorage.getItem(KEY); } catch (e) { return null; }
    }

    function apply(dark) {
        var value = dark ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', value);
        document.documentElement.setAttribute('data-bs-theme', value);
    }

    apply(read() === 'dark');

    window.grandAdminTheme = {
        isDark: function () {
            return document.documentElement.getAttribute('data-bs-theme') === 'dark';
        },
        set: function (dark) {
            try { localStorage.setItem(KEY, dark ? 'dark' : 'light'); } catch (e) { /* private mode */ }
            apply(!!dark);
        }
    };
})();
