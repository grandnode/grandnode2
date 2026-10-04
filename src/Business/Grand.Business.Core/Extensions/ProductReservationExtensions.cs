using Grand.Domain.Catalog;

namespace Grand.Business.Core.Extensions;

public static class ProductReservationExtensions
{
    /// <summary>
    ///     Finds the first resource whose reservations cover the whole rental period, or null when none is free
    /// </summary>
    public static IGrouping<string, ProductReservation> FindGroupToBook(
        this IEnumerable<ProductReservation> reservations, Product product, DateTime rentalStartDate,
        DateTime rentalEndDate)
    {
        foreach (var group in reservations.GroupBy(x => x.Resource))
        {
            var freeDays = group.Select(x => x.Date.Date).ToHashSet();
            if (BookedDays(product, rentalStartDate, rentalEndDate).All(day => freeDays.Contains(day)))
                return group;
        }

        return null;
    }

    /// <summary>
    ///     Gets the reservations of a resource that fall within the rental period
    /// </summary>
    public static IEnumerable<ProductReservation> InRentalPeriod(this IEnumerable<ProductReservation> reservations,
        Product product, DateTime rentalStartDate, DateTime rentalEndDate)
    {
        var bookedDays = BookedDays(product, rentalStartDate, rentalEndDate).ToHashSet();
        return reservations.Where(x => bookedDays.Contains(x.Date.Date));
    }

    /// <summary>
    ///     Gets the days that a rental period covers. Reservations of day products are generated at the store's local
    ///     midnight and read back as UTC, so the time of day of a stored reservation is an arbitrary offset, while the
    ///     dates posted by the datepicker are always midnight - only the date part may be compared.
    /// </summary>
    private static IEnumerable<DateTime> BookedDays(Product product, DateTime rentalStartDate, DateTime rentalEndDate)
    {
        var includeEndDate = product.IncBothDate && product.IntervalUnitId == IntervalUnit.Day;
        for (var iterator = rentalStartDate.Date;
             includeEndDate ? iterator <= rentalEndDate.Date : iterator < rentalEndDate.Date;
             iterator = iterator.AddDays(1))
            yield return iterator;
    }
}
