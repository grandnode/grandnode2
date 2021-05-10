using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Business.Catalog.Extensions
{
    public static class TierPriceExtensions
    {
        /// <summary>
        /// Filter tier prices by date range
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <param name="date">Date in UTC; use null to filter by current</param>
        /// <returns>Tier prices</returns>
        public static IEnumerable<TierPrice> FilterByDate(this IEnumerable<TierPrice> source, DateTime? date = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (!date.HasValue)
                date = DateTime.UtcNow;

            return source.Where(tierPrice =>
                (!tierPrice.StartDateTimeUtc.HasValue || tierPrice.StartDateTimeUtc.Value < date.Value) &&
                (!tierPrice.EndDateTimeUtc.HasValue || tierPrice.EndDateTimeUtc.Value > date.Value));
        }
        /// <summary>
        /// Filter tier prices by specified store
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <param name="storeId">Store id</param>
        /// <returns>Tier prices</returns>
        public static IEnumerable<TierPrice> FilterByStore(this IEnumerable<TierPrice> source, string storeId)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Where(tierPrice => string.IsNullOrEmpty(tierPrice.StoreId) || tierPrice.StoreId == storeId);
        }

        /// <summary>
        /// Filter tier prices for specified customer
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <param name="customer">Customer</param>
        /// <returns>Tier prices</returns>
        public static IEnumerable<TierPrice> FilterForCustomer(this IEnumerable<TierPrice> source, Customer customer)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (customer == null)
                return source.Where(tierPrice => string.IsNullOrEmpty(tierPrice.CustomerGroupId));

            return source.Where(tierPrice => string.IsNullOrEmpty(tierPrice.CustomerGroupId) ||
                customer.Groups.Contains(tierPrice.CustomerGroupId));
        }

        /// <summary>
        /// Filter tier prices by specified currency
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <param name="currencyCode">currencyCode</param>
        /// <returns>Tier prices</returns>
        public static IEnumerable<TierPrice> FilterByCurrency(this IEnumerable<TierPrice> source, string currencyCode)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return source.Where(tierPrice => string.IsNullOrEmpty(tierPrice.CurrencyCode) || tierPrice.CurrencyCode == currencyCode);
        }

        /// <summary>
        /// Remove duplicated quantities if exists
        /// </summary>
        /// <param name="source">Tier prices</param>
        /// <returns>Tier prices</returns>
        public static IEnumerable<TierPrice> RemoveDuplicatedQuantities(this IEnumerable<TierPrice> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            var result = source.OrderBy(x => x.Price).GroupBy(x => x.Quantity).Select(x => x.First()).OrderBy(x => x.Quantity).ToList();
            return result;
        }


    }
}
