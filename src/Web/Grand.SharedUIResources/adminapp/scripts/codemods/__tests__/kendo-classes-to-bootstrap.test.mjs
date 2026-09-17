import { describe, it, expect } from 'vitest'
import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { ICON_MAP, convertClassValue, convertView } from '../kendo-classes-to-bootstrap.mjs'

describe('the icon mapping', () => {
    it('names an icon bootstrap-icons actually ships, every time', () => {
        const manifest = JSON.parse(readFileSync(
            fileURLToPath(new URL('../../../node_modules/bootstrap-icons/font/bootstrap-icons.json', import.meta.url)), 'utf8'))
        const missing = Object.entries(ICON_MAP)
            .filter(([, target]) => !(target.replace(/^bi-/, '') in manifest))
            .map(([source, target]) => `${source} -> ${target}`)
        expect(missing).toEqual([])
    })
})

describe('class values', () => {
    it('turns a Kendo button into a Bootstrap one', () => {
        expect(convertClassValue('k-button').value).toBe('btn btn-default btn-sm')
    })

    it('keeps the accent button accented, as one button', () => {
        expect(convertClassValue('k-primary k-button').value).toBe('btn btn-primary btn-sm')
    })

    it('drops the icon-plus-text modifier Bootstrap has no need for', () => {
        expect(convertClassValue('k-button run-now k-button-icontext').value)
            .toBe('btn btn-default btn-sm run-now')
    })

    it('replaces the underline-less link', () => {
        expect(convertClassValue('k-link editvalue').value).toBe('text-decoration-none editvalue')
    })

    it('turns a Kendo icon into a bootstrap-icon', () => {
        expect(convertClassValue('k-icon k-i-edit').value).toBe('bi bi-pencil')
        expect(convertClassValue('k-icon k-i-close-outline').value).toBe('bi bi-x-circle')
    })

    it('drops k-input from a form control', () => {
        expect(convertClassValue('form-control k-input text-box single-line').value)
            .toBe('form-control text-box single-line')
    })

    it('leaves the grid API class names alone', () => {
        const value = 'k-grid-delete k-detail-row'
        expect(convertClassValue(value)).toEqual({ value, changed: false })
    })

    it('renames only the decoration of a grid button', () => {
        expect(convertClassValue('k-button k-grid-delete').value).toBe('btn btn-default btn-sm k-grid-delete')
    })

    it('is idempotent', () => {
        const once = convertClassValue('k-button k-link k-icon k-i-save').value
        expect(convertClassValue(once).value).toBe(once)
    })
})

describe('a whole view', () => {
    it('rewrites both quote styles', () => {
        const src = `<a class='k-link editpicture'>x</a><span class="k-icon k-i-eye"></span>`
        const { output, changed } = convertView(src)
        expect(output).toBe(`<a class='text-decoration-none editpicture'>x</a><span class="bi bi-eye"></span>`)
        expect(changed).toBe(2)
    })
})
