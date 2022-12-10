﻿using Grand.Domain.Customers;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping
{
    public interface IShippingMethodService
    {
        /// <summary>
        /// Gets a shipping method
        /// </summary>
        /// <param name="shippingMethodId">The shipping method identifier</param>
        /// <returns>Shipping method</returns>
        Task<ShippingMethod> GetShippingMethodById(string shippingMethodId);


        /// <summary>
        /// Gets all shipping methods
        /// </summary>
        /// <param name="filterByCountryId">The country identifier to filter by</param>
        /// <param name="customer"></param>
        /// <returns>Shipping methods</returns>
        Task<IList<ShippingMethod>> GetAllShippingMethods(string filterByCountryId = "", Customer customer = null);

        /// <summary>
        /// Inserts a shipping method
        /// </summary>
        /// <param name="shippingMethod">Shipping method</param>
        Task InsertShippingMethod(ShippingMethod shippingMethod);

        /// <summary>
        /// Updates the shipping method
        /// </summary>
        /// <param name="shippingMethod">Shipping method</param>
        Task UpdateShippingMethod(ShippingMethod shippingMethod);

        /// <summary>
        /// Deletes a shipping method
        /// </summary>
        /// <param name="shippingMethod">The shipping method</param>
        Task DeleteShippingMethod(ShippingMethod shippingMethod);
    }
}
