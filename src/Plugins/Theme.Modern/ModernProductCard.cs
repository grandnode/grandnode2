namespace Theme.Modern;

/// <summary>
///     Values the Modern product card shows that the overview model does not carry directly.
///     The client twin (Catalog/Partials/Client/CatalogProductGridView) repeats the same rule
///     in its template; keep the two in step.
/// </summary>
public static class ModernProductCard
{
    /// <summary>
    ///     Whole-number discount for the card badge, or null when there is nothing worth showing:
    ///     no old price, an old price not above the current one, a zero price, or less than 1%.
    ///     Rounds half away from zero, which is what JavaScript's Math.round does for positives.
    /// </summary>
    public static int? DiscountPercent(double price, double oldPrice)
    {
        if (price <= 0 || oldPrice <= price)
            return null;

        var percent = (int)Math.Round((1 - price / oldPrice) * 100, MidpointRounding.AwayFromZero);
        return percent >= 1 ? percent : null;
    }
}
