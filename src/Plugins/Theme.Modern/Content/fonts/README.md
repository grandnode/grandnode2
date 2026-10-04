# Fonts

Geist and Geist Mono are licensed under the SIL Open Font License 1.1 (see `OFL-Geist.txt`).
They are served from this folder; the storefront makes no request to Google Fonts.

The files are the `latin` and `latin-ext` subsets of the variable fonts served by Google Fonts,
downloaded once on 2026-09-27. `latin-ext` is included because `latin` lacks Polish and other
Central European letters; `fonts.css` gives each file its `unicode-range`, so a page only
downloads `latin-ext` when it contains such a character.

| File | Axes | Source |
|---|---|---|
| `geist-latin-var.woff2` | wght 400–700 | https://fonts.gstatic.com/s/geist/v5/gyByhwUxId8gMEwcGFU.woff2 |
| `geist-latin-ext-var.woff2` | wght 400–700 | https://fonts.gstatic.com/s/geist/v5/gyByhwUxId8gMEwSGFWfOw.woff2 |
| `geist-mono-latin-var.woff2` | wght 400–600 | https://fonts.gstatic.com/s/geistmono/v6/or3nQ6H-1_WfwkMZI_qYFrcdmg.woff2 |
| `geist-mono-latin-ext-var.woff2` | wght 400–600 | https://fonts.gstatic.com/s/geistmono/v6/or3nQ6H-1_WfwkMZI_qYFrkdmgPn.woff2 |

They were found through
`https://fonts.googleapis.com/css2?family=Geist:wght@400..700&family=Geist+Mono:wght@400..600&display=swap`
(requested with a Chrome User-Agent).

Licence: https://github.com/google/fonts/blob/main/ofl/geist/OFL.txt
