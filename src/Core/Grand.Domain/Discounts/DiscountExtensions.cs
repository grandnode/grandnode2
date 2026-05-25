namespace Grand.Domain.Discounts;

public static class DiscountExtensions
{
    /// <summary>
    ///     Returns true when the discount is enabled and <paramref name="utcNow" /> falls within
    ///     its optional start/end date window (both bounds are inclusive).
    /// </summary>
    public static bool IsDiscountActive(this Discount discount, DateTime utcNow)
    {
        if (!discount.IsEnabled)
            return false;

        if (discount.StartDateUtc.HasValue && utcNow < discount.StartDateUtc.Value)
            return false;

        if (discount.EndDateUtc.HasValue && utcNow > discount.EndDateUtc.Value)
            return false;

        return true;
    }
}
