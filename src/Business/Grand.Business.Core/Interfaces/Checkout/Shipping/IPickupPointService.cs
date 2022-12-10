﻿using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Checkout.Shipping
{
    public interface IPickupPointService
    {

        /// <summary>
        /// Gets a warehouse
        /// </summary>
        /// <param name="pickupPointId">The pickup point identifier</param>
        /// <returns>PickupPoint</returns>
        Task<PickupPoint> GetPickupPointById(string pickupPointId);

        /// <summary>
        /// Gets all pickup points
        /// </summary>
        /// <returns>PickupPoints</returns>
        Task<IList<PickupPoint>> GetAllPickupPoints();

        /// <summary>
        /// Gets active pickup points
        /// </summary>
        /// <returns>PickupPoints</returns>
        Task<IList<PickupPoint>> LoadActivePickupPoints(string storeId = "");

        /// <summary>
        /// Inserts a pickupPoint
        /// </summary>
        /// <param name="pickupPoint">PickupPoint</param>
        Task InsertPickupPoint(PickupPoint pickupPoint);

        /// <summary>
        /// Updates the pickupPoint
        /// </summary>
        /// <param name="pickupPoint">PickupPoint</param>
        Task UpdatePickupPoint(PickupPoint pickupPoint);

        /// <summary>
        /// Deletes a pickupPoint
        /// </summary>
        /// <param name="pickupPoint">The pickupPoint</param>
        Task DeletePickupPoint(PickupPoint pickupPoint);
    }
}
