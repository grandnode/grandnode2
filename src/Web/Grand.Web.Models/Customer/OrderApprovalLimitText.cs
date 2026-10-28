using System.Globalization;

namespace Grand.Web.Models.Customer;

/// <summary>
///     The order approval limit as a shopper types and reads it: text in the current culture. A native number
///     input posts a dot decimal, which a comma-decimal culture rejects or reads as a thousands separator
///     (de-DE turns "250.5" into 2505), so the limit travels as text and is parsed here, once, the way the
///     storefront parses its other money fields.
/// </summary>
public static class OrderApprovalLimitText
{
    /// <summary>
    ///     Parses the text with the current culture. Empty means no limit (null); returns false for text that is
    ///     not a finite, non-negative number.
    /// </summary>
    public static bool TryParse(string text, out double? limit)
    {
        limit = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (!double.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture, out var value) || !double.IsFinite(value) || value < 0)
            return false;

        limit = value;
        return true;
    }

    /// <summary>
    ///     Formats the limit with the current culture, without a currency symbol or digit grouping; empty for none
    /// </summary>
    public static string Format(double? limit)
    {
        return limit.HasValue ? limit.Value.ToString(CultureInfo.CurrentCulture) : string.Empty;
    }
}
