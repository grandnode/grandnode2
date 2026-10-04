//Parsing of a date or a time typed in the panel's working culture.
//
//The date pickers post what they always posted - the culture short date, the culture short
//time - so the text a user types has to be read with the same .NET pattern the server
//formatted it with, never with the browser's locale. format.js formats; this parses.
//
//Parsing is deliberately lenient in the way .NET's DateTime.Parse is: any non-digit run
//separates the parts, so 31.12.2026 is read by a dd/MM/yyyy culture too, and a two-digit
//year is pivoted the way the invariant calendar pivots it (00-49 -> 2000s, 50-99 -> 1900s,
//TwoDigitYearMax 2049).

import { normalizeCulture } from '../grid/format.js'

const literal = /'[^']*'|"[^"]*"|\\./g

/** The order the y, M and d parts appear in, e.g. ['d','M','y'] for dd.MM.yyyy. */
export function dateOrder(pattern) {
    const order = []
    for (const char of String(pattern || '').replace(literal, '')) {
        const part = char === 'y' ? 'y' : char === 'M' ? 'M' : char === 'd' ? 'd' : null
        if (part && order[order.length - 1] !== part) order.push(part)
    }
    return order.length === 3 ? order : ['M', 'd', 'y']
}

/** Whether a pattern writes its month as a name (MMM/MMMM) rather than a number. */
function hasMonthName(pattern) {
    return /M{3,}/.test(String(pattern || '').replace(literal, ''))
}

function monthFromName(text, calendar) {
    const needle = text.toLocaleLowerCase()
    const lists = [calendar.months, calendar.monthsAbbr]
    for (const list of lists) {
        const index = list.findIndex(name => name && name.toLocaleLowerCase() === needle)
        if (index >= 0) return index
    }
    //a prefix is enough, as it is for the abbreviations a user types by hand
    for (const list of lists) {
        const index = list.findIndex(name => name && needle.length >= 3 && name.toLocaleLowerCase().startsWith(needle))
        if (index >= 0) return index
    }
    return -1
}

function pivotYear(year, digits) {
    if (digits > 2 || year > 99) return year
    //DateTimeFormatInfo.InvariantInfo.Calendar.TwoDigitYearMax is 2049
    return year < 50 ? 2000 + year : 1900 + year
}

/**
 * Reads a date typed in the culture's short-date pattern.
 * @returns {{year:number,month:number,day:number}|undefined} month is 0-based, as the
 *          date picker's i18n contract wants it.
 */
export function parseDateParts(text, pattern, cultureData) {
    const calendar = normalizeCulture(cultureData).calendar
    const raw = String(text ?? '').trim()
    if (!raw) return undefined

    const order = dateOrder(pattern)
    const tokens = raw.split(/[^\p{L}\p{N}]+/u).filter(Boolean)
    if (tokens.length < 2 || tokens.length > 3) return undefined

    const parts = { y: null, M: null, d: null }
    const named = hasMonthName(pattern) || tokens.some(token => /\p{L}/u.test(token))
    let digitsOfYear = 4

    for (let i = 0; i < tokens.length; i++) {
        const token = tokens[i]
        const slot = order[i] ?? order[order.length - 1]
        if (/\p{L}/u.test(token)) {
            //a name can only be the month, wherever the culture puts it
            const month = monthFromName(token, calendar)
            if (month < 0) return undefined
            parts.M = month + 1
            continue
        }
        if (!/^\d+$/.test(token)) return undefined
        //when a name took the month slot, the numbers fill the remaining slots in order
        const target = named && slot === 'M' ? order.find(p => p !== 'M' && parts[p] == null) ?? slot : slot
        if (target === 'y') digitsOfYear = token.length
        parts[target] = Number(token)
    }

    //a two-part date is day and month; the year is the current one
    if (parts.y == null) parts.y = new Date().getFullYear()
    if (parts.M == null || parts.d == null) return undefined

    const year = pivotYear(parts.y, digitsOfYear)
    const month = parts.M - 1
    const day = parts.d
    if (month < 0 || month > 11 || day < 1 || day > 31) return undefined
    //reject an impossible day (31 February) the way the picker expects: no value
    const probe = new Date(year, month, day)
    probe.setFullYear(year)
    if (probe.getMonth() !== month || probe.getDate() !== day) return undefined
    return { year, month, day }
}

/**
 * Reads a time typed in the culture's short/long time pattern, with or without the
 * culture's AM/PM designator.
 * @returns {{hours:number,minutes:number,seconds:number,milliseconds:number}|undefined}
 */
export function parseTimeParts(text, cultureData) {
    const calendar = normalizeCulture(cultureData).calendar
    const raw = String(text ?? '').trim()
    if (!raw) return undefined

    const am = (calendar.am || '').trim().toLocaleLowerCase()
    const pm = (calendar.pm || '').trim().toLocaleLowerCase()
    const lower = raw.toLocaleLowerCase()
    let designator = null
    if (pm && lower.includes(pm)) designator = 'pm'
    else if (am && lower.includes(am)) designator = 'am'

    const numbers = raw.match(/\d+/g)
    if (!numbers || numbers.length === 0) return undefined
    let hours = Number(numbers[0])
    const minutes = numbers.length > 1 ? Number(numbers[1]) : 0
    const seconds = numbers.length > 2 ? Number(numbers[2]) : 0
    const milliseconds = numbers.length > 3 ? Number(numbers[3].padEnd(3, '0').slice(0, 3)) : 0

    if (designator === 'pm' && hours < 12) hours += 12
    if (designator === 'am' && hours === 12) hours = 0
    if (hours > 23 || minutes > 59 || seconds > 59) return undefined
    return { hours, minutes, seconds, milliseconds }
}
