/*
 * Font Awesome 4 and simple-line-icons -> bootstrap-icons.
 *
 * The panels loaded two icon fonts: Font Awesome 4.6 (1 573 uses, 69 distinct glyphs) and
 * simple-line-icons (74 uses, 30 distinct glyphs). bootstrap-icons replaces both - it is
 * the font the storefront already ships, it is MIT, and it comes with Bootstrap 5 rather
 * than beside it.
 *
 * Every entry below is one deliberate decision. Where the two sets do not line up
 * one for one the nearest glyph in the same visual family is used, and the comment
 * says so. Nothing is guessed at run time: every target is checked against the
 * bootstrap-icons manifest by the unit test, so a typo cannot ship.
 */

//Font Awesome 4 -> bootstrap-icons. Font Awesome's names are the 4.x ones (the -o suffix
//meant the outline variant; bootstrap-icons treats the outline as the default and marks
//the filled one -fill).
export const FA_MAP = {
    'fa-angle-double-left': 'bi-chevron-double-left',
    'fa-angle-double-right': 'bi-chevron-double-right',
    'fa-angle-down': 'bi-chevron-down',
    'fa-angle-left': 'bi-chevron-left',
    'fa-angle-right': 'bi-chevron-right',
    'fa-arrow-circle-left': 'bi-arrow-left-circle',
    'fa-ban': 'bi-slash-circle',
    'fa-bar-chart-o': 'bi-bar-chart',
    'fa-bell': 'bi-bell',
    'fa-bell-o': 'bi-bell',
    'fa-blog': 'bi-journal-text',
    'fa-briefcase': 'bi-briefcase',
    'fa-building-o': 'bi-building',
    'fa-cart-plus': 'bi-cart-plus',
    'fa-check': 'bi-check-lg',
    'fa-check-circle': 'bi-check-circle',
    'fa-chevron-down': 'bi-chevron-down',
    'fa-chevron-up': 'bi-chevron-up',
    'fa-chevron-right': 'bi-chevron-right',
    'fa-clock': 'bi-clock',
    'fa-clone': 'bi-files',
    'fa-close': 'bi-x-lg',
    'fa-comment': 'bi-chat',
    'fa-copy': 'bi-files',
    'fa-cube': 'bi-box',
    'fa-cubes': 'bi-boxes',
    'fa-database': 'bi-database',
    'fa-dot-circle-o': 'bi-record-circle',
    'fa-download': 'bi-download',
    //the pencil-in-a-square of the admin hint resources
    'fa-edit': 'bi-pencil-square',
    'fa-envelope': 'bi-envelope',
    'fa-envelope-o': 'bi-envelope',
    'fa-exclamation-triangle': 'bi-exclamation-triangle',
    'fa-eye': 'bi-eye',
    'fa-facebook': 'bi-facebook',
    'fa-file-excel-o': 'bi-file-earmark-excel',
    'fa-file-o': 'bi-file-earmark',
    'fa-file-pdf-o': 'bi-file-earmark-pdf',
    'fa-filter': 'bi-funnel',
    'fa-folder': 'bi-folder',
    'fa-folder-open': 'bi-folder2-open',
    //a bee; the knowledge base link. bootstrap-icons has no insect, so the nearest thing
    //to "community forum" it does have
    'fa-forumbee': 'bi-chat-square-text',
    'fa-forward': 'bi-fast-forward',
    'fa-gift': 'bi-gift',
    'fa-globe': 'bi-globe',
    //the news feed on the dashboard, not the site itself
    'fa-hacker-news': 'bi-newspaper',
    'fa-home': 'bi-house',
    'fa-info-circle': 'bi-info-circle',
    'fa-language': 'bi-translate',
    'fa-list-alt': 'bi-card-list',
    'fa-list-ol': 'bi-list-ol',
    'fa-lock': 'bi-lock',
    'fa-map-marker': 'bi-geo-alt',
    'fa-minus': 'bi-dash-lg',
    'fa-minus-square-o': 'bi-dash-square',
    'fa-money': 'bi-cash',
    'fa-paper-plane': 'bi-send',
    'fa-pencil': 'bi-pencil',
    'fa-plug': 'bi-plug',
    'fa-plus': 'bi-plus-lg',
    'fa-plus-square-o': 'bi-plus-square',
    'fa-refresh': 'bi-arrow-clockwise',
    'fa-search': 'bi-search',
    'fa-send': 'bi-send',
    'fa-shopping-cart': 'bi-cart',
    'fa-sitemap': 'bi-diagram-3',
    'fa-sliders': 'bi-sliders',
    'fa-star': 'bi-star',
    'fa-tag': 'bi-tag',
    'fa-tags': 'bi-tags',
    'fa-tasks': 'bi-list-task',
    'fa-times': 'bi-x-lg',
    'fa-times-circle-o': 'bi-x-circle',
    'fa-trash-o': 'bi-trash',
    'fa-truck': 'bi-truck',
    'fa-upload': 'bi-upload',
    'fa-user': 'bi-person',
    'fa-user-plus': 'bi-person-plus',
    //the "impersonate customer" action
    'fa-user-secret': 'bi-person-badge',
    'fa-users': 'bi-people',
    'fa-wrench': 'bi-wrench'
}

//simple-line-icons -> bootstrap-icons. These carry no base class of their own, so the
//codemod adds bi next to them.
export const SLI_MAP = {
    'icon-basket': 'bi-basket',
    'icon-bell': 'bi-bell',
    'icon-book-open': 'bi-book',
    'icon-bubbles': 'bi-chat-dots',
    'icon-bulb': 'bi-lightbulb',
    'icon-calendar': 'bi-calendar',
    'icon-home': 'bi-house',
    'icon-info': 'bi-info-circle',
    'icon-logout': 'bi-box-arrow-right',
    'icon-magnifier': 'bi-search',
    'icon-question': 'bi-question-circle',
    'icon-refresh': 'bi-arrow-repeat',
    'icon-reload': 'bi-arrow-clockwise',
    'icon-settings': 'bi-gear',
    'icon-user': 'bi-person'
}

//Font Awesome's own modifier classes. bootstrap-icons has no sizing scale, so the two
//sizes the panels use become the font-size utilities admin.bootstrap.css defines next to
//them, and fa-fw (a fixed-width glyph box) becomes bi-fw, defined in the same stylesheet.
export const FA_MODIFIERS = {
    'fa-fw': 'bi-fw',
    'fa-lg': 'fs-5',
    'fa-2x': 'fs-3',
    'fa-3x': 'fs-1',
    'fa-spin': 'bi-spin',
    'fa-pull-left': 'float-start',
    'fa-pull-right': 'float-end'
}

//The base class. Font Awesome needed "fa" next to the glyph; bootstrap-icons needs "bi".
const FA_BASE = new Set(['fa', 'fas', 'far', 'fab', 'fal'])

/** Rewrites one whitespace-separated class token list. Returns the new list. */
export function convertIconClassList(tokens) {
    const out = []
    //where "bi" goes: where the Font Awesome base class stood, or in front of the first
    //glyph when the icon set had no base class of its own
    let baseAt = -1
    for (const token of tokens) {
        if (FA_BASE.has(token)) { if (baseAt < 0) baseAt = out.length; continue }
        if (FA_MAP[token]) { if (baseAt < 0) baseAt = out.length; out.push(FA_MAP[token]); continue }
        if (FA_MODIFIERS[token]) { out.push(FA_MODIFIERS[token]); continue }
        if (SLI_MAP[token]) { if (baseAt < 0) baseAt = out.length; out.push(SLI_MAP[token]); continue }
        out.push(token)
    }
    if (baseAt >= 0 && !out.includes('bi')) out.splice(baseAt, 0, 'bi')
    return out
}

const PLAIN_CLASS = /^[A-Za-z][A-Za-z0-9_-]*$/

export function convertIconValue(value) {
    const parts = value.split(/(\s+)/)
    const tokens = parts.filter((_, i) => i % 2 === 0).filter(Boolean)
    if (!tokens.some(t => FA_BASE.has(t) || FA_MAP[t] || FA_MODIFIERS[t] || SLI_MAP[t])) {
        return { value, changed: false }
    }
    //a value that mixes icon classes with Razor is left for a person
    if (!tokens.every(t => PLAIN_CLASS.test(t))) return { value, changed: false, manual: true }
    const converted = convertIconClassList(tokens)
    const leading = /^\s*/.exec(value)[0]
    const trailing = /\s*$/.exec(value)[0]
    const result = `${leading}${converted.join(' ')}${trailing}`
    return { value: result, changed: result !== value }
}
