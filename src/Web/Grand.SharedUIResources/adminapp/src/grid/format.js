//Culture-aware display formatting for grid cells, driven by the CultureInfo data the
//<admin-grid> tag helper emits (see AdminGridCulture). Covers the formats the Kendo
//columns use: {0:G}, {0:0}, n2/n4/n8, c2, HH:mm, plus the other .NET standard date
//formats and simple custom numeric patterns.

export const invariantCulture = {
    name: '',
    numberFormat: {
        decimal: '.',
        group: ',',
        groupSizes: [3],
        negativeSign: '-',
        decimals: 2,
        currency: { symbol: '¤', decimals: 2, decimal: '.', group: ',', groupSizes: [3], positivePattern: 0, negativePattern: 0 },
        percent: { symbol: '%', decimals: 2, positivePattern: 0, negativePattern: 0 }
    },
    calendar: {
        shortDate: 'MM/dd/yyyy',
        longDate: 'dddd, dd MMMM yyyy',
        shortTime: 'HH:mm',
        longTime: 'HH:mm:ss',
        dateSeparator: '/',
        timeSeparator: ':',
        am: 'AM',
        pm: 'PM',
        months: ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'],
        monthsAbbr: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
        days: ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'],
        daysAbbr: ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'],
        //0 = Sunday, as Date#getDay counts it; the calendar of the date picker starts here
        firstDayOfWeek: 0
    }
}

/** Merges server culture data over the invariant defaults so missing parts never throw. */
export function normalizeCulture(culture) {
    const c = culture || {}
    const nf = { ...invariantCulture.numberFormat, ...(c.numberFormat || {}) }
    nf.currency = { ...invariantCulture.numberFormat.currency, ...(c.numberFormat?.currency || {}) }
    nf.percent = { ...invariantCulture.numberFormat.percent, ...(c.numberFormat?.percent || {}) }
    const calendar = { ...invariantCulture.calendar, ...(c.calendar || {}) }
    //a heading shows a month on its own; only some cultures write that differently from the
    //name a date is written with, and the island leaves the field out when they are the same
    if (!calendar.monthsStandalone) calendar.monthsStandalone = calendar.months
    return {
        name: c.name || '',
        numberFormat: nf,
        calendar
    }
}

const isoDate = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,7}))?)?)?(Z|[+-]\d{2}:?\d{2})?$/

/**
 * Parses a value coming from the server into a Date. ISO strings with Z or an offset are
 * absolute instants; without one they are wall-clock values (the admin converts dates to
 * the user's time zone before serializing them) and are kept as local time.
 */
export function toDate(value) {
    if (value instanceof Date) return isNaN(value.getTime()) ? null : value
    if (typeof value !== 'string') return null
    const m = isoDate.exec(value.trim())
    if (!m) return null
    const ms = m[7] ? Number((m[7] + '00').slice(0, 3)) : 0
    if (m[8]) return new Date(value.trim())
    return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]), Number(m[4] || 0), Number(m[5] || 0), Number(m[6] || 0), ms)
}

function roundTo(value, decimals) {
    //avoids 1.005.toFixed(2) === "1.00"
    const factor = Math.pow(10, decimals)
    return Math.round((Math.abs(value) * factor) + Number.EPSILON * factor) / factor
}

function groupDigits(integer, group, sizes) {
    if (!group || !sizes || sizes.length === 0) return integer
    const out = []
    let rest = integer
    let sizeIndex = 0
    let size = sizes[0]
    while (size > 0 && rest.length > size) {
        out.unshift(rest.slice(rest.length - size))
        rest = rest.slice(0, rest.length - size)
        if (sizeIndex < sizes.length - 1) size = sizes[++sizeIndex]
    }
    out.unshift(rest)
    return out.join(group)
}

function formatAbs(value, decimals, decimal, group, groupSizes) {
    const fixed = roundTo(value, decimals).toFixed(decimals)
    const [integer, fraction] = fixed.split('.')
    const grouped = groupDigits(integer, group, groupSizes)
    return fraction ? grouped + decimal + fraction : grouped
}

const currencyPositive = ['$n', 'n$', '$ n', 'n $']
const currencyNegative = ['($n)', '-$n', '$-n', '$n-', '(n$)', '-n$', 'n-$', 'n$-', '-n $', '-$ n', 'n $-', '$ n-', '$ -n', 'n- $', '($ n)', '(n $)', '$- n']
const percentPositive = ['n %', 'n%', '%n', '% n']
const percentNegative = ['-n %', '-n%', '-%n', '%-n', '%n-', 'n-%', 'n%-', '-% n', 'n %-', '% n-', '% -n', 'n- %']

function applyPattern(pattern, number, symbol, negativeSign) {
    return pattern.replace(/[n$%-]/g, token => {
        if (token === 'n') return number
        if (token === '-') return negativeSign
        return symbol
    })
}

/** Formats a number with a .NET/Kendo standard or simple custom numeric format. */
export function formatNumber(value, format, cultureData) {
    const culture = normalizeCulture(cultureData)
    const nf = culture.numberFormat
    if (typeof value !== 'number' || !isFinite(value)) return value == null ? '' : String(value)
    const fmt = (format || '').trim()
    const standard = /^([nNcCpPdDfF])(\d{0,2})$/.exec(fmt)
    const negative = value < 0
    if (standard) {
        const kind = standard[1].toLowerCase()
        const precision = standard[2] === '' ? null : Number(standard[2])
        if (kind === 'n' || kind === 'f') {
            const decimals = precision ?? nf.decimals
            const text = formatAbs(value, decimals, nf.decimal, kind === 'n' ? nf.group : '', nf.groupSizes)
            return (negative && Number(text.replace(/\D/g, '')) !== 0 ? nf.negativeSign : '') + text
        }
        if (kind === 'd') {
            const text = String(Math.round(Math.abs(value))).padStart(precision ?? 0, '0')
            return (negative ? nf.negativeSign : '') + text
        }
        if (kind === 'c') {
            const cf = nf.currency
            const text = formatAbs(value, precision ?? cf.decimals, cf.decimal || nf.decimal, cf.group ?? nf.group, cf.groupSizes || nf.groupSizes)
            const pattern = negative ? currencyNegative[cf.negativePattern] || '-$n' : currencyPositive[cf.positivePattern] || '$n'
            return applyPattern(pattern, text, cf.symbol, nf.negativeSign)
        }
        const pf = nf.percent
        const text = formatAbs(value * 100, precision ?? pf.decimals, nf.decimal, nf.group, nf.groupSizes)
        const pattern = negative ? percentNegative[pf.negativePattern] || '-n %' : percentPositive[pf.positivePattern] || 'n %'
        return applyPattern(pattern, text, pf.symbol, nf.negativeSign)
    }
    if (/^[#0,]*(\.[#0]*)?$/.test(fmt) && fmt !== '') {
        const [intPart, fracPart = ''] = fmt.split('.')
        const required = (fracPart.match(/0/g) || []).length
        const optional = (fracPart.match(/#/g) || []).length
        let text = formatAbs(value, required + optional, nf.decimal, intPart.includes(',') ? nf.group : '', nf.groupSizes)
        if (optional > 0) {
            const [i, f = ''] = text.split(nf.decimal)
            const trimmed = f.slice(0, required) + f.slice(required).replace(/0+$/, '')
            text = trimmed ? i + nf.decimal + trimmed : i
        }
        const isZero = Number(text.replace(/\D/g, '')) === 0
        return (negative && !isZero ? nf.negativeSign : '') + text
    }
    return String(value)
}

const dateTokens = /dddd|ddd|dd|d|MMMM|MMM|MM|M|yyyy|yyy|yy|y|HH|H|hh|h|mm|m|ss|s|fff|ff|f|tt|t|zzz|zz|z|'[^']*'|"[^"]*"|\\.|[/:]/g

const pad = (n, width) => String(n).padStart(width, '0')

/** Formats a Date with a .NET standard (d, D, t, T, g, G, f, F) or custom format. */
export function formatDate(date, format, cultureData) {
    const calendar = normalizeCulture(cultureData).calendar
    const standard = {
        d: calendar.shortDate,
        D: calendar.longDate,
        t: calendar.shortTime,
        T: calendar.longTime,
        g: calendar.shortDate + ' ' + calendar.shortTime,
        G: calendar.shortDate + ' ' + calendar.longTime,
        f: calendar.longDate + ' ' + calendar.shortTime,
        F: calendar.longDate + ' ' + calendar.longTime,
        s: "yyyy'-'MM'-'dd'T'HH':'mm':'ss"
    }
    const pattern = standard[format] || format || standard.G
    const hours = date.getHours()
    return pattern.replace(dateTokens, token => {
        switch (token) {
            case 'dddd': return calendar.days[date.getDay()]
            case 'ddd': return calendar.daysAbbr[date.getDay()]
            case 'dd': return pad(date.getDate(), 2)
            case 'd': return String(date.getDate())
            case 'MMMM': return calendar.months[date.getMonth()]
            case 'MMM': return calendar.monthsAbbr[date.getMonth()]
            case 'MM': return pad(date.getMonth() + 1, 2)
            case 'M': return String(date.getMonth() + 1)
            case 'yyyy': return pad(date.getFullYear(), 4)
            case 'yyy': return pad(date.getFullYear(), 3)
            case 'yy': return pad(date.getFullYear() % 100, 2)
            case 'y': return String(date.getFullYear() % 100)
            case 'HH': return pad(hours, 2)
            case 'H': return String(hours)
            case 'hh': return pad(hours % 12 || 12, 2)
            case 'h': return String(hours % 12 || 12)
            case 'mm': return pad(date.getMinutes(), 2)
            case 'm': return String(date.getMinutes())
            case 'ss': return pad(date.getSeconds(), 2)
            case 's': return String(date.getSeconds())
            case 'fff': return pad(date.getMilliseconds(), 3)
            case 'ff': return pad(date.getMilliseconds(), 3).slice(0, 2)
            case 'f': return pad(date.getMilliseconds(), 3).slice(0, 1)
            case 'tt': return hours < 12 ? calendar.am : calendar.pm
            case 't': return (hours < 12 ? calendar.am : calendar.pm).slice(0, 1)
            case 'zzz':
            case 'zz':
            case 'z': {
                const offset = -date.getTimezoneOffset()
                const sign = offset < 0 ? '-' : '+'
                const h = Math.floor(Math.abs(offset) / 60)
                if (token === 'z') return sign + h
                if (token === 'zz') return sign + pad(h, 2)
                return sign + pad(h, 2) + ':' + pad(Math.abs(offset) % 60, 2)
            }
            case '/': return calendar.dateSeparator
            case ':': return calendar.timeSeparator
            default:
                if (token[0] === '\\') return token.slice(1)
                return token.slice(1, -1)
        }
    })
}

function isDateFormat(fmt) {
    return /^[dDtTgGfFs]$/.test(fmt) || (/[yMdHhms]/.test(fmt) && !/[#0]/.test(fmt))
}

/**
 * Formats a cell value with a column format: "{0:n2}", "n2", "{0:G}" or text around a
 * placeholder like "{0:n2} kg". Values that do not fit the format are shown as they are.
 */
export function formatValue(value, format, culture) {
    if (value == null) return ''
    if (!format) return String(value)
    const apply = fmt => {
        if (isDateFormat(fmt)) {
            const date = toDate(value)
            return date ? formatDate(date, fmt, culture) : String(value)
        }
        if (typeof value === 'number') return formatNumber(value, fmt, culture)
        return String(value)
    }
    if (format.includes('{0')) {
        return format.replace(/\{0(?::([^}]*))?\}/g, (_, fmt) => (fmt ? apply(fmt) : String(value)))
    }
    return apply(format)
}

/**
 * Parses a number typed in the working culture. Group separators (including the
 * non-breaking spaces several cultures use) are ignored. Returns null when empty and
 * NaN when the text is not a number.
 */
export function parseNumber(text, cultureData) {
    if (typeof text === 'number') return text
    const nf = normalizeCulture(cultureData).numberFormat
    const raw = String(text ?? '').trim()
    if (raw === '') return null
    let normalized = raw
    if (nf.group) normalized = normalized.split(nf.group).join('')
    normalized = normalized.replace(/[\s\u00a0\u202f]/g, '')
    if (nf.decimal !== '.') {
        if (normalized.includes('.') && !raw.includes(nf.decimal) && nf.group !== '.') {
            //accept the invariant separator too, e.g. "0.5" typed in pl-PL
        } else {
            normalized = normalized.split(nf.decimal).join('.')
        }
    }
    normalized = normalized.replace(nf.negativeSign, '-')
    if (!/^-?\d*(\.\d*)?$/.test(normalized) || normalized === '-' || normalized === '.') return NaN
    return Number(normalized)
}

/**
 * Formats a number the way the MVC model binder reads it in the request culture: the
 * culture's decimal separator, no group separators (they are not spaces in every
 * culture), optionally a fixed number of decimals - kendo.toString(value, "n8") without
 * the grouping.
 */
export function toServerNumber(value, decimals, cultureData) {
    if (value == null || value === '') return ''
    if (typeof value !== 'number' || !isFinite(value)) return String(value)
    const nf = normalizeCulture(cultureData).numberFormat
    let text
    if (decimals == null) {
        text = Math.abs(value).toLocaleString('en-US', { useGrouping: false, maximumFractionDigits: 20 })
    } else {
        text = roundTo(value, decimals).toFixed(decimals)
    }
    const [integer, fraction] = text.split('.')
    const body = fraction ? integer + nf.decimal + fraction : integer
    const isZero = Number(body.replace(/\D/g, '')) === 0
    return (value < 0 && !isZero ? '-' : '') + body
}
