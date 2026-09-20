import { describe, it, expect } from 'vitest'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { FA_MAP, SLI_MAP, FA_MODIFIERS, convertIconValue } from '../lib/icons-to-bootstrap-icons.mjs'
import { convertView } from '../icons-to-bootstrap-icons.mjs'

describe('the mapping table', () => {
    const manifest = JSON.parse(readFileSync(
        fileURLToPath(new URL('../../../node_modules/bootstrap-icons/font/bootstrap-icons.json', import.meta.url)), 'utf8'))
    const names = new Set(Object.keys(manifest))

    it('names an icon bootstrap-icons actually ships, every time', () => {
        const missing = [...Object.entries(FA_MAP), ...Object.entries(SLI_MAP)]
            .filter(([, target]) => !names.has(target.replace(/^bi-/, '')))
            .map(([source, target]) => `${source} -> ${target}`)
        expect(missing).toEqual([])
    })

    it('maps the Font Awesome modifiers onto something, not onto a glyph', () => {
        for (const target of Object.values(FA_MODIFIERS)) {
            expect(names.has(target.replace(/^bi-/, ''))).toBe(false)
        }
    })
})

describe('class values', () => {
    it('replaces the base class and the glyph', () => {
        expect(convertIconValue('fa fa-check').value).toBe('bi bi-check-lg')
        expect(convertIconValue('fa fa-trash-o').value).toBe('bi bi-trash')
    })

    it('adds the base class to a simple-line-icon, which had none', () => {
        expect(convertIconValue('icon-basket').value).toBe('bi bi-basket')
    })

    it('keeps the classes that are not icons, in place', () => {
        expect(convertIconValue('btn btn-sm fa fa-plus text-muted').value)
            .toBe('btn btn-sm bi bi-plus-lg text-muted')
    })

    it('translates the Font Awesome modifiers', () => {
        expect(convertIconValue('fa fa-cube fa-fw').value).toBe('bi bi-box bi-fw')
    })

    it('leaves a value with no icon in it alone', () => {
        expect(convertIconValue('btn btn-primary')).toEqual({ value: 'btn btn-primary', changed: false })
    })

    it('reports a value that mixes an icon with Razor instead of rewriting it', () => {
        const value = 'fa @Model.IconClass'
        expect(convertIconValue(value)).toEqual({ value, changed: false, manual: true })
    })

    it('is idempotent', () => {
        const once = convertIconValue('fa fa-check fa-fw').value
        expect(convertIconValue(once).value).toBe(once)
    })
})

describe('a whole view', () => {
    it('rewrites only the class attributes that carry an icon', () => {
        const src = '<i class="fa fa-search"></i><span class="badge">fa fa-search</span><i class="icon-settings"></i>'
        const { output, changed } = convertView(src)
        expect(output).toBe('<i class="bi bi-search"></i><span class="badge">fa fa-search</span><i class="bi bi-gear"></i>')
        expect(changed).toBe(2)
    })
})
