using System.Globalization;

namespace Grand.Web.Common.TagHelpers.Admin.Grid;

/// <summary>
///     Number and date formatting data of the request culture for admin.grid.js
///     (adminapp/src/grid/format.js) - the part of CultureInfo the grid formats need, so the
///     browser formats {0:n2}, {0:G} and friends the way the server culture does.
/// </summary>
public class GridCulture
{
    public string Name { get; set; }
    public GridNumberFormat NumberFormat { get; set; }
    public GridCalendar Calendar { get; set; }

    public static GridCulture From(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);
        var dateTimeFormat = GregorianDateTimeFormat(culture);
        var nf = culture.NumberFormat;
        return new GridCulture {
            Name = culture.Name,
            NumberFormat = new GridNumberFormat {
                Decimal = nf.NumberDecimalSeparator,
                Group = nf.NumberGroupSeparator,
                GroupSizes = nf.NumberGroupSizes,
                NegativeSign = nf.NegativeSign,
                Decimals = nf.NumberDecimalDigits,
                Currency = new GridCurrencyFormat {
                    Symbol = nf.CurrencySymbol,
                    Decimals = nf.CurrencyDecimalDigits,
                    Decimal = nf.CurrencyDecimalSeparator,
                    Group = nf.CurrencyGroupSeparator,
                    GroupSizes = nf.CurrencyGroupSizes,
                    PositivePattern = nf.CurrencyPositivePattern,
                    NegativePattern = nf.CurrencyNegativePattern
                },
                Percent = new GridPercentFormat {
                    Symbol = nf.PercentSymbol,
                    Decimals = nf.PercentDecimalDigits,
                    PositivePattern = nf.PercentPositivePattern,
                    NegativePattern = nf.PercentNegativePattern
                }
            },
            Calendar = new GridCalendar {
                ShortDate = dateTimeFormat.ShortDatePattern,
                LongDate = dateTimeFormat.LongDatePattern,
                ShortTime = dateTimeFormat.ShortTimePattern,
                LongTime = dateTimeFormat.LongTimePattern,
                DateSeparator = dateTimeFormat.DateSeparator,
                TimeSeparator = dateTimeFormat.TimeSeparator,
                Am = dateTimeFormat.AMDesignator,
                Pm = dateTimeFormat.PMDesignator,
                Months = dateTimeFormat.MonthGenitiveNames.Take(12).ToArray(),
                MonthsAbbr = dateTimeFormat.AbbreviatedMonthNames.Take(12).ToArray(),
                Days = dateTimeFormat.DayNames,
                DaysAbbr = dateTimeFormat.AbbreviatedDayNames
            }
        };
    }

    /// <summary>
    ///     JavaScript dates are Gregorian. Cultures whose default calendar is not (ar-SA uses
    ///     Um Al-Qura) get the patterns of their Gregorian optional calendar when they have one.
    /// </summary>
    private static DateTimeFormatInfo GregorianDateTimeFormat(CultureInfo culture)
    {
        if (culture.DateTimeFormat.Calendar is GregorianCalendar)
            return culture.DateTimeFormat;
        var gregorian = culture.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();
        if (gregorian is null)
            return culture.DateTimeFormat;
        var clone = (CultureInfo)culture.Clone();
        clone.DateTimeFormat.Calendar = gregorian;
        return clone.DateTimeFormat;
    }
}

public class GridNumberFormat
{
    public string Decimal { get; set; }
    public string Group { get; set; }
    public int[] GroupSizes { get; set; }
    public string NegativeSign { get; set; }
    public int Decimals { get; set; }
    public GridCurrencyFormat Currency { get; set; }
    public GridPercentFormat Percent { get; set; }
}

public class GridCurrencyFormat
{
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string Decimal { get; set; }
    public string Group { get; set; }
    public int[] GroupSizes { get; set; }
    public int PositivePattern { get; set; }
    public int NegativePattern { get; set; }
}

public class GridPercentFormat
{
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public int PositivePattern { get; set; }
    public int NegativePattern { get; set; }
}

public class GridCalendar
{
    public string ShortDate { get; set; }
    public string LongDate { get; set; }
    public string ShortTime { get; set; }
    public string LongTime { get; set; }
    public string DateSeparator { get; set; }
    public string TimeSeparator { get; set; }
    public string Am { get; set; }
    public string Pm { get; set; }
    public string[] Months { get; set; }
    public string[] MonthsAbbr { get; set; }
    public string[] Days { get; set; }
    public string[] DaysAbbr { get; set; }
}
