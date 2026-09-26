# Fonts

Both families are licensed under the SIL Open Font License 1.1 (see `OFL-Fraunces.txt` and
`OFL-Inter.txt`). They are served from this folder; the storefront makes no request to Google Fonts.

The files are the Latin subsets of the variable fonts served by Google Fonts, downloaded once on
2026-09-26:

| File | Axes | Source |
|---|---|---|
| `fraunces-latin-var.woff2` | opsz 9–144, wght 300–600 | https://fonts.gstatic.com/s/fraunces/v38/6NU78FyLNQOQZAnv9bYEvDiIdE9Ea92uemAk_WBq8U_9v0c2Wa0KxC9TeA.woff2 |
| `inter-latin-var.woff2` | wght 400–700 | https://fonts.gstatic.com/s/inter/v20/UcC73FwrK3iLTeHuS_nVMrMxCp50SjIa1ZL7.woff2 |

They were found through
`https://fonts.googleapis.com/css2?family=Fraunces:opsz,wght@9..144,300;9..144,400;9..144,600&family=Inter:wght@400;500;600;700&display=swap`
(requested with a Chrome User-Agent), keeping only the `/* latin */` blocks. Google serves one
variable file per family for all requested weights, so there is one file per family here.

Licences: https://github.com/google/fonts/blob/main/ofl/fraunces/OFL.txt and
https://github.com/google/fonts/blob/main/ofl/inter/OFL.txt
