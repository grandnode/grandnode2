// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { compileTemplate, sanitizeUrl } from './template.js'
import { compileCondition } from './expression.js'

function render(html, item, scope = {}) {
    const host = document.createElement('div')
    host.appendChild(compileTemplate(html).render(item, scope))
    return host
}

afterEach(() => {
    vi.restoreAllMocks()
})

describe('cell templates', () => {
    it('encodes {{ }} output', () => {
        const host = render('<a href="Edit/{{ Id }}">{{ Name }}</a>', { Id: '5', Name: '<img src=x onerror=alert(1)>' })
        expect(host.querySelector('img')).toBeNull()
        expect(host.querySelector('a').textContent).toBe('<img src=x onerror=alert(1)>')
        expect(host.innerHTML).toBe('<a href="Edit/5">&lt;img src=x onerror=alert(1)&gt;</a>')
    })

    it('does not break out of attributes', () => {
        const host = render('<span title="{{ Name }}">x</span>', { Name: '" onmouseover="alert(1)' })
        const span = host.querySelector('span')
        expect(span.getAttribute('title')).toBe('" onmouseover="alert(1)')
        expect(span.hasAttribute('onmouseover')).toBe(false)
    })

    it('renders {{{ }}} as HTML only when opted in', () => {
        const host = render('<div>{{{ AttributeInfo }}}</div>', { AttributeInfo: 'Color: <b>Red</b><br />Size: M' })
        expect(host.querySelector('b').textContent).toBe('Red')
        expect(host.querySelectorAll('br')).toHaveLength(1)
    })

    it('does not evaluate placeholders that come from data', () => {
        const host = render('<span>{{ Name }}</span> {{{ Html }}}', { Name: '{{ Secret }}', Html: '<i>{{{ Secret }}}</i>', Secret: 'leaked' })
        expect(host.textContent).not.toContain('leaked')
        expect(host.querySelector('span').textContent).toBe('{{ Secret }}')
        expect(host.querySelector('i').textContent).toBe('{{{ Secret }}}')
    })

    it('drops URL attributes that would run script', () => {
        const host = render('<a href="{{ Url }}">a</a><img src="{{ Img }}"><form action="{{ Url }}"></form>', {
            Url: ' JaVa\tScRiPt:alert(1)',
            Img: 'javascript:alert(1)'
        })
        expect(host.querySelector('a').hasAttribute('href')).toBe(false)
        expect(host.querySelector('img').hasAttribute('src')).toBe(false)
        expect(host.querySelector('form').hasAttribute('action')).toBe(false)
    })

    it('keeps relative, absolute http and image data URLs', () => {
        const host = render('<img src="{{ Thumb }}"><a href="{{ Link }}">x</a>', {
            Thumb: 'data:image/png;base64,iVBORw0KGgo=',
            Link: 'https://example.com/a?b=1&c=2'
        })
        expect(host.querySelector('img').getAttribute('src')).toBe('data:image/png;base64,iVBORw0KGgo=')
        expect(host.querySelector('a').getAttribute('href')).toBe('https://example.com/a?b=1&c=2')
        expect(sanitizeUrl('data:text/html;base64,PHNjcmlwdD4=', 'href')).toBeNull()
        expect(sanitizeUrl('/Admin/Product/Edit/1', 'href')).toBe('/Admin/Product/Edit/1')
    })

    it('removes placeholders from event handler, style and srcdoc attributes', () => {
        const warn = vi.spyOn(console, 'warn').mockImplementation(() => { })
        const host = render('<a onclick="go(\'{{ Id }}\')" style="color:{{ Color }}" data-id="{{ Id }}">x</a><iframe srcdoc="{{ Html }}"></iframe>', {
            Id: "'); alert(1); ('", Color: 'red', Html: '<script>alert(1)</script>'
        })
        const a = host.querySelector('a')
        expect(a.hasAttribute('onclick')).toBe(false)
        expect(a.hasAttribute('style')).toBe(false)
        expect(a.getAttribute('data-id')).toBe("'); alert(1); ('")
        expect(host.querySelector('iframe').hasAttribute('srcdoc')).toBe(false)
        expect(warn).toHaveBeenCalledTimes(3)
    })

    it('supports data-if and data-else', () => {
        const tpl = '<i data-if="Published" class="yes"></i><i data-else class="no"></i><b>{{ Name }}</b>'
        expect(render(tpl, { Published: true, Name: 'a' }).innerHTML).toBe('<i class="yes"></i><b>a</b>')
        expect(render(tpl, { Published: false, Name: 'a' }).innerHTML).toBe('<i class="no"></i><b>a</b>')
        expect(render('<span data-if="Price != 10">{{ Price }}</span>', { Price: 10 }).innerHTML).toBe('')
        expect(render('<br data-if="AttributeInfo">', { AttributeInfo: '' }).innerHTML).toBe('')
    })

    it('handles nested conditions', () => {
        const tpl = '<div data-if="A"><span data-if="B">ab</span><span data-else>a</span></div><div data-else>none</div>'
        expect(render(tpl, { A: true, B: false }).textContent).toBe('a')
        expect(render(tpl, { A: true, B: true }).textContent).toBe('ab')
        expect(render(tpl, { A: false, B: true }).textContent).toBe('none')
    })

    it('formats placeholders and reads texts', () => {
        const culture = { numberFormat: { decimal: ',', group: ' ', groupSizes: [3] } }
        const host = render('<span>{{ Ratio | n2 }} {{ $texts.Mark }}</span>', { Ratio: 1234.5 }, { culture, texts: { Mark: '<Mark>' } })
        expect(host.textContent).toBe('1 234,50 <Mark>')
    })

    it('renders missing and object values as empty text', () => {
        expect(render('<span>{{ Missing }}|{{ Obj }}|{{ Zero }}|{{ Flag }}</span>', { Obj: { a: 1 }, Zero: 0, Flag: false }).textContent)
            .toBe('||0|false')
    })

    it('does not read inherited properties', () => {
        expect(render('<span>{{ constructor }}{{ __proto__ }}</span>', {}).textContent).toBe('')
    })
})

describe('conditions', () => {
    it('evaluates comparisons, negation, && and ||', () => {
        const item = { ProductTypeId: 10, Name: 'x', Empty: '', StoreId: 's1', List: [] }
        expect(compileCondition('ProductTypeId != 10')(item)).toBe(false)
        expect(compileCondition('ProductTypeId == 10 && Name')(item)).toBe(true)
        expect(compileCondition('!Empty')(item)).toBe(true)
        expect(compileCondition("StoreId == '' || StoreId == 's1'")(item)).toBe(true)
        expect(compileCondition("StoreId == 's2' || Empty")(item)).toBe(false)
        expect(compileCondition('List')(item)).toBe(false)
        expect(compileCondition('Missing == null')(item)).toBe(true)
        expect(compileCondition('ProductTypeId >= 5 && ProductTypeId < 11')(item)).toBe(true)
        expect(compileCondition('$texts.On')(item, { texts: { On: 'yes' } })).toBe(true)
    })

    it('rejects anything that is not the condition language', () => {
        expect(() => compileCondition('alert(1)')).toThrow()
        expect(() => compileCondition('a = 1')).toThrow()
        expect(() => compileCondition('a &&')).toThrow()
        expect(() => compileCondition('')).toThrow()
        expect(() => compileCondition('a; b')).toThrow()
    })
})
