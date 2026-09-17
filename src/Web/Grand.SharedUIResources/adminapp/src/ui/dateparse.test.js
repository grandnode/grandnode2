import { describe, it, expect } from 'vitest'
import { dateOrder, parseDateParts, parseTimeParts } from './dateparse.js'

//the calendars of the cultures the panel is used in, as GridCulture.From writes them
const en = { name: 'en-US', calendar: { shortDate: 'M/d/yyyy', shortTime: 'h:mm tt', am: 'AM', pm: 'PM' } }
const pl = { name: 'pl-PL', calendar: { shortDate: 'dd.MM.yyyy', shortTime: 'HH:mm' } }
const de = { name: 'de-DE', calendar: { shortDate: 'dd.MM.yyyy', shortTime: 'HH:mm' } }
const sv = { name: 'sv-SE', calendar: { shortDate: 'yyyy-MM-dd', shortTime: 'HH:mm' } }
const ar = { name: 'ar-SA', calendar: { shortDate: 'dd/MM/yyyy', shortTime: 'h:mm tt', am: 'ص', pm: 'م' } }

describe('dateOrder', () => {
    it('reads the order of the parts out of a .NET pattern', () => {
        expect(dateOrder('M/d/yyyy')).toEqual(['M', 'd', 'y'])
        expect(dateOrder('dd.MM.yyyy')).toEqual(['d', 'M', 'y'])
        expect(dateOrder('yyyy-MM-dd')).toEqual(['y', 'M', 'd'])
    })

    it('ignores literals and falls back to the invariant order', () => {
        expect(dateOrder("dd' de 'MMMM' de 'yyyy")).toEqual(['d', 'M', 'y'])
        expect(dateOrder('')).toEqual(['M', 'd', 'y'])
    })
})

describe('parseDateParts', () => {
    it('reads a date in the culture order, not the browser locale order', () => {
        //the same text means two different days in the two cultures, which is the whole point
        expect(parseDateParts('03.04.2026', pl.calendar.shortDate, pl)).toEqual({ year: 2026, month: 3, day: 3 })
        expect(parseDateParts('03/04/2026', en.calendar.shortDate, en)).toEqual({ year: 2026, month: 2, day: 4 })
        expect(parseDateParts('2026-04-03', sv.calendar.shortDate, sv)).toEqual({ year: 2026, month: 3, day: 3 })
        expect(parseDateParts('03/04/2026', ar.calendar.shortDate, ar)).toEqual({ year: 2026, month: 3, day: 3 })
    })

    it('accepts any separator, as DateTime.Parse does', () => {
        expect(parseDateParts('3-4-2026', de.calendar.shortDate, de)).toEqual({ year: 2026, month: 3, day: 3 })
        expect(parseDateParts('3 4 2026', de.calendar.shortDate, de)).toEqual({ year: 2026, month: 3, day: 3 })
    })

    it('pivots a two-digit year the way the invariant calendar does', () => {
        expect(parseDateParts('01.02.49', pl.calendar.shortDate, pl).year).toBe(2049)
        expect(parseDateParts('01.02.50', pl.calendar.shortDate, pl).year).toBe(1950)
    })

    it('reads a month written as a name', () => {
        const culture = {
            calendar: {
                shortDate: 'd MMMM yyyy',
                months: ['stycznia', 'lutego', 'marca', 'kwietnia', 'maja', 'czerwca', 'lipca', 'sierpnia', 'września', 'października', 'listopada', 'grudnia'],
                monthsAbbr: ['sty', 'lut', 'mar', 'kwi', 'maj', 'cze', 'lip', 'sie', 'wrz', 'paź', 'lis', 'gru']
            }
        }
        expect(parseDateParts('3 kwietnia 2026', culture.calendar.shortDate, culture))
            .toEqual({ year: 2026, month: 3, day: 3 })
        expect(parseDateParts('3 kwi 2026', culture.calendar.shortDate, culture))
            .toEqual({ year: 2026, month: 3, day: 3 })
    })

    it('fills in the current year for a date without one', () => {
        const parsed = parseDateParts('03.04', pl.calendar.shortDate, pl)
        expect(parsed).toEqual({ year: new Date().getFullYear(), month: 3, day: 3 })
    })

    it('refuses what is not a date in this culture', () => {
        expect(parseDateParts('', pl.calendar.shortDate, pl)).toBeUndefined()
        expect(parseDateParts('nonsense', pl.calendar.shortDate, pl)).toBeUndefined()
        //31 February is not a day, and the picker must be given no value rather than a wrong one
        expect(parseDateParts('31.02.2026', pl.calendar.shortDate, pl)).toBeUndefined()
        expect(parseDateParts('13/13/2026', en.calendar.shortDate, en)).toBeUndefined()
    })
})

describe('parseTimeParts', () => {
    it('reads a 24-hour time', () => {
        expect(parseTimeParts('23:45', pl)).toEqual({ hours: 23, minutes: 45, seconds: 0, milliseconds: 0 })
    })

    it('reads the culture AM/PM designators, including a non-Latin one', () => {
        expect(parseTimeParts('8:07 PM', en)).toMatchObject({ hours: 20, minutes: 7 })
        expect(parseTimeParts('12:30 AM', en)).toMatchObject({ hours: 0, minutes: 30 })
        expect(parseTimeParts('8:07 م', ar)).toMatchObject({ hours: 20, minutes: 7 })
        expect(parseTimeParts('8:07 ص', ar)).toMatchObject({ hours: 8, minutes: 7 })
    })

    it('reads seconds when they are typed', () => {
        expect(parseTimeParts('23:45:09', pl)).toMatchObject({ hours: 23, minutes: 45, seconds: 9 })
    })

    it('refuses what is not a time', () => {
        expect(parseTimeParts('', pl)).toBeUndefined()
        expect(parseTimeParts('25:00', pl)).toBeUndefined()
        expect(parseTimeParts('abc', pl)).toBeUndefined()
    })
})
