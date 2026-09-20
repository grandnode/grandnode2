import { describe, expect, it } from 'vitest'
import { formatDate, formatNumber, formatValue, parseNumber, toDate, toServerNumber } from './format.js'

//shapes emitted by AdminGridCulture for the cultures named in the plan's risk table
const enUS = {
    name: 'en-US',
    numberFormat: {
        decimal: '.', group: ',', groupSizes: [3], negativeSign: '-', decimals: 2,
        currency: { symbol: '$', decimals: 2, decimal: '.', group: ',', groupSizes: [3], positivePattern: 0, negativePattern: 1 },
        percent: { symbol: '%', decimals: 2, positivePattern: 1, negativePattern: 1 }
    },
    calendar: {
        shortDate: 'M/d/yyyy', longDate: 'dddd, MMMM d, yyyy', shortTime: 'h:mm tt', longTime: 'h:mm:ss tt',
        dateSeparator: '/', timeSeparator: ':', am: 'AM', pm: 'PM'
    }
}
const plPL = {
    name: 'pl-PL',
    numberFormat: {
        decimal: ',', group: '\u00a0', groupSizes: [3], negativeSign: '-', decimals: 2,
        currency: { symbol: 'zł', decimals: 2, decimal: ',', group: '\u00a0', groupSizes: [3], positivePattern: 3, negativePattern: 8 },
        percent: { symbol: '%', decimals: 2, positivePattern: 0, negativePattern: 0 }
    },
    calendar: {
        shortDate: 'dd.MM.yyyy', longDate: 'dddd, d MMMM yyyy', shortTime: 'HH:mm', longTime: 'HH:mm:ss',
        dateSeparator: '.', timeSeparator: ':', am: '', pm: '',
        months: ['stycznia', 'lutego', 'marca', 'kwietnia', 'maja', 'czerwca', 'lipca', 'sierpnia', 'września', 'października', 'listopada', 'grudnia'],
        days: ['niedziela', 'poniedziałek', 'wtorek', 'środa', 'czwartek', 'piątek', 'sobota']
    }
}
const deDE = {
    name: 'de-DE',
    numberFormat: {
        decimal: ',', group: '.', groupSizes: [3], negativeSign: '-', decimals: 2,
        currency: { symbol: '€', decimals: 2, decimal: ',', group: '.', groupSizes: [3], positivePattern: 3, negativePattern: 8 }
    },
    calendar: { shortDate: 'dd.MM.yyyy', longTime: 'HH:mm:ss', shortTime: 'HH:mm', dateSeparator: '.', timeSeparator: ':' }
}
const hiIN = {
    name: 'hi-IN',
    numberFormat: { decimal: '.', group: ',', groupSizes: [3, 2], negativeSign: '-', decimals: 2 }
}

describe('formatNumber', () => {
    it('formats n2, n4 and n8 with culture separators', () => {
        expect(formatNumber(1234567.891, 'n2', enUS)).toBe('1,234,567.89')
        expect(formatNumber(1234567.891, 'n2', plPL)).toBe('1\u00a0234\u00a0567,89')
        expect(formatNumber(1234567.891, 'n2', deDE)).toBe('1.234.567,89')
        expect(formatNumber(0.45359237, 'n8', plPL)).toBe('0,45359237')
        expect(formatNumber(2.5, 'n4', enUS)).toBe('2.5000')
        expect(formatNumber(-3.14159, 'n2', enUS)).toBe('-3.14')
    })

    it('rounds half away from zero like .NET', () => {
        expect(formatNumber(1.005, 'n2', enUS)).toBe('1.01')
        expect(formatNumber(-1.005, 'n2', enUS)).toBe('-1.01')
        expect(formatNumber(-0.001, 'n2', enUS)).toBe('0.00')
    })

    it('uses variable group sizes', () => {
        expect(formatNumber(123456789, 'n0', hiIN)).toBe('12,34,56,789')
    })

    it('formats c2 with the currency patterns', () => {
        expect(formatNumber(1234.5, 'c2', enUS)).toBe('$1,234.50')
        expect(formatNumber(-1234.5, 'c2', enUS)).toBe('-$1,234.50')
        expect(formatNumber(1234.5, 'c2', plPL)).toBe('1\u00a0234,50 zł')
        expect(formatNumber(-1234.5, 'c2', deDE)).toBe('-1.234,50 €')
    })

    it('formats the custom integer format {0:0} without grouping', () => {
        expect(formatNumber(12345.6, '0', enUS)).toBe('12346')
        expect(formatNumber(-2, '0', enUS)).toBe('-2')
        expect(formatNumber(1234.5, '#,##0.00', deDE)).toBe('1.234,50')
        expect(formatNumber(1.5, '0.##', plPL)).toBe('1,5')
    })

    it('returns non-numbers unchanged', () => {
        expect(formatNumber('12.50 $', 'n2', enUS)).toBe('12.50 $')
        expect(formatNumber(null, 'n2', enUS)).toBe('')
    })
})

describe('dates', () => {
    const date = new Date(2024, 0, 31, 14, 5, 9)

    it('formats G, d, t and custom HH:mm', () => {
        expect(formatDate(date, 'G', enUS)).toBe('1/31/2024 2:05:09 PM')
        expect(formatDate(date, 'G', plPL)).toBe('31.01.2024 14:05:09')
        expect(formatDate(date, 'd', deDE)).toBe('31.01.2024')
        expect(formatDate(date, 't', enUS)).toBe('2:05 PM')
        expect(formatDate(date, 'HH:mm', enUS)).toBe('14:05')
        expect(formatDate(date, 'D', plPL)).toBe('środa, 31 stycznia 2024')
        expect(formatDate(date, "yyyy'-'MM", enUS)).toBe('2024-01')
    })

    it('treats ISO strings without an offset as local wall-clock time', () => {
        const parsed = toDate('2024-01-31T14:05:09')
        expect(parsed.getHours()).toBe(14)
        expect(parsed.getDate()).toBe(31)
        expect(toDate('2024-01-31T14:05:09.1234567').getMilliseconds()).toBe(123)
        expect(toDate('2024-01-31T14:05:09Z').getTime()).toBe(Date.UTC(2024, 0, 31, 14, 5, 9))
        expect(toDate('not a date')).toBeNull()
        expect(toDate(42)).toBeNull()
    })
})

describe('formatValue', () => {
    it('applies Kendo column formats', () => {
        expect(formatValue('2024-01-31T14:05:09', '{0:G}', plPL)).toBe('31.01.2024 14:05:09')
        expect(formatValue(7, '{0:0}', enUS)).toBe('7')
        expect(formatValue(3.5, '{0:n2} kg', plPL)).toBe('3,50 kg')
        expect(formatValue(3.5, 'c2', enUS)).toBe('$3.50')
    })

    it('shows values that do not fit the format as they are', () => {
        expect(formatValue('', '{0:G}', enUS)).toBe('')
        expect(formatValue('n/a', '{0:G}', enUS)).toBe('n/a')
        expect(formatValue(null, '{0:G}', enUS)).toBe('')
        expect(formatValue('text', null, enUS)).toBe('text')
    })
})

describe('parseNumber', () => {
    it('reads numbers typed in the culture', () => {
        expect(parseNumber('1\u00a0234,5', plPL)).toBe(1234.5)
        expect(parseNumber('1 234,5', plPL)).toBe(1234.5)
        expect(parseNumber('1.234,5', deDE)).toBe(1234.5)
        expect(parseNumber('1,234.5', enUS)).toBe(1234.5)
        expect(parseNumber('-0,25', plPL)).toBe(-0.25)
        expect(parseNumber('0.5', plPL)).toBe(0.5)
    })

    it('returns null for empty and NaN for garbage', () => {
        expect(parseNumber('', enUS)).toBeNull()
        expect(parseNumber('   ', enUS)).toBeNull()
        expect(parseNumber('abc', enUS)).toBeNaN()
        expect(parseNumber('1,2,3.4.5', enUS)).toBeNaN()
    })
})

describe('toServerNumber', () => {
    it('posts the culture decimal separator without grouping', () => {
        expect(toServerNumber(1234.5, 8, plPL)).toBe('1234,50000000')
        expect(toServerNumber(0.45359237, 8, enUS)).toBe('0.45359237')
        expect(toServerNumber(1234.5, 2, deDE)).toBe('1234,50')
        expect(toServerNumber(1e-7, null, enUS)).toBe('0.0000001')
        expect(toServerNumber(-2.5, null, plPL)).toBe('-2,5')
        expect(toServerNumber(null, 2, plPL)).toBe('')
    })
})
